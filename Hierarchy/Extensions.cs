using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Open.Evaluation.Hierarchy
{
    public static class Extensions
    {

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

		/// <summary>
		/// For any evaluation node, correct the hierarchy to match.
		/// </summary>
		/// <param name="target">The node tree to correct.</param>
		/// <returns>The updated tree.</returns>
		public static Node<T> FixHierarchy<T>(
			this Node<T>.Factory factory,
			Node<T> target,
			Catalog<T> catalog)
			where T : IEvaluate
		{
			// Does this node's value contain children?
			if (target.Value is IReproducable r)
			{
				// This recursion technique will operate on the leaves of the tree first.
				var node = factory.Map(
					(T)r.CreateNewFrom(
						r.ReproductionParam,
						// Using new children... Rebuild using new structure and check for registration.
						target.Select(n => (IEvaluate)catalog.Register(factory.FixHierarchy(n, catalog).Value))
					)
				);
				node.Parent = target.Parent;
				return node;
			}
			// No children? Then clear any child notes.
			else
			{
				if (target.Count != 0)
					target.Children.Clear();

				var old = target.Value;
				var registered = catalog.Register(target.Value);
				if (!old.Equals(registered))
					target.Value = registered;

				return target;
			}
		}
	}
}
