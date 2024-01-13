/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Disposable;
using Open.Evaluation.Core;
using Open.Numeric.Primes;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using Throw;

namespace Open.Evaluation.Arithmetic;

public partial class Sum<TResult> :
	ArithmeticOperatorBase<TResult>
	where TResult : notnull, INumber<TResult>
{
	protected Sum(IEnumerable<IEvaluate<TResult>> children)
		: base(Symbols.Sum, children, true) { }

	protected override EvaluationResult<TResult> EvaluateInternal(Context context)
	{
		Children.Length.Throw("Cannot resolve sum of empty set.").IfEquals(0);

		var descs = new List<Lazy<string>>();
		var result = TResult.AdditiveIdentity;
		foreach (var r in ChildResults(context))
		{
			result += r.Result;
			descs.Add(r.Description);
		}

		return new(result, Describe(descs));
	}

	static bool IsProductWithSingleConstant(
		IEvaluate<TResult> a,
		[NotNullWhen(true)] out IConstant<TResult> value)
	{
		if (a is Product<TResult> aP)
		{
			var count = 0;
			IConstant<TResult>? v = default;
			foreach (var c in aP.Children.OfType<IConstant<TResult>>())
			{
				v = c;
				if (++count != 1) break;
			}
			if (count == 1)
			{
				value = v!;
				return true;
			}
		}

		value = default!;
		return false;
	}

	public override int Compare(IEvaluate<TResult>? x, IEvaluate<TResult>? y)
	{
		if (x is null) return y is null ? 0 : -1;
		if (y is null) return +1;

		var aFound = IsProductWithSingleConstant(x, out var aConstant);
		var bFound = IsProductWithSingleConstant(y, out var bConstant);
		if (aFound && bFound)
		{
			var result = base.Compare(aConstant, bConstant);
			if (result != 0) return result;
		}
		else if (aFound)
		{
			if (TResult.Zero > aConstant.Value)
				return +1;
		}
		else if (bFound)
		{
			if (TResult.Zero > bConstant.Value)
				return -1;
		}

		return base.Compare(x, y);
	}

	[GeneratedRegex("^\\(-(\\d+)(\\s*[*/]\\s*)(.+)\\)$|^-(\\d+)$", RegexOptions.Compiled)]
	private static partial Regex HasNegativeMultiplePattern();
	private static readonly Regex HasNegativeMultiple = HasNegativeMultiplePattern();

	protected override void ToStringInternal_OnAppendNextChild(StringBuilder result, int index, Lazy<string> child)
	{
		Debug.Assert(result is not null);
		if (index != 0)
		{
			var c = child.Value;
			var m = HasNegativeMultiple.Match(c);
			if (m.Success)
			{
				result.Append(" - ");
				result.Append(m.Groups[4].Success
					? m.Groups[4].Value
					: m.Groups[1].Value == "1"
						? $"({m.Groups[3].Value})"
						: $"({m.Groups[1].Value}{m.Groups[2].Value}{m.Groups[3].Value})");

				return;
			}
		}

		base.ToStringInternal_OnAppendNextChild(result, index, child);
	}

	protected override IEvaluate<TResult> Reduction(
		ICatalog<IEvaluate<TResult>> catalog)
	{
		catalog.ThrowIfNull().OnlyInDebug();
		var zero = catalog.GetConstant(TResult.Zero);

		// Phase 1: Flatten sums of sums.
		var children = catalog.Flatten<Sum<TResult>>(Children.Select(a =>
		{
			// Check for products that can be flattened as well.
			if (a is not Product<TResult> aP || aP.Children.Length != 2) return a;

			var aS = aP.Children.OfType<Sum<TResult>>().ToArray();
			if (aS.Length != 1) return a;

			var aC = aP.Children.OfType<IConstant<TResult>>().ToArray();
			if (aC.Length != 1) return a;

			var aCv = aC[0];
			return catalog.SumOf(aS[0].Children.Select(c => catalog.ProductOf(aCv, c)));
		})).Where(c => c != zero).ToList(); // ** children's reduction is done here.

		// Phase 2: Can we collapse?
		switch (children.Count)
		{
			case 0:
				return catalog.GetConstant(TResult.Zero);
			case 1:
				return children[0];
		}

		// Check for NaN.
		foreach (var child in children.OfType<IConstant<TResult>>())
		{
			var c = child.Value;
#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable CS1718 // Comparison made to same variable
			if (c != c) return catalog.GetConstant(c);
#pragma warning restore IDE0079 // Remove unnecessary suppression
#pragma warning restore CS1718 // Comparison made to same variable
		}

		var one = catalog.GetConstant(TResult.One);

		// Phase 3: Look for groupings by "multiples".
		var withMultiples = catalog.MultiplesExtracted(children, true).ToArray();

		// Phase 4: Replace multipliable products with single merged version.
		return catalog.SumOf(
			withMultiples
				.GroupBy(g => g.Hash)
				.OrderBy(g => g.Key) // Ensure consistency.
				.Select(g => (
					multiple: catalog.SumOfConstants(g.Select(t => t.Multiple ?? one)),
					first: g.First().Entry
				))
				.Where(i => i.multiple != zero)
				.Select(i => i.multiple == one
					? i.first
					: catalog.GetReduced(catalog.ProductOf(i.multiple, i.first))
				));
	}

	internal static Sum<TResult> Create(
		ICatalog<IEvaluate<TResult>> catalog,
		IEnumerable<IEvaluate<TResult>> param)
	{
		catalog.ThrowIfNull();
		param.ThrowIfNull();
		Contract.EndContractBlock();

		return catalog.Register(new Sum<TResult>(param));
	}

	internal virtual IEvaluate<TResult> NewUsing(
		ICatalog<IEvaluate<TResult>> catalog,
		IReadOnlyList<IEvaluate<TResult>> param)
		=> param.Count == 1 ? param[0] : Create(catalog, param);

	public override IEvaluate<TResult> NewUsing(
		ICatalog<IEvaluate<TResult>> catalog,
		IEnumerable<IEvaluate<TResult>> param)
		=> param is IReadOnlyList<IEvaluate<TResult>> p
		? NewUsing(catalog, p)
		: ConditionalTransform(param, p => Create(catalog, p));

	public bool TryExtractGreatestFactor(
		ICatalog<IEvaluate<TResult>> catalog,
		[NotNullWhen(true)] out IEvaluate<TResult> sum,
		[NotNullWhen(true)] out IConstant<TResult> greatestFactor)
	{
		catalog.ThrowIfNull().OnlyInDebug();
		Contract.EndContractBlock();

		var one = catalog.GetConstant(TResult.One);
		greatestFactor = one;
		sum = this;
		// Phase 5: Try and group by GCF:
		using var productsLease = ListPool<Product<TResult>>.Rent();
		foreach (var c in Children)
		{
			// All of them must be products for GCF to work.
			if (c is Product<TResult> p)
				productsLease.Item.Add(p);
			else
				return false;
		}

		// Try and get all the constants, and if a product does not have one, then done.
		using var constantLease = ListPool<TResult>.Rent();
		var constants = constantLease.Item;
		foreach (var p in productsLease.Item)
		{
			using var c = p.Children.OfType<IConstant<TResult>>().GetEnumerator();
			if (c.MoveNext()) // At least 1. OK.
			{
				var e = c.Current;
				if (!c.MoveNext()) // More than 1? Abort.
				{
					constants.Add(e.Value);
					continue;
				}
			}

			return false;
		}

		// Convert all the constants to factors, and if any are invalid for factoring, then done.
		using var factorsLease = ListPool<TResult>.Rent();
		foreach (var v in constants)
		{
			var d = TResult.Abs(v);
			if (d <= TResult.One || !d.IsInteger()) return false;
			factorsLease.Item.Add(d);
		}
		constantLease.Dispose();

		var gcf = Prime.GreatestFactor(factorsLease.Item);
		Debug.Assert(factorsLease.Item.All(f => f >= gcf));
		factorsLease.Dispose();
		if (gcf <= TResult.One) return false;

		greatestFactor = catalog.GetConstant(gcf);
		sum = catalog
			.SumOf(catalog.MultiplesExtracted(productsLease.Item)
			.Select(e =>
			{
				var m = e.Multiple ?? one;
				return m != one && tryGetReducedFactor(m.Value, out var f)
					? catalog.ProductOf(in f, e.Entry)
					: e.Entry;
			}));

		return true;

		bool tryGetReducedFactor(TResult value, out TResult f)
		{
			var r = value / gcf;
			f = r;
			return r != TResult.One;
		}
	}
}

public static class Sum
{
	internal static Sum<TResult> Create<TResult>(
		ICatalog<IEvaluate<TResult>> catalog,
		IEnumerable<IEvaluate<TResult>> param)
		where TResult : notnull, INumber<TResult>
		=> Sum<TResult>.Create(catalog, param);

	static IEvaluate<TResult> SumOfCollection<TResult>(
		ICatalog<IEvaluate<TResult>> catalog,
		List<IEvaluate<TResult>> childList)
		where TResult : notnull, INumber<TResult>
	{
		catalog.ThrowIfNull().OnlyInDebug();
		childList.ThrowIfNull().OnlyInDebug();

		var constants = childList.ExtractType<IConstant<TResult>>();

		if (constants.Count == 0)
			return Create(catalog, childList);

		var c = constants.Count == 1
			? constants[0]
			: catalog.SumOfConstants(constants);

		ListPool<IConstant<TResult>>.Shared.Give(constants);

		if (childList.Count == 0)
			return c;

		childList.Add(c);

		return Create(catalog, childList);
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
		in TResult multiple,
		IEvaluate<TResult> child,
		params IEvaluate<TResult>[] moreChildren)
		where TResult : notnull, INumber<TResult>
		=> SumOf(catalog, moreChildren.Prepend(child).Prepend(catalog.GetConstant(multiple)));

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