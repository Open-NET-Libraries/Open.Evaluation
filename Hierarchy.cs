/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Disposable;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Open.Evaluation.Hierarchy
{

	/// <summary>
	/// Used for mapping a tree of evaluations which do not have access to their parent nodes.
	/// </summary>
	public class NodeFactory<T> : DisposableBase
	{
		public NodeFactory()
		{
			Pool = new ConcurrentQueueObjectPool<Node>(
				() => new Node(), PrepareForPool, ushort.MaxValue);
		}

		protected override void OnDispose(bool calledExplicitly)
		{
			if (calledExplicitly)
			{
				DisposeOf(ref Pool);
			}
		}

		ConcurrentQueueObjectPool<Node> Pool;

		public void Recycle(Node n)
		{
			Pool.Give(n);
		}

		void PrepareForPool(Node n)
		{
			n.Value = default(T);
			n.Parent = null;
			n.Teardown(c =>
			{
				// Avoid recursion.
				if (c != n) Pool.Give(n);
			});
		}

		// WARNING: Care must be taken not to have duplicate nodes anywhere in the tree but having duplicate values are allowed.

		/// <summary>
		/// Clones a node by recreating the tree and copying the values.
		/// </summary>
		/// <param name="target">The node to replicate.</param>
		/// <param name="parent">If a parent is specified it will use that node as its parent.  By default it ends up being detatched.</param>
		/// <param name="onNodeCloned">A function that recieves the old node and its clone.</param>
		/// <returns>The copy of the tree/branch.</returns>
		public Node Clone(
			Node target,
			Node parent = null,
			Action<Node, Node> onNodeCloned = null)
		{
			var clone = Pool.Take();
			clone.Value = target.Value;
			clone.Parent = parent;

			foreach (var child in target)
				clone.AddLast(Clone(child, clone));

			onNodeCloned?.Invoke(target, clone);

			return clone;
		}


		/// <summary>
		/// Clones a node by recreating the tree and copying the values.
		/// </summary>
		/// <param name="target">The node to replicate.</param>
		/// <param name="onNodeCloned">A function that recieves the old node and its clone.</param>
		/// <returns>The copy of the tree/branch.</returns>
		public Node Clone(Node target, Action<Node, Node> onNodeCloned)
		{
			return Clone(target, null, onNodeCloned);
		}

		/// <summary>
		/// Create's a clone of the entire tree but only returns the clone of this node.
		/// </summary>
		/// <returns>A clone of this node as part of a newly cloned tree.</returns>
		public Node CloneTree(Node target)
		{
			Node node = null;
			Clone(target.Root, (n, clone) =>
			{
				if (n == target) node = clone;
			});
			return node;
		}


		/// <summary>
		/// Generates a full hierarchy if the root is an IParent and uses the root as the value of the hierarchy.
		/// Essentially building a map of the tree.
		/// </summary>
		/// <typeparam name="T">Child type.</typeparam>
		/// <typeparam name="TRoot">The type of the root.</typeparam>
		/// <param name="root">The root instance.</param>
		/// <returns>The full map of the root.</returns>
		public Node GetHierarchy<TRoot>(TRoot root)
			where TRoot : T
		{
			var current = Pool.Take();
			current.Value = root;

			if( root is IParent<T> parent)
			{
				foreach (var child in parent.Children)
				{
					var node = GetHierarchy<T>(child);
					node.Parent = current;
					current.AddLast(node);
				}
			}

			return current;
		}

		public Node GetHierarchy(T root)
		{
			return GetHierarchy<T>(root);
		}

		public class Node : LinkedList<Node>, IDisposable
		{
			public Node Parent { get; internal set; }

			public T Value { get; set; }

			internal Node()
			{

			}


			/// <summary>
			/// Iterates through all of the descendants of this node starting breadth first.
			/// </summary>
			/// <returns>All the descendants of this node.</returns>
			public IEnumerable<Node> GetDescendants()
			{
				// Attempt to be more breadth first.

				foreach (var child in this)
					yield return child;

				var grandchildren = this.SelectMany(c => c);
				foreach (var grandchild in grandchildren)
					yield return grandchild;

				foreach (var descendant in grandchildren.SelectMany(c => c.GetDescendants()))
					yield return descendant;
			}

			/// <returns>This and all of its descendants.</returns>
			public IEnumerable<Node> GetNodes()
			{
				yield return this;
				foreach (var descendant in GetDescendants())
					yield return descendant;
			}

			/// <summary>
			/// Finds the root node of this tree.
			/// </summary>
			public Node Root
			{
				get
				{
					var current = this;
					while (current.Parent != null)
						current = current.Parent;
					return current;
				}
			}


			public void ValidateDescendants()
			{
				foreach (var child in this)
				{
					if (child.Parent != this)
						throw new Exception("A node has a child that has its parent mapped to another node.");
					child.ValidateDescendants();
				}
			}

			public void Teardown(Action<Node> recycledNodeHandler = null)
			{
				Value = default(T);
				while (Count != 0)
				{
					var child = Last.Value;
					RemoveLast();
					child.Teardown(recycledNodeHandler);
				}
				recycledNodeHandler?.Invoke(this);
			}

			public void Dispose()
			{
				Teardown();
			}


		}

	}
}