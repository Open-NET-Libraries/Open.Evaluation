using Open.Evaluation.Core;
using Open.Hierarchy;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Open.Evaluation
{
	public static partial class CatalogExtensions
	{
		/// <summary>
		/// For any evaluation node, correct the hierarchy to match.
		/// 
		/// Uses .NewUsing methods to reconstruct the tree.
		/// </summary>
		/// <param name="target">The node tree to correct.</param>
		/// <returns>The updated tree.</returns>
		public static Node<T> FixHierarchy<T, TValue>(
			this Catalog<T> catalog,
			Node<T> target,
			bool operateDirectly = false)
			where T : class, IEvaluate<TValue>
		{
			if (!operateDirectly)
				target = catalog.Factory.Clone(target);

			var value = target.Value;
			// Does this node's value contain children?
			if (value is IParent<IEnumerable<T>> p)
			{
				var fixedChildren = target
					.Select(n => {
						var f = FixHierarchy<T, TValue>(catalog, n, true);
						var v = f.Value;
						if(f!=n) catalog.Factory.Recycle(f); // Only the owner of the target node should do the recycling.
						return catalog.Register(v);
					}).ToArray();

				Node<T> node;

				if (value is IReproducable<IEnumerable<T>> r)
				{
					// This recursion technique will operate on the leaves of the tree first.
					node = catalog.Factory.Map(
						(T)r.NewUsing(
							(ICatalog<IEvaluate>)catalog,
							// Using new children... Rebuild using new structure and check for registration.
							fixedChildren
						)
					);
				}
				else if (value is IReproducable<(T, T)> e) // Functions, exponent, etc...
				{
					Debug.Assert(target.Children.Count==2);
					node = catalog.Factory.Map(
						(T)e.NewUsing(
							(ICatalog<IEvaluate>)catalog,
							(fixedChildren[0], fixedChildren[1])
						)
					);

				}
				else
				{
					throw new Exception("Unknown IParent / IReproducable.");
				}

				target.Parent?.Replace(target, node);
				return node;

			}
			// No children? Then clear any child notes.
			else
			{
				target.Clear();

				var old = target.Value;
				var registered = catalog.Register(target.Value);
				if (old != registered)
					target.Value = registered;

				return target;
			}
		}
	}
}
