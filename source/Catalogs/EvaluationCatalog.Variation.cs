using Open.Evaluation.Core;
using Open.Hierarchy;
using System;
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

		static bool CheckPromoteChildrenValidity(
			IParent parent)
			// Validate worthyness.
			=> parent?.Children.Count == 1;




		public static IEvaluate<double> PromoteChildren(
			this EvalDoubleVariationCatalog catalog,
			Node<IEvaluate<double>> gene)
		{
			// Validate worthyness.
			if (!CheckPromoteChildrenValidity(gene)) return null;

			return catalog.Catalog.ApplyClone(gene,
				newGene => newGene.Value = newGene.Children.Single().Value);
		}

		// This should handle the case of demoting a function.
		public static IEvaluate<double> PromoteChildren(
			this EvalDoubleVariationCatalog catalog,
			Node<IEvaluate<double>> root, int descendantIndex)
			=> PromoteChildren(catalog, root.GetDescendantsOfType().ElementAt(descendantIndex));

		public static IEvaluate<double> ApplyFunction(
			this EvalDoubleVariationCatalog catalog,
			Genome source, int geneIndex, char fn)
		{
			if (!Operators.Available.Functions.Contains(fn))
				throw new ArgumentException("Invalid function operator.", nameof(fn));

			// Validate worthyness.
			return catalog.Catalog.ApplyClone(source, geneIndex, (g, newGenome) =>
			{
				var newFn = Operators.New(fn);
				newGenome.Replace(g, newFn);
				newFn.Add(g);
			});

		}

		public static IEvaluate<double> ApplyFunction(
			this EvalDoubleVariationCatalog catalog,
			Genome source, IGene gene, char fn)
		{
			return ApplyFunction(source, source.Genes.IndexOf(gene), fn);
		}
	}
}
