using Open.Evaluation.Arithmetic;
using Open.Evaluation.Core;
using Open.Hierarchy;
using Open.Numeric;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Threading;

namespace Open.Evaluation.Catalogs
{
	public partial class EvaluationCatalog<T>
		where T : IComparable
	{
		private MutationCatalog _mutation;

		public MutationCatalog Mutation =>
			LazyInitializer.EnsureInitialized(ref _mutation, () => new MutationCatalog(this));

		public class MutationCatalog : SubmoduleBase<EvaluationCatalog<T>>
		{
			internal MutationCatalog(EvaluationCatalog<T> source) : base(source)
			{

			}
		}
	}

	[SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
	public static partial class EvaluationCatalogExtensions
	{

		public static IEvaluate<double> MutateSign(
			this EvaluationCatalog<double>.MutationCatalog catalog,
			Node<IEvaluate<double>> node, byte options = 3)
		{
			if (node == null) throw new ArgumentNullException(nameof(node));
			if (options > 3) throw new ArgumentOutOfRangeException(nameof(options));
			Contract.EndContractBlock();

			var root = node.Root;
			var isRoot = root == node;
			// ReSharper disable once ImplicitlyCapturedClosure
			bool parentIsSquareRoot() => !isRoot && node.Parent.Value is Exponent<double> ex && ex.IsSquareRoot();

			// ReSharper disable once AccessToModifiedClosure
			var modifier = new Lazy<double>(() => catalog.Catalog.GetMultiple(node.Value));

			switch (RandomUtilities.Random.Next(options))
			{
				case 0:
					// Alter Sign
					var result = catalog.Catalog.MultiplyNode(node, -1);
					if (!parentIsSquareRoot()) return result;

					node = catalog.Factory.Map(result);
					// Sorry, not gonna mess with unreal (sqrt neg numbers yet).
					if (RandomUtilities.Random.Next(2) == 0)
						goto case 1;

					goto case 2;

				case 1:
					// Don't zero the root or make the internal multiple negative.
					if (isRoot && modifier.Value == +1 || parentIsSquareRoot() && modifier.Value <= 0)
						goto case 2;

					// Decrease multiple.
					return catalog.Catalog.AdjustNodeMultiple(node, -1);

				case 2:
					// Don't zero the root. (makes no sense)
					if (isRoot && modifier.Value == -1)
						goto case 1;
					// Increase multiple.
					return catalog.Catalog.AdjustNodeMultiple(node, +1);
			}

			throw new ArgumentOutOfRangeException(nameof(options));

		}

		public static IEvaluate<double> MutateParameter(
			this EvaluationCatalog<double>.MutationCatalog catalog,
			IEvaluate<double> root, Parameter gene)
		{
			var inputParamCount = root.Genes.OfType<Parameter>().GroupBy(p => p.ToString()).Count();
			return catalog.Catalog.ApplyClone(root, gene, (g, newGenome) =>
			{
				var parameter = (Parameter)g;
				var nextParameter = RandomUtilities.NextRandomIntegerExcluding(inputParamCount + 1, parameter.ID);
				newGenome.Replace(g, GetParameterGene(nextParameter, parameter.Modifier));
			});
		}

		public static IEvaluate<double> ChangeOperation(
			this EvaluationCatalog<double>.MutationCatalog catalog,
			IEvaluate<double> root, IOperator<,> gene)
		{
			bool isFn = gene is IFunction;
			if (isFn)
			{
				// Functions with no other options?
				if (Operators.Available.Functions.Count < 2)
					return null;
			}
			else
			{
				// Never will happen, but logic states that this is needed.
				if (Operators.Available.Operators.Count < 2)
					return null;
			}

			return ApplyClone(root, gene, (g, newGenome) =>
			{
				var og = (IOperator)g;
				IOperator replacement = isFn
					? Operators.GetRandomFunctionGene(og.Operator)
					: Operators.GetRandomOperationGene(og.Operator);
				replacement.AddThese(og.Children);
				og.Clear();
				newGenome.Replace(g, replacement);
			});
		}

		public static IEvaluate<double> AddParameter(
			this EvaluationCatalog<double>.MutationCatalog catalog,
			IEvaluate<double> root, IOperator gene)
		{
			bool isFn = gene is IFunction;
			if (isFn)
			{
				// Functions with no other options?
				if (gene is SquareRootGene || gene is DivisionGene)
					return null;
			}

			var inputParamCount = root.Genes.OfType<Parameter>().GroupBy(p => p.ToString()).Count();
			return ApplyClone(root, gene, (g, newGenome) =>
			{
				var og = (IOperator)g;
				og.Add(GetParameterGene(RandomUtilities.Random.Next(inputParamCount + 1)));
			});
		}

		public static IEvaluate<double> BranchOperation(
			this EvaluationCatalog<double>.MutationCatalog catalog,
			IEvaluate<double> root, IOperator gene)
		{
			var inputParamCount = root.Genes.OfType<Parameter>().GroupBy(p => p.ToString()).Count();
			return ApplyClone(root, gene, (g, newGenome) =>
			{
				var n = GetParameterGene(RandomUtilities.Random.Next(inputParamCount));
				var newOp = Operators.GetRandomOperationGene();

				if (gene is IFunction || RandomUtilities.Random.Next(4) == 0)
				{
					var index = RandomUtilities.Random.Next(2);
					if (index == 1)
					{
						newOp.Add(n);
						newOp.Add(g);
					}
					else
					{
						newOp.Add(g);
						newOp.Add(n);
					}
					newGenome.Replace(g, newOp);
				}
				else
				{
					newOp.Add(n);
					// Useless to divide a param by itself, avoid...
					newOp.Add(GetParameterGene(RandomUtilities.Random.Next(inputParamCount)));

					((IOperator)g).Add(newOp);
				}

			});
		}

		public static IEvaluate<double> Square(
			this EvaluationCatalog<double>.MutationCatalog catalog,
			IEvaluate<double> root, IGene gene)
		{
			if (root.FindParent(gene) is SquareRootGene)
				return null;

			return ApplyClone(root, gene, (g, newGenome) =>
			{
				var newFn = new ProductGene(g.Modifier);
				g.Modifier = 1;
				newFn.Add(g);
				newFn.Add(g.Clone());
				newGenome.Replace(g, newFn);
			});
		}
	}
}
