using Open.Disposable;
using Open.Evaluation.Core;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Numerics;
using Throw;

namespace Open.Evaluation.Arithmetic;
public static partial class ArithmeticExtensions
{
	public static Constant<TValue> ProductOfConstants<TValue>(
		this ICatalog<IEvaluate<TValue>> catalog,
		[DisallowNull] in TValue c1,
		IEnumerable<IConstant<TValue>> constants)
		where TValue : notnull, IComparable<TValue>, IComparable, INumber<TValue>
	{
		catalog.ThrowIfNull().OnlyInDebug();
		constants.ThrowIfNull().OnlyInDebug();
		Contract.EndContractBlock();

		if (TValue.IsNaN(c1))
			return catalog.GetConstant(c1);

		if (c1 == TValue.Zero)
			return catalog.GetConstant(TValue.Zero);

		var result = c1;
		// ReSharper disable once PossibleMultipleEnumeration
		foreach (var c in constants)
		{
			var val = c.Value;
			if (TValue.IsNaN(val))
				return catalog.GetConstant(val);

			if (val == TValue.Zero)
				return catalog.GetConstant(TValue.Zero);

			result *= val;
		}

		return catalog.GetConstant(result);
	}

	public static Constant<TValue> ProductOfConstants<TValue>(
		this ICatalog<IEvaluate<TValue>> catalog,
		IEnumerable<IConstant<TValue>> constants)
		where TValue : notnull, IComparable<TValue>, IComparable, INumber<TValue>
		=> ProductOfConstants(catalog, TValue.MultiplicativeIdentity, constants);

	public static Constant<TValue> ProductOfConstants<TValue>(
		this ICatalog<IEvaluate<TValue>> catalog,
		in IConstant<TValue> c1,
		in IConstant<TValue> c2,
		params IConstant<TValue>[] rest)
		where TValue : notnull, INumber<TValue>
	{
		catalog.ThrowIfNull().OnlyInDebug();
		c1.ThrowIfNull().OnlyInDebug();
		c1.ThrowIfNull().OnlyInDebug();
		Contract.EndContractBlock();

		return ProductOfConstants(catalog, c1.Value, rest.Prepend(c2));
	}

	public static Constant<TValue> ProductOfConstants<TValue>(
		this ICatalog<IEvaluate<TValue>> catalog,
		[DisallowNull] in TValue c1,
		IConstant<TValue> c2,
		params IConstant<TValue>[] rest)
		where TValue : notnull, INumber<TValue>
		=> ProductOfConstants(catalog, c1, rest.Prepend(c2));

	public static IEnumerable<(string Hash, IConstant<TResult>? Multiple, IEvaluate<TResult> Entry)> MultiplesExtracted<TResult>(
		this ICatalog<IEvaluate<TResult>> catalog,
		IEnumerable<IEvaluate<TResult>> source, bool reduce = false)
		where TResult : notnull, INumber<TResult>
	{
		foreach (var c in source)
		{
			if (c is not Product<TResult> p)
			{
				yield return (c.Description.Value, default(IConstant<TResult>?), c);
				continue;
			}

			var reduced = reduce
				? p.ReductionWithMutlipleExtracted(catalog, out var multiple)
				: p.ExtractMultiple(catalog, out multiple);

			yield return (
				reduced.Description.Value,
				multiple,
				reduced
			);
		}
	}

	public static IEvaluate<TResult> ProductOf<TResult>(
		this ICatalog<IEvaluate<TResult>> catalog,
		IEnumerable<IEvaluate<TResult>> children)
		where TResult : notnull, INumber<TResult>
	{
		catalog.ThrowIfNull();
		if (children is IReadOnlyCollection<IEvaluate<TResult>> ch && ch.Count == 0)
			throw new NotSupportedException("Cannot produce a product of an empty set.");

		using var childListRH = ListPool<IEvaluate<TResult>>.Rent();
		var childList = childListRH.Item;
		childList.AddRange(children);
		if (childList.Count == 0)
			throw new NotSupportedException("Cannot produce a product of an empty set.");
		var constants = childList.ExtractType<IConstant<TResult>>();

		if (constants.Count > 0)
		{
			var c = constants.Count == 1
				? constants[0]
				: catalog.ProductOfConstants(constants);

			if (childList.Count == 0)
				return c;

			var cValue = c.Value;
			if (TResult.IsNaN(cValue))
				return c;

			if(cValue != TResult.MultiplicativeIdentity)
				childList.Add(c);
		}

		switch (childList.Count)
		{
			case 0:
				Debug.Fail("Extraction failure.", "Should not have occured.");
				throw new InvalidOperationException("Extraction failure.");
			case 1:
				return childList[0];
			default:
				return Product<TResult>.Create(catalog, childList);
		}
	}

	public static IEvaluate<TResult> ProductOf<TResult>(
		this ICatalog<IEvaluate<TResult>> catalog,
		IEvaluate<TResult> multiple,
		IEnumerable<IEvaluate<TResult>> children)
		where TResult : notnull, INumber<TResult>
		=> ProductOf(catalog, children.Append(multiple));

	public static IEvaluate<TResult> ProductOf<TResult>(
		this ICatalog<IEvaluate<TResult>> catalog,
		IEvaluate<TResult> child1,
		IEvaluate<TResult> child2,
		params IEvaluate<TResult>[] moreChildren)
		where TResult : notnull, INumber<TResult>
		=> ProductOf(catalog, moreChildren.Prepend(child2).Prepend(child1));

	public static IEvaluate<TResult> ProductOf<TResult>(
		this ICatalog<IEvaluate<TResult>> catalog,
		[DisallowNull] in TResult multiple,
		IEnumerable<IEvaluate<TResult>> children)
		where TResult : notnull, INumber<TResult>
		=> ProductOf(catalog, catalog.GetConstant(multiple), children);

	public static IEvaluate<TResult> ProductOf<TResult>(
		this ICatalog<IEvaluate<TResult>> catalog,
		[DisallowNull] in TResult multiple,
		IEvaluate<TResult> first,
		params IEvaluate<TResult>[] rest)
		where TResult : notnull, INumber<TResult>
		=> ProductOf(catalog, rest.Prepend(first).Prepend(catalog.GetConstant(multiple)));

	public static IEvaluate<TResult> ProductOfSum<TResult>(
		this ICatalog<IEvaluate<TResult>> catalog,
		IEvaluate<TResult> multiple,
		Sum<TResult> sum)
		where TResult : notnull, INumber<TResult>
		=> multiple is Sum<TResult> m
		? ProductOfSums(catalog, m, sum)
		: catalog.GetReduced(catalog.SumOf(sum.Children.Select(c => ProductOf(catalog, multiple, c))));

	public static IEvaluate<TResult> ProductOfSums<TResult>(
		this ICatalog<IEvaluate<TResult>> catalog,
		Sum<TResult> a,
		Sum<TResult> b)
		where TResult : notnull, INumber<TResult>
		=> catalog.GetReduced(catalog.SumOf(a.Children.Select(c => ProductOfSum(catalog, c, b))));

	public static IEvaluate<TResult> ProductOfSums<TResult>(
		this ICatalog<IEvaluate<TResult>> catalog,
		IReadOnlyCollection<Sum<TResult>> sums)
		where TResult : notnull, INumber<TResult>
	{
		if (sums.Count == 0) return catalog.GetConstant((TResult)(dynamic)1);
		using var e = sums.GetEnumerator();
		if (!e.MoveNext()) throw new NotSupportedException("Collection empty with count > 0.");
		IEvaluate<TResult> p = e.Current;
		while (e.MoveNext()) p = ProductOfSum(catalog, p, e.Current);
		return catalog.GetReduced(p);
	}
}
