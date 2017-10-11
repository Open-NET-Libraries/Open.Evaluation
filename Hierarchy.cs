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
			public Node<T> Parent { get; set; }

			public T Value { get; set; }

			public Node()
			{

			}

			/// <summary>
			/// Clones a node by recreating the tree and copying the values.
			/// </summary>
			/// <param name="parent">If a parent is specified it will use that node as its parent.  By default it ends up being detatched.</param>
			/// <returns>The copy of the tree/branch.</returns>
			public Node<T> Clone(Node<T> parent = null)
			{
				var clone = new Node<T>()
				{
					Value = this.Value,
					Parent = parent
				};
				foreach (var child in this)
					clone.AddLast(child.Clone(clone));

				return clone;
			}

			/// <summary>
			/// Clones existing node and looks for replacement node to replace with a clone of the replacement.
			/// </summary>
			/// <param name="toReplace">The node to search for and replace.</param>
			/// <param name="replacement">The node to use as a replacement.</param>
			/// <param name="parent">If a parent is specified it will use that node as its parent.  By default it ends up being detatched.</param>
			/// <returns>The copy of the tree/branch.</returns>
			public Node<T> CloneReplaced(Node<T> toReplace, Node<T> replacement, Node<T> parent = null)
			{
				if (this == toReplace)
					return replacement.Clone(parent);

				var clone = new Node<T>();
				clone.Value = this.Value;
				clone.Parent = parent;

				foreach (var child in this)
					clone.AddLast(child.CloneReplaced(toReplace, replacement, clone));

				return clone;
			}

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

			public IEnumerable<Node<T>> GetNodes()
			{
				yield return this;
				foreach (var descendant in GetDescendants())
					yield return descendant;
			}

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
				if (count != parent.Children.Count)
					return false;

				for (var i = 0; i < count; i++)
				{
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