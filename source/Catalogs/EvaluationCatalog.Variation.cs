using Open.Evaluation.Arithmetic;
using Open.Evaluation.Core;
using Open.Hierarchy;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;

namespace Open.Evaluation.Catalogs
{
	using EvalDoubleVariationCatalog = EvaluationCatalog<double>.VariationCatalog;

	public partial class EvaluationCatalog<T>
		where T : IComparable
	{
		private VariationCatalog _variation;
		public VariationCatalog Variation =>
			LazyInitializer.EnsureInitialized(ref _variation, () => new VariationCatalog(this));

		public class VariationCatalog : SubmoduleBase<EvaluationCatalog<T>>
		{
			internal VariationCatalog(EvaluationCatalog<T> source) : base(source)
			{

			}
		}
	}


	public static partial class EvaluationCatalogExtensions
	{

		public static bool IsValidForRemoval<T>(this Node<IEvaluate<T>> gene, bool ifRoot = false)
			where T : struct, IComparable
		{
			if (gene == gene.Root) return ifRoot;
			// Validate worthyness.
			var parent = gene.Parent;
			Debug.Assert(parent != null);

			switch (parent.Value)
			{
				case OperatorBase<T> _ when parent.Count < 2:
				case Exponent<T> _:
					return false;
			}

			// Search for potential futility...
			// Basically, if there is no dynamic nodes left after reduction then it's not worth removing.
			return !parent.Any(g => g != gene && !(g.Value is IConstant))
				   && parent.IsValidForRemoval(true);
		}

		/// <summary>
		/// Removes a node from its parent.
		/// </summary>
		/// <param name="catalog">The catalog to use.</param>
		/// <param name="node">The node to remove from the tree.</param>
		/// <param name="newRoot">The resultant root node corrected by .FixHierarchy()</param>
		/// <returns>true if sucessful</returns>
		public static bool TryRemoveValid(
			this EvalDoubleVariationCatalog catalog,
			Node<IEvaluate<double>> node,
			out IEvaluate<double> newRoot)
		{
			Debug.Assert(catalog != null);
			if (node == null) throw new ArgumentNullException(nameof(node));
			if (IsValidForRemoval(node))
			{
				newRoot = catalog.Catalog
					.RemoveNode(node.CloneTree())
					.Recycle();
				return true;
			}
			newRoot = default;
			return false;
		}

		/// <summary>
		/// Removes a node from its parent.
		/// </summary>
		/// <param name="catalog">The catalog to use.</param>
		/// <param name="sourceNode">The root node to remove a descendant from.</param>
		/// <param name="descendantIndex">The index of the descendant in the heirarchy (breadth-first).</param>
		/// <param name="newRoot">The resultant root node corrected by .FixHierarchy()</param>
		/// <returns>true if sucessful</returns>
		public static bool TryRemoveValidAt(
			this EvalDoubleVariationCatalog catalog,
			Node<IEvaluate<double>> sourceNode,
			int descendantIndex,
			out IEvaluate<double> newRoot)
		{
			Debug.Assert(catalog != null);
			if (sourceNode == null) throw new ArgumentNullException(nameof(sourceNode));

			return TryRemoveValid(
				catalog,
				sourceNode
					.GetDescendantsOfType()
					.ElementAt(descendantIndex),
				out newRoot);
		}




		static bool CheckPromoteChildrenValidity(
			IParent parent)
			// Validate worthyness.
			=> parent?.Children.Count == 1;

		public static IEvaluate<double> PromoteChildren(
			this EvalDoubleVariationCatalog catalog,
			Node<IEvaluate<double>> node)
		{
			Debug.Assert(catalog != null);
			if (node == null) throw new ArgumentNullException(nameof(node));
			Contract.EndContractBlock();

			// Validate worthyness.
			if (!CheckPromoteChildrenValidity(node)) return null;

			return catalog.Catalog.ApplyClone(node,
				newNode => newNode.Children.Single());
		}

		// This should handle the case of demoting a function.
		public static IEvaluate<double> PromoteChildrenAt(
			this EvalDoubleVariationCatalog catalog,
			Node<IEvaluate<double>> root, int descendantIndex)
		{
			Debug.Assert(catalog != null);
			if (root == null) throw new ArgumentNullException(nameof(root));
			Contract.EndContractBlock();

			return PromoteChildren(catalog,
				root.GetDescendantsOfType()
					.ElementAt(descendantIndex));
		}

		public static IEvaluate<double> ApplyFunction(
			this EvalDoubleVariationCatalog catalog,
			Node<IEvaluate<double>> node, char fn, IEnumerable<IEvaluate<double>> parameters)
		{
			Debug.Assert(catalog != null);
			if (node == null) throw new ArgumentNullException(nameof(node));
			Contract.EndContractBlock();

			if (!Registry.Arithmetic.Functions.Contains(fn))
				throw new ArgumentException("Invalid function operator.", nameof(fn));

			var c = catalog.Catalog;
			return c.ApplyClone(node, newNode =>
				Registry.Arithmetic.GetFunction(c, fn, parameters.ToArray()));
		}

		public static IEvaluate<double> ApplyRandomFunction(
			this EvalDoubleVariationCatalog catalog,
			Node<IEvaluate<double>> node)
		{
			Debug.Assert(catalog != null);
			if (node == null) throw new ArgumentNullException(nameof(node));
			Contract.EndContractBlock();

			var c = catalog.Catalog;
			var n = Registry.Arithmetic.GetRandomFunction(c, node.Value);
			return n == null ? null : c.ApplyClone(node, newNode => n);
		}

		public static IEvaluate<double> ApplyFunctionAt(
			this EvalDoubleVariationCatalog catalog,
			Node<IEvaluate<double>> root, int descendantIndex, char fn, IEnumerable<IEvaluate<double>> parameters)
		{
			Debug.Assert(catalog != null);
			if (root == null) throw new ArgumentNullException(nameof(root));
			Contract.EndContractBlock();

			return ApplyFunction(catalog,
				root.GetDescendantsOfType()
					.ElementAt(descendantIndex).Parent, fn, parameters);
		}


	}
}
