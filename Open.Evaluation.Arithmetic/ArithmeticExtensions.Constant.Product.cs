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
	[return: NotNull]
	public static Constant<TValue> ProductOfConstants<TValue>(
		[DisallowNull, NotNull] this ICatalog<IEvaluate<TValue>> catalog,
		[DisallowNull, NotNull] in TValue c1,
		[DisallowNull, NotNull] IEnumerable<IConstant<TValue>> constants)
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

	[return: NotNull]
	public static Constant<TValue> ProductOfConstants<TValue>(
		[DisallowNull, NotNull] this ICatalog<IEvaluate<TValue>> catalog,
		[DisallowNull, NotNull] IEnumerable<IConstant<TValue>> constants)
		where TValue : notnull, IComparable<TValue>, IComparable, INumber<TValue>
		=> ProductOfConstants(catalog, TValue.MultiplicativeIdentity, constants);

	[return: NotNull]
	public static Constant<TValue> ProductOfConstants<TValue>(
		[DisallowNull, NotNull] this ICatalog<IEvaluate<TValue>> catalog,
		[DisallowNull, NotNull] in IConstant<TValue> c1,
		[DisallowNull, NotNull] in IConstant<TValue> c2,
		params IConstant<TValue>[] rest)
		where TValue : notnull, INumber<TValue>
	{
		catalog.ThrowIfNull().OnlyInDebug();
		c1.ThrowIfNull().OnlyInDebug();
		c1.ThrowIfNull().OnlyInDebug();
		Contract.EndContractBlock();

		return ProductOfConstants(catalog, c1.Value, rest.Prepend(c2));
	}

	[return: NotNull]
	public static Constant<TValue> ProductOfConstants<TValue>(
		[DisallowNull, NotNull] this ICatalog<IEvaluate<TValue>> catalog,
		[DisallowNull, NotNull] in TValue c1,
		[DisallowNull, NotNull] IConstant<TValue> c2,
		params IConstant<TValue>[] rest)
		where TValue : notnull, INumber<TValue>
		=> ProductOfConstants(catalog, c1, rest.Prepend(c2));

	[return: NotNull]
	public static IEnumerable<(string Hash, IConstant<TResult>? Multiple, IEvaluate<TResult> Entry)> MultiplesExtracted<TResult>(
		[DisallowNull, NotNull] this ICatalog<IEvaluate<TResult>> catalog,
		[DisallowNull, NotNull] IEnumerable<IEvaluate<TResult>> source, bool reduce = false)
		where TResult : notnull, INumber<TResult>
	{
		foreach (var c in source)
		{
			if (c is not Product<TResult> p)
			{
				yield return (c.Description, default(IConstant<TResult>?), c);
				continue;
			}

			var reduced = reduce
				? p.ReductionWithMutlipleExtracted(catalog, out var multiple)
				: p.ExtractMultiple(catalog, out multiple);

			yield return (
				reduced.Description,
				multiple,
				reduced
			);
		}
	}

	[return: NotNull]
	public static IEvaluate<TResult> ProductOf<TResult>(
		[DisallowNull, NotNull] this ICatalog<IEvaluate<TResult>> catalog,
		[DisallowNull, NotNull] IEnumerable<IEvaluate<TResult>> children)
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

	[return: NotNull]
	public static IEvaluate<TResult> ProductOf<TResult>(
		[DisallowNull, NotNull] this ICatalog<IEvaluate<TResult>> catalog,
		[DisallowNull, NotNull] IEvaluate<TResult> multiple,
		[DisallowNull, NotNull] IEnumerable<IEvaluate<TResult>> children)
		where TResult : notnull, INumber<TResult>
		=> ProductOf(catalog, children.Append(multiple));

	[return: NotNull]
	public static IEvaluate<TResult> ProductOf<TResult>(
		[DisallowNull, NotNull] this ICatalog<IEvaluate<TResult>> catalog,
		[DisallowNull, NotNull] IEvaluate<TResult> child1,
		[DisallowNull, NotNull] IEvaluate<TResult> child2,
		params IEvaluate<TResult>[] moreChildren)
		where TResult : notnull, INumber<TResult>
		=> ProductOf(catalog, moreChildren.Prepend(child2).Prepend(child1));

	[return: NotNull]
	public static IEvaluate<TResult> ProductOf<TResult>(
		[DisallowNull, NotNull] this ICatalog<IEvaluate<TResult>> catalog,
		[DisallowNull, NotNull] in TResult multiple,
		[DisallowNull, NotNull] IEnumerable<IEvaluate<TResult>> children)
		where TResult : notnull, INumber<TResult>
		=> ProductOf(catalog, catalog.GetConstant(multiple), children);

	[return: NotNull]
	public static IEvaluate<TResult> ProductOf<TResult>(
		[DisallowNull, NotNull] this ICatalog<IEvaluate<TResult>> catalog,
		[DisallowNull, NotNull] in TResult multiple,
		[DisallowNull, NotNull] IEvaluate<TResult> first,
		params IEvaluate<TResult>[] rest)
		where TResult : notnull, INumber<TResult>
		=> ProductOf(catalog, rest.Prepend(first).Prepend(catalog.GetConstant(multiple)));

	[return: NotNull]
	public static IEvaluate<TResult> ProductOfSum<TResult>(
		[DisallowNull, NotNull] this ICatalog<IEvaluate<TResult>> catalog,
		[DisallowNull, NotNull] IEvaluate<TResult> multiple,
		[DisallowNull, NotNull] Sum<TResult> sum)
		where TResult : notnull, INumber<TResult>
		=> multiple is Sum<TResult> m
		? ProductOfSums(catalog, m, sum)
		: catalog.GetReduced(catalog.SumOf(sum.Children.Select(c => ProductOf(catalog, multiple, c))));

	[return: NotNull]
	public static IEvaluate<TResult> ProductOfSums<TResult>(
		[DisallowNull, NotNull] this ICatalog<IEvaluate<TResult>> catalog,
		[DisallowNull, NotNull] Sum<TResult> a,
		[DisallowNull, NotNull] Sum<TResult> b)
		where TResult : notnull, INumber<TResult>
		=> catalog.GetReduced(catalog.SumOf(a.Children.Select(c => ProductOfSum(catalog, c, b))));

	[return: NotNull]
	public static IEvaluate<TResult> ProductOfSums<TResult>(
		[DisallowNull, NotNull] this ICatalog<IEvaluate<TResult>> catalog,
		[DisallowNull, NotNull] IReadOnlyCollection<Sum<TResult>> sums)
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
