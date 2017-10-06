/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using System.Collections.Generic;
using System.Linq;

namespace Open.Evaluation
{
	public interface IParent
	{
		IReadOnlyList<object> Children { get; }
	}

	public interface IParent<out TChild> : IParent
	{
		new IReadOnlyList<TChild> Children { get; }
	}


	public static class Hierarchy
	{
		// WARNING: Care must be taken not to have duplicate nodes anywhere in the tree but having duplicate values are allowed.

		public class Node<T> : LinkedList<Node<T>>
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

		}


		public static Node<T> Get<T, TRoot>(TRoot root)
			where TRoot : T
		{
			var current = new Node<T>();
			current.Value = root;

			var parent = root as IParent<T>;
			foreach (var child in parent.Children)
			{
				var node = Get<T, T>(child);
				node.Parent = current;
				current.AddLast(node);
			}
			return current;
		}

		public static bool AreChildrenMaligned(this Node<IEvaluate> target)
		{
			var parent = target.Value as IParent;
			if (parent != null)
			{
				var nChildren = target.ToList();
				var count = nChildren.Count;
				if (count != parent.Children.Count)
					return true;

				for (var i = 0; i < count; i++)
				{
					if (nChildren[i] != parent.Children[i])
						return true;
				}
			}

			return false;
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