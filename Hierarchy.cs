/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace Open.Evaluation
{

	/// <summary>
	/// Used for mapping a tree of evaluations which do not have access to their parent nodes.
	/// </summary>
	public static class Hierarchy
	{
	
		// WARNING: Care must be taken not to have duplicate nodes anywhere in the tree but having duplicate values are allowed.

		public class Node<T> : LinkedList<Node<T>>, IDisposable
		{
			public Node<T> Parent { get; internal set; }

			public T Value { get; set; }

			public Node()
			{

			}

			/// <summary>
			/// Clones a node by recreating the tree and copying the values.
			/// </summary>
			/// <param name="parent">If a parent is specified it will use that node as its parent.  By default it ends up being detatched.</param>
			/// <param name="onNodeCloned">A function that recieves the old node and its clone.</param>
			/// <returns>The copy of the tree/branch.</returns>
			public Node<T> Clone(
				Node<T> parent = null,
				Action<Node<T>, Node<T>> onNodeCloned = null)
			{
				var clone = new Node<T>()
				{
					Value = this.Value,
					Parent = parent
				};

				foreach (var child in this)
					clone.AddLast(child.Clone(clone));

				onNodeCloned?.Invoke(this, clone);

				return clone;
			}

			/// <summary>
			/// Clones a node by recreating the tree and copying the values.
			/// </summary>
			/// <param name="onNodeCloned">A function that recieves the old node and its clone.</param>
			/// <returns>The copy of the tree/branch.</returns>
			public Node<T> Clone(Action<Node<T>, Node<T>> onNodeCloned)
			{
				return Clone(null, onNodeCloned);
			}

			/// <summary>
			/// Create's a clone of the entire tree but only returns the clone of this node.
			/// </summary>
			/// <returns>A clone of this node as part of a newly cloned tree.</returns>
			public Node<T> CloneTree()
			{
				Node<T> node = null;
				Root.Clone((n, clone) =>
				{
					if (n == this) node = clone;
				});
				return node;
			}

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


			public void ValidateDescendants()
			{
				foreach (var child in this)
				{
					if (child.Parent != this)
						throw new Exception("A node has a child that has its parent mapped to another node.");
					child.ValidateDescendants();
				}
			}

			public void Teardown(Action<Node<T>> recycledNodeHandler = null)
			{
				Value = default(T);
				while(Count!=0)
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

		/// <summary>
		/// Generates a full hierarchy if the root is an IParent and uses the root as the value of the hierarchy.
		/// Essentially building a map of the tree.
		/// </summary>
		/// <typeparam name="T">Child type.</typeparam>
		/// <typeparam name="TRoot">The type of the root.</typeparam>
		/// <param name="root">The root instance.</param>
		/// <returns>The full map of the root.</returns>
		public static Node<T> Get<T, TRoot>(TRoot root)
			where TRoot : T
		{
			var parent = root as IParent<T>;
			var current = new Node<T>()
			{
				Value = root
			};

			foreach (var child in parent.Children)
			{
				var node = Get<T>(child);
				node.Parent = current;
				current.AddLast(node);
			}
			return current;
		}

		public static Node<T> Get<T>(T root)
		{
			return Get<T, T>(root);
		}

		public static bool AreChildrenAligned(this Node<IEvaluate> target)
		{
			if (target.Value is IParent parent)
			{
				// If the value contains children, return true only if they match.
				var children = target.ToArray();
				var count = children.Length;

                // The count should match...
				if (count != parent.Children.Count)
					return false;

				for (var i = 0; i < count; i++)
				{
                    // Does the map of the children match the actual?
					if (children[i] != parent.Children[i])
						return false;
				}
			}
			else
			{
				// Value does not have children? Return true only if this has no children.
				return target.Count == 0;
			}

			// Everything is the same..
			return true;
		}

		public static void AlignValues(this Node<IEvaluate> target)
		{
			//var parent = target.Value as IParent;
			//if (parent != null)
			//{
			//	var nChildren = target.ToList();
			//	var count = nChildren.Count;
			//	if (count != parent.Children.Count)
			//		return true;

			//	for (var i = 0; i < count; i++)
			//	{
			//		if (nChildren[i] != parent.Children[i])
			//			return true;
			//	}
			//}

			//return false;
		}


	}
}