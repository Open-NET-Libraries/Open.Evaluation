using Open.Evaluation.Arithmetic;
using Open.Evaluation.Core;
using Open.Hierarchy;
using Open.Numeric;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;

namespace Open.Evaluation.Catalogs
{
	using IFunction = IFunction<double>;
	using IOperator = IOperator<IEvaluate<double>, double>;

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

			var n = node;
			var isRoot = n == n.Root;
			// ReSharper disable once ImplicitlyCapturedClosure
			bool parentIsSquareRoot() => !isRoot && n.Parent.Value is Exponent<double> ex && ex.IsSquareRoot();

			// ReSharper disable once AccessToModifiedClosure
			var modifier = new Lazy<double>(() => catalog.Catalog.GetMultiple(n.Value));

			try
			{
				switch (RandomUtilities.Random.Next(options))
				{
					case 0:
						// Alter Sign
						var result = catalog.Catalog.MultiplyNode(n, -1);
						if (!parentIsSquareRoot()) return result;

						n = catalog.Factory.Map(result);
						// Sorry, not gonna mess with unreal (sqrt neg numbers yet).
						if (RandomUtilities.Random.Next(2) == 0)
							goto case 1;

						goto case 2;

					case 1:
						// Don't zero the root or make the internal multiple negative.
						if (isRoot && modifier.Value == +1 || parentIsSquareRoot() && modifier.Value <= 0)
							goto case 2;

						// Decrease multiple.
						return catalog.Catalog.AdjustNodeMultiple(n, -1);

					case 2:
						// Don't zero the root. (makes no sense)
						if (isRoot && modifier.Value == -1)
							goto case 1;
						// Increase multiple.
						return catalog.Catalog.AdjustNodeMultiple(n, +1);
				}
			}
			finally
			{
				if (n != node) n.Recycle();
			}

			throw new ArgumentOutOfRangeException(nameof(options));

		}

		public static IEvaluate<double> MutateParameter(
			this EvaluationCatalog<double>.MutationCatalog catalog,
			Node<IEvaluate<double>> node)
		{
			if (!(node.Value is IParameter p))
				throw new ArgumentException("Does not contain a Parameter.", nameof(node));

			return catalog.Catalog.ApplyClone(node, newNode =>
			{
				var rv = node.Root.Value;
				var nextParameter = RandomUtilities.NextRandomIntegerExcluding(
					p == rv ? p.ID : (((IParent)rv).GetDescendants().OfType<IParameter>().Distinct().Count()) + 1,
					p.ID);

				newNode.Value = catalog.Catalog.GetParameter(nextParameter);
			});
		}

		public static IEvaluate<double> ChangeOperation(
			this EvaluationCatalog<double>.MutationCatalog catalog,
			Node<IEvaluate<double>> node)
		{
			if (!(node.Value is IOperator o))
				throw new ArgumentException("Does not contain an Operation.", nameof(node));

			var symbol = o.Symbol;
			var isFn = Registry.Arithmetic.Functions.Contains(symbol);
			if (isFn)
			{
				// Functions with no other options?
				if (Registry.Arithmetic.Functions.Count < 2)
					return null;
			}
			else
			{
				// Never will happen, but logic states that this is needed.
				if (Registry.Arithmetic.Operators.Count < 2)
					return null;
			}

			var c = catalog.Catalog;
			return c.ApplyClone(node, _ => isFn
				? Registry.Arithmetic.GetRandomFunction(c, o.Children.ToArray(), symbol)
				: Registry.Arithmetic.GetRandomOperator(c, o.Children, symbol));
		}

		public static IEvaluate<double> AddParameter(
			this EvaluationCatalog<double>.MutationCatalog catalog,
			Node<IEvaluate<double>> node)
		{
			switch (node.Value)
			{
				case Exponent<double> _:
					return null;
				case IParent p:
					return catalog.Catalog.ApplyClone(node, newNode =>
						newNode.AddValue(catalog.Catalog.GetParameter(
							RandomUtilities.Random.Next(
								p.GetDescendants().OfType<IParameter>().Distinct().Count() + 1))));

				default:
					throw new ArgumentException("Invalid node type for adding a paremeter.", nameof(node));
			}
		}

		public static IEvaluate<double> BranchOperation(
			this EvaluationCatalog<double>.MutationCatalog catalog,
			Node<IEvaluate<double>> node)
		{
			var rv = node.Root.Value;
			var inputParamCount = rv is IParent p ? p.GetDescendants().OfType<IParameter>().Distinct().Count() : rv is IParameter ? 1 : 0;
			return catalog.Catalog.ApplyClone(node, newNode =>
			{
				var parameter = catalog.Catalog.GetParameter(RandomUtilities.Random.Next(inputParamCount));
				IEvaluate<double>[] children;

				var nv = newNode.Value;
				if (newNode.Value is IFunction || RandomUtilities.Random.Next(4) == 0)
				{
					children = RandomUtilities.Random.Next(2) == 1
						? new[] { parameter, nv }
						: new[] { nv, parameter };
				}
				else
				{
					children = new[] { parameter, nv };
				}

				return Registry.Arithmetic.GetRandomOperator(catalog, children);
			});
		}

		public static IEvaluate<double> Square(
			this EvaluationCatalog<double>.MutationCatalog catalog,
			Node<IEvaluate<double>> node)
			=> catalog.Catalog.ApplyClone(node, newNode =>
			{
				if (!(node.Value is Exponent<double>))
					return catalog.Factory.Map(catalog.Catalog.GetExponent(node.Value, 2));

				var power = newNode.Children[1];
				newNode.Replace(power,
					catalog.Factory.Map(catalog.Catalog.ProductOf(2, power.Value)));
				return newNode;
			});
	}
}
