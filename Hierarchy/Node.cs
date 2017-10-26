/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Disposable;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace Open.Evaluation.Hierarchy
{

	public sealed class Node<T> : IReadOnlyCollection<Node<T>>
	{
		// Prefering a LinkedList over List because of the adding, removing, splicing that can happen when attemtping to manipulate a tree.
		internal readonly LinkedList<Node<T>> Children = new LinkedList<Node<T>>();

		public Node<T> Parent { get; internal set; }

		public T Value { get; set; }

		Node() { }

		/// <summary>
		/// Iterates through all of the descendants of this node starting breadth first.
		/// </summary>
		/// <returns>All the descendants of this node.</returns>
		public IEnumerable<Node<T>> GetDescendants()
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
		public IEnumerable<Node<T>> GetNodes()
		{
			yield return this;
			foreach (var descendant in GetDescendants())
				yield return descendant;
		}

		/// <summary>
		/// Finds the root node of this tree.
		/// </summary>
		public Node<T> Root
		{
			get
			{
				var current = this;
				while (current.Parent != null)
					current = current.Parent;
				return current;
			}
		}

		public int Count => Children.Count;

		/// <summary>
		/// Asserts that parent child relationships are correclty aligned and that there aren't dupilicate nodes in the tree.
		/// </summary>
		public void Validate()
		{
			// Ensure no duplicate nodes.
			Validate(new HashSet<Node<T>>());
		}

		void Validate(HashSet<Node<T>> registry)
		{
			foreach (var child in Children)
			{
				if (child.Parent != this)
					throw new Exception("A node has a child that has its parent mapped to another node.");
				if (!registry.Add(child))
					throw new Exception("A node already exists in the tree and will cause infinite recusion.");

				child.Validate(registry);
			}
		}

		internal void Teardown(Action<Node<T>> afterNodeRemovedHandler = null)
		{
			Value = default(T);
			while (Children.Count != 0)
			{
				Node<T> child;
				lock (Children) // Not really necessary but just in case there's an external call that occurs twice.  This will ensure nodes are capured correctly instead of potentially duplicated or missed.
				{
					var count = Children.Count;
					if (count == 0) break;
					child = Children.Last.Value;
					Children.RemoveLast();
				}
				child.Teardown(afterNodeRemovedHandler);
			}
			afterNodeRemovedHandler?.Invoke(this);
		}

		public IEnumerator<Node<T>> GetEnumerator()
		{
			return Children.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}



		/// <summary>
		/// Used for mapping a tree of evaluations which do not have access to their parent nodes.
		/// </summary>
		public class Factory : DisposableBase
		{
			#region Creation, Recycling, and Disposal
			public Factory()
			{
				Pool = new ConcurrentQueueObjectPool<Node<T>>(
					() => new Node<T>(), PrepareForPool, ushort.MaxValue);
			}

			protected override void OnDispose(bool calledExplicitly)
			{
				if (calledExplicitly)
				{
					DisposeOf(ref Pool);
				}
			}

			ConcurrentQueueObjectPool<Node<T>> Pool;

			public void Recycle(Node<T> n)
			{
				AssertIsAlive();

				Pool.Give(n);
			}

			void PrepareForPool(Node<T> n)
			{
				n.Value = default(T);
				n.Parent = null;
				n.Teardown(c =>
				{
					// Avoid recursion.
					if (c != n) Pool?.Give(n);
				});
			}
			#endregion

			// WARNING: Care must be taken not to have duplicate nodes anywhere in the tree but having duplicate values are allowed.

			/// <summary>
			/// Clones a node by recreating the tree and copying the values.
			/// </summary>
			/// <param name="target">The node to replicate.</param>
			/// <param name="parent">If a parent is specified it will use that node as its parent.  By default it ends up being detatched.</param>
			/// <param name="onNodeCloned">A function that recieves the old node and its clone.</param>
			/// <returns>The copy of the tree/branch.</returns>
			public Node<T> Clone(
				Node<T> target,
				Node<T> parent = null,
				Action<Node<T>, Node<T>> onNodeCloned = null)
			{
				AssertIsAlive();

				var clone = Pool.Take();
				clone.Value = target.Value;
				clone.Parent = parent;

				foreach (var child in target)
					clone.Children.AddLast(Clone(child, clone));

				onNodeCloned?.Invoke(target, clone);

				return clone;
			}


			/// <summary>
			/// Clones a node by recreating the tree and copying the values.
			/// </summary>
			/// <param name="target">The node to replicate.</param>
			/// <param name="onNodeCloned">A function that recieves the old node and its clone.</param>
			/// <returns>The copy of the tree/branch.</returns>
			public Node<T> Clone(Node<T> target, Action<Node<T>, Node<T>> onNodeCloned)
			{
				return Clone(target, null, onNodeCloned);
			}

			/// <summary>
			/// Create's a clone of the entire tree but only returns the clone of this node.
			/// </summary>
			/// <returns>A clone of this node as part of a newly cloned tree.</returns>
			public Node<T> CloneTree(Node<T> target)
			{
				Node<T> node = null;
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
			public Node<T> Map<TRoot>(TRoot root)
			where TRoot : T
			{
				AssertIsAlive();

				var current = Pool.Take();
				current.Value = root;

				if (root is IParent<T> parent)
				{
					foreach (var child in parent.Children)
					{
						var node = Map<T>(child);
						node.Parent = current;
						current.Children.AddLast(node);
					}
				}

				return current;
			}

			/// <summary>
			/// Generates a full hierarchy if the root is an IParent and uses the root as the value of the hierarchy.
			/// Essentially building a map of the tree.
			/// </summary>
			/// <typeparam name="T">The type of the root.</typeparam>
			/// <param name="root">The root instance.</param>
			/// <returns>The full map of the root.</returns>
			public Node<T> Map(T root)
			{
				return Map<T>(root);
			}

			/// <summary>
			/// Generates a full hierarchy if the root of the container is an IParent and uses the root as the value of the hierarchy.
			/// Essentially building a map of the tree.
			/// </summary>
			/// <typeparam name="T">The type of the root.</typeparam>
			/// <param name="container">The container of the root instance.</param>
			/// <returns>The full map of the root.</returns>
			public Node<T> Map<TRoot>(IHaveRoot<TRoot> container)
				where TRoot : T
			{
				return Map(container.Root);
			}

		}

	}

}