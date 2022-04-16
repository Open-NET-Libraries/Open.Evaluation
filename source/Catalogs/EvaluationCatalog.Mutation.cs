using Open.Evaluation.Arithmetic;
using Open.Evaluation.Core;
using Open.Hierarchy;
using Open.RandomizationExtensions;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;

using IFunction = Open.Evaluation.Core.IFunction<double>;
using IOperator = Open.Evaluation.Core.IOperator<Open.Evaluation.Core.IEvaluate<double>, double>;

namespace Open.Evaluation.Catalogs;

[SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "For type inference.")]
public partial class EvaluationCatalog<T>
	where T : IComparable
{
	private MutationCatalog? _mutation;
	public MutationCatalog Mutation =>
		LazyInitializer.EnsureInitialized(ref _mutation, () => new MutationCatalog(this))!;

	public class MutationCatalog : SubmoduleBase<EvaluationCatalog<T>>
	{
		internal MutationCatalog(EvaluationCatalog<T> source) : base(source)
		{
		}
	}
}

public static partial class EvaluationCatalogExtensions
{
	const string CannotOperatePowerNullValue = "Cannot operate when the power.Value is null.";

	public static IEvaluate<double> MutateSign(
		this EvaluationCatalog<double>.MutationCatalog catalog,
		Node<IEvaluate<double>> node, byte options = 3)
	{
		if (catalog is null) throw new ArgumentNullException(nameof(catalog));
		if (node is null) throw new ArgumentNullException(nameof(node));
		if (options > 3) throw new ArgumentOutOfRangeException(nameof(options));
		Contract.EndContractBlock();

		var n = node;
		var isRoot = n == n.Root;
		Debug.Assert(!isRoot || n.Parent is null);
		// ReSharper disable once ImplicitlyCapturedClosure
		bool parentIsSquareRoot() => !isRoot && n.Parent?.Value is Exponent<double> ex && ex.IsSquareRoot();

		// ReSharper disable once AccessToModifiedClosure
		var modifier = new Lazy<double>(() => catalog.Catalog.GetMultiple(n.Value));

		try
		{
			switch (Randomizer.Random.Next(options))
			{
				case 0:
					// Alter Sign
					var result = catalog.Catalog.MultiplyNode(n, -1);

					// Sorry, not gonna mess with unreal (sqrt neg numbers yet).
					if (!parentIsSquareRoot()) return result;

					n = catalog.Factory.Map(result);
					if (Randomizer.Random.Next(2) == 0)
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
		if (catalog is null)
			throw new ArgumentNullException(nameof(catalog));
		if (node?.Value is null)
			throw new ArgumentException("No node value.", nameof(node));
		if (node.Value is not IParameter p)
			throw new ArgumentException("Does not contain a Parameter.", nameof(node));
		Contract.EndContractBlock();

		return catalog.Catalog.ApplyClone(node, _ =>
		{
			var rv = node.Root.Value;
			var nextParameter = Randomizer.Random.NextExcluding(
				p == rv
					? p.ID
					: ((IParent)rv!).GetDescendants().OfType<IParameter>().Distinct().Count()
						+ (p.ID == 0 ? 1 : Randomizer.Random.Next(2)) /* Increase the possibility of parameter ID decrease vs increase */,
				p.ID);

			return catalog.Catalog.GetParameter(nextParameter);
		});
	}

	public static IEvaluate<double>? ChangeOperation(
		this EvaluationCatalog<double>.MutationCatalog catalog,
		Node<IEvaluate<double>> node)
	{
		if (catalog is null)
			throw new ArgumentNullException(nameof(catalog));
		if (node is null)
			throw new ArgumentNullException(nameof(node));

		if (node.Value is not IOperator o)
			throw new ArgumentException("Does not contain an Operation.", nameof(node));

		var symbol = o.Symbol;
		var isFn = Registry.Arithmetic.Functions.Contains(symbol);
		if (isFn)
		{
			// Functions with no other options?
			if (Registry.Arithmetic.Functions.Length < 2)
			{
				if (node.Count < 2)
					return null;
				isFn = false;
			}
		}

		if (!isFn)
		{
			// Never will happen, but logic states that this is needed.
			if (Registry.Arithmetic.Operators.Length < 2)
				return null;
		}

		var c = catalog.Catalog;
		return c.ApplyClone(node, _ => isFn
			? Registry.Arithmetic.GetRandomFunction(c, o.Children.ToArray(), symbol)!
			: Registry.Arithmetic.GetRandomOperator(c, o.Children, symbol)!);
	}

	public static IEvaluate<double>? AddParameter(
		this EvaluationCatalog<double>.MutationCatalog catalog,
		Node<IEvaluate<double>> node)
	{
		if (catalog is null) throw new ArgumentNullException(nameof(catalog));
		if (node is null) throw new ArgumentNullException(nameof(node));
		Contract.EndContractBlock();

		return node.Value switch
		{
			Exponent<double> _ => null,

			IParent p => catalog.Catalog.ApplyClone(node,
				newNode => newNode.AddValue(catalog.Catalog.GetParameter(
					Randomizer.Random.Next(
						p.GetDescendants().OfType<IParameter>().Distinct().Count() + 1)))),

			_ => throw new ArgumentException("Invalid node type for adding a paremeter.", nameof(node)),
		};
	}

	public static IEvaluate<double> BranchOperation(
		this EvaluationCatalog<double>.MutationCatalog catalog,
		Node<IEvaluate<double>> node)
	{
		if (catalog is null) throw new ArgumentNullException(nameof(catalog));
		if (node is null) throw new ArgumentNullException(nameof(node));
		Contract.EndContractBlock();

		return catalog.Catalog.ApplyClone(node, (catalog, node), (newNode, param) =>
		{
			var (catalog, node) = param;
			var rv = node.Root.Value;
			var inputParamCount = rv is IParent p ? p.GetDescendants().OfType<IParameter>().Distinct().Count() : rv is IParameter ? 1 : 0;
			var parameter = catalog.Catalog.GetParameter(Randomizer.Random.Next(inputParamCount));
			IEvaluate<double>[] children;

			var nv = newNode.Value ?? throw new NotSupportedException(CannotOperateNewNodeNullValue);
			children
				= newNode.Value is IFunction || Randomizer.Random.Next(4) == 0
				? Randomizer.Random.Next(2) == 1
					? new IEvaluate<double>[] { parameter, nv }
					: new IEvaluate<double>[] { nv, parameter }
				: (new[] { parameter, nv });

			return Registry.Arithmetic.GetRandomOperator(catalog, children)!; // Will throw in ApplyClone if null.
		});
	}

	public static IEvaluate<double> AdjustExponent(
		this EvaluationCatalog<double>.MutationCatalog catalog,
		Node<IEvaluate<double>> node, double value)
	{
		if (catalog is null) throw new ArgumentNullException(nameof(catalog));
		if (node is null) throw new ArgumentNullException(nameof(node));
		if (value == 0) throw new ArgumentException("A value of zero will have no effect.", nameof(value));
		Contract.EndContractBlock();

		return node.Value is Exponent<double>
			? catalog.Catalog.ApplyClone(node, newNode =>
			{
				var power = newNode.Children[1];
				newNode.Replace(power,
					catalog.Factory.Map(catalog.Catalog.SumOf(in value, power.Value ?? throw new NotSupportedException(CannotOperatePowerNullValue))));
			})
			: catalog.Catalog.ApplyClone(node, newNode =>
					catalog.Catalog.GetExponent(newNode.Value ?? throw new NotSupportedException(CannotOperateNewNodeNullValue), 1 + value));
	}

	public static IEvaluate<double> Square(
		this EvaluationCatalog<double>.MutationCatalog catalog,
		Node<IEvaluate<double>> node)
	{
		if (catalog is null) throw new ArgumentNullException(nameof(catalog));
		if (node is null) throw new ArgumentNullException(nameof(node));
		Contract.EndContractBlock();

		return node.Value is Exponent<double>
			? catalog.Catalog.ApplyClone(node, newNode =>
			{
				var power = newNode.Children[1];
				newNode.Replace(power,
					catalog.Factory.Map(catalog.Catalog.ProductOf(2, power.Value ?? throw new NotSupportedException(CannotOperatePowerNullValue))));
			})
			: catalog.Catalog.ApplyClone(node, newNode =>
					catalog.Catalog.GetExponent(newNode.Value ?? throw new NotSupportedException(CannotOperateNewNodeNullValue), 2));
	}
}
