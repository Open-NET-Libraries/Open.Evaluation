using Open.Disposable;
using Open.Evaluation.Core;
using System.Diagnostics.Contracts;
using System.Numerics;
using Throw;

namespace Open.Evaluation.Arithmetic;
public static partial class ArithmeticExtensions
{
	static IEvaluate<TResult> SumOfCollection<TResult>(
		ICatalog<IEvaluate<TResult>> catalog,
		List<IEvaluate<TResult>> childList)
		where TResult : notnull, INumber<TResult>
	{
		catalog.ThrowIfNull().OnlyInDebug();
		childList.ThrowIfNull().OnlyInDebug();

		var constants = childList.ExtractType<IConstant<TResult>>();

		if (constants.Count == 0)
			return Sum<TResult>.Create(catalog, childList);

		var c = constants.Count == 1
			? constants[0]
			: catalog.SumOfConstants(constants);

		ListPool<IConstant<TResult>>.Shared.Give(constants);

		if (childList.Count == 0)
			return c;

		childList.Add(c);

		return Sum<TResult>.Create(catalog, childList);
	}

	public static IEvaluate<TResult> SumOf<TResult>(
		this ICatalog<IEvaluate<TResult>> catalog,
		IReadOnlyList<IEvaluate<TResult>> children)
		where TResult : notnull, INumber<TResult>
	{
		catalog.ThrowIfNull().OnlyInDebug();
		children.ThrowIfNull().OnlyInDebug();
		Contract.EndContractBlock();

		switch (children.Count)
		{
			case 0:
				return catalog.GetConstant(TResult.Zero);

			case 1:
				return children[0];

			default:
			{
				using var childListRH = ListPool<IEvaluate<TResult>>.Rent();
				var childList = childListRH.Item;
				childList.AddRange(children);
				return SumOfCollection(catalog, childList);
			}
		}
	}

	public static IEvaluate<TResult> SumOf<TResult>(
		this ICatalog<IEvaluate<TResult>> catalog,
		IEnumerable<IEvaluate<TResult>> children)
		where TResult : notnull, INumber<TResult>
	{
		catalog.ThrowIfNull().OnlyInDebug();
		children.ThrowIfNull().OnlyInDebug();
		Contract.EndContractBlock();

		if (children is IReadOnlyList<IEvaluate<TResult>> ch)
			return SumOf(catalog, ch);

		using var e = children.GetEnumerator();
		if (!e.MoveNext()) return catalog.GetConstant(TResult.Zero);
		var v0 = e.Current;
		if (!e.MoveNext()) return v0;

		using var childListRH = ListPool<IEvaluate<TResult>>.Rent();
		var childList = childListRH.Item;
		childList.Add(v0);
		do { childList.Add(e.Current); }
		while (e.MoveNext());
		return SumOfCollection(catalog, childList);
	}

	public static IEvaluate<TResult> SumOf<TResult>(
		this ICatalog<IEvaluate<TResult>> catalog,
		IEvaluate<TResult> child1,
		IEvaluate<TResult> child2,
		params IEvaluate<TResult>[] moreChildren)
		where TResult : notnull, INumber<TResult>
		=> SumOf(catalog, moreChildren.Prepend(child2).Prepend(child1));

	public static IEvaluate<TResult> SumOf<TResult>(
		this ICatalog<IEvaluate<TResult>> catalog,
		in TResult value,
		IEvaluate<TResult> child,
		params IEvaluate<TResult>[] moreChildren)
		where TResult : notnull, INumber<TResult>
		=> SumOf(catalog, moreChildren.Prepend(child).Prepend(catalog.GetConstant(value)));

	public static Constant<TValue> SumOfConstants<TValue>(
		this ICatalog<IEvaluate<TValue>> catalog,
		in TValue c1,
		IEnumerable<IConstant<TValue>> constants)
		where TValue : notnull, INumber<TValue>
	{
		catalog.ThrowIfNull().OnlyInDebug();
		constants.ThrowIfNull().OnlyInDebug();
		Contract.EndContractBlock();

		if (TValue.IsNaN(c1))
			return catalog.GetConstant(c1);

		var result = c1;
		// ReSharper disable once PossibleMultipleEnumeration
		// ReSharper disable once LoopCanBeConvertedToQuery
		foreach (var c in constants)
		{
			var val = c.Value;
			if (TValue.IsNaN(val))
				return catalog.GetConstant(val);

			result += val;
		}
		return catalog.GetConstant(result);
	}

	public static Constant<TValue> SumOfConstants<TValue>(
		this ICatalog<IEvaluate<TValue>> catalog,
		IEnumerable<IConstant<TValue>> constants)
		where TValue : notnull, INumber<TValue>
		=> SumOfConstants(catalog, TValue.AdditiveIdentity, constants);

	public static Constant<TValue> SumOfConstants<TValue>(
		this ICatalog<IEvaluate<TValue>> catalog,
		in TValue c1, in IConstant<TValue> c2,
		params IConstant<TValue>[] rest)
		where TValue : notnull, IComparable<TValue>, IComparable, INumber<TValue>
		=> SumOfConstants(catalog, c1, rest.Prepend(c2));

	public static Constant<TValue> SumOfConstants<TValue>(
		this ICatalog<IEvaluate<TValue>> catalog,
		in IConstant<TValue> c1,
		in IConstant<TValue> c2,
		params IConstant<TValue>[] rest)
		where TValue : notnull, IComparable<TValue>, IComparable, INumber<TValue>
	{
		c1.ThrowIfNull().OnlyInDebug();
		c2.ThrowIfNull().OnlyInDebug();
		Contract.EndContractBlock();

		return SumOfConstants(catalog, c1.Value, rest.Prepend(c2));
	}
}
