using Open.Evaluation.Core;
using Open.Hierarchy;
using System;
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

		public static IEvaluate<double> ReduceMultipleMagnitude(
			this EvalDoubleVariationCatalog catalog,
			Node<IEvaluate<double>> source, int geneIndex)
		{
			return catalog.Catalog.ApplyClone(source, geneIndex, g =>
			{
				var absMultiple = Math.Abs(g.Modifier);
				g.Modifier -= g.Modifier / absMultiple;
			});
		}



		public static IEvaluate<double> RemoveGene(
			this EvalDoubleVariationCatalog catalog,
			Node<IEvaluate> source, Node<IEvaluate> gene)
		{
			if (IsValidForRemoval(gene))
			{
				return catalog.Catalog.ApplyClone(source, geneIndex, (g, newGenome) =>
				{
					var parent = newGenome.FindParent(g);
					parent.Remove(g);
				});
			}
			return null;

		}
		public static IEvaluate<double> RemoveGene(
			this EvalDoubleVariationCatalog catalog,
			Genome source, IGene gene)
		{
			return RemoveGene(source, source.Genes.IndexOf(gene));
		}

		public static bool CheckPromoteChildrenValidity(
			this EvalDoubleVariationCatalog catalog,
			Genome source, IGene gene)
		{
			// Validate worthyness.
			var op = gene as GeneNode;
			return op != null && op.Count == 1;
		}

		// This should handle the case of demoting a function.
		public static IEvaluate<double> PromoteChildren(
			this EvalDoubleVariationCatalog catalog,
			Genome source, int geneIndex)
		{
			// Validate worthyness.
			var gene = source.Genes[geneIndex];

			if (CheckPromoteChildrenValidity(source, gene))
			{
				return catalog.Catalog.ApplyClone(source, geneIndex, (g, newGenome) =>
				{
					var op = (GeneNode)g;
					var child = op.Children.Single();
					op.Remove(child);
					newGenome.Replace(g, child);
				});
			}
			return null;

		}

		public static IEvaluate<double> PromoteChildren(
			this EvalDoubleVariationCatalog catalog,
			Genome source, IGene gene)
		{
			return PromoteChildren(source, source.Genes.IndexOf(gene));
		}

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
