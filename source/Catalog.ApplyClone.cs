using Open.Evaluation.Core;
using Open.Hierarchy;
using System;

namespace Open.Evaluation
{
	public static partial class CatalogExtensions
	{
		/// <summary>
		/// Provides a cloned node (as part of a cloned tree) for the handler to operate on.
		/// </summary>
		/// <param name="sourceNode">The node to clone.</param>
		/// <param name="handler">the handler to pass the cloned node to.</param>
		/// <returns>The resultant value corrected by .FixHierarchy()</returns>
		public static T ApplyClone<T, TValue>(
			this Catalog<T> catalog,
			Node<T> sourceNode,
			Action<Node<T>> handler)
			where T : class, IEvaluate<TValue>
		{
			var newGene = catalog.Factory.CloneTree(sourceNode); // * new 1
			handler(newGene);
			var newRoot = catalog.FixHierarchy<T,TValue>(newGene.Root); // * new 2
			var value = newRoot.Value;
			catalog.Factory.Recycle(newGene.Root); // * 1
			catalog.Factory.Recycle(newRoot); // * 2
			return value;
		}
	}
}
