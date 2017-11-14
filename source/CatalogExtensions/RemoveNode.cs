using Open.Evaluation.Core;
using Open.Hierarchy;
using System;

namespace Open.Evaluation
{
	public static partial class CatalogExtensions
	{

		public static Node<T> RemoveNode<T, TValue>(
			this Catalog<T> catalog,
			Node<T> node)
			where T : class, IEvaluate<TValue>
		{
			if (node == null || node.Parent == null) return null;
			var root = node.Root;
			node.Parent.Remove(node);
			return catalog.FixHierarchy<T, TValue>(root, true);
		}
	}
}
