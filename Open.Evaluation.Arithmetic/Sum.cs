/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

namespace Open.Evaluation.Arithmetic;

public partial class Sum<T>
	: ArithmeticOperatorBase<T>
	where T : notnull, INumber<T>
{
	protected Sum(ICatalog<IEvaluate<T>> catalog, IEnumerable<IEvaluate<T>> children)
		: base(catalog, Symbols.Sum, children, true) { }

	protected override EvaluationResult<T> EvaluateInternal(Context context)
	{
		Children.Length.Throw("Cannot resolve sum of empty set.").IfEquals(0);

		var descs = new List<Lazy<string>>();
		var result = T.AdditiveIdentity;
		foreach (var r in ChildResults(context))
		{
			result += r.Result;
			descs.Add(r.Description);
		}

		return new(result, Describe(descs));
	}

	static bool IsProductWithSingleConstant(
		IEvaluate<T> a,
		[NotNullWhen(true)] out IConstant<T> value)
	{
		if (a is Product<T> aP)
		{
			var count = 0;
			IConstant<T>? v = default;
			foreach (var c in aP.Children.OfType<IConstant<T>>())
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

	public override int Compare(IEvaluate<T>? x, IEvaluate<T>? y)
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
			if (T.Zero > aConstant.Value)
				return +1;
		}
		else if (bFound)
		{
			if (T.Zero > bConstant.Value)
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

	protected override IEvaluate<T> Reduction(
		ICatalog<IEvaluate<T>> catalog)
	{
		catalog.ThrowIfNull().OnlyInDebug();
		var zero = catalog.GetConstant(T.Zero);

		// Phase 1: Flatten sums of sums.
		var children = catalog
			.Flatten(Children
				.Select(a =>
				{
					// Check for products that can be flattened as well.
					if (a is not Product<T> aP || aP.Children.Length != 2) return a;

					var aS = aP.Children.OfType<Sum<T>>().ToArray();
					if (aS.Length != 1) return a;

					var aC = aP.Children.OfType<IConstant<T>>().ToArray();
					if (aC.Length != 1) return a;

					var aCv = aC[0];
					return catalog.SumOf(aS[0].Children.Select(c => catalog.ProductOf(aCv, c)));
				}), parent => parent is Sum<T>)
				.Where(c => c != zero)
				.ToList(); // ** children's reduction is done here.

		// Phase 2: Can we collapse?
		switch (children.Count)
		{
			case 0:
				return catalog.GetConstant(T.Zero);
			case 1:
				return children[0];
		}

		// Check for NaN.
		foreach (var child in children.OfType<IConstant<T>>())
		{
			var c = child.Value;
			if (c.IsNaN()) return catalog.GetConstant(c);
		}

		var one = catalog.GetConstant(T.One);

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

	internal static Sum<T> Create(
		ICatalog<IEvaluate<T>> catalog,
		IEnumerable<IEvaluate<T>> param)
	{
		catalog.ThrowIfNull().OnlyInDebug();
		param.ThrowIfNull().OnlyInDebug();
		Contract.EndContractBlock();

		return catalog.Register(new Sum<T>(catalog, param));
	}

	internal virtual IEvaluate<T> NewUsing(
		ICatalog<IEvaluate<T>> catalog,
		IReadOnlyList<IEvaluate<T>> param)
		=> param.Count == 1 ? param[0] : Create(catalog, param);

	public override IEvaluate<T> NewUsing(
		ICatalog<IEvaluate<T>> catalog,
		IEnumerable<IEvaluate<T>> param)
		=> param is IReadOnlyList<IEvaluate<T>> p
		? NewUsing(catalog, p)
		: ConditionalTransform(param, p => Create(catalog, p));

	public bool TryExtractGreatestFactor(
		ICatalog<IEvaluate<T>> catalog,
		[NotNullWhen(true)] out IEvaluate<T> sum,
		[NotNullWhen(true)] out IConstant<T> greatestFactor)
	{
		catalog.ThrowIfNull().OnlyInDebug();
		Contract.EndContractBlock();

		var one = catalog.GetConstant(T.One);
		greatestFactor = one;
		sum = this;
		// Phase 5: Try and group by GCF:
		using var productsLease = ListPool<Product<T>>.Rent();
		foreach (var c in Children)
		{
			// All of them must be products for GCF to work.
			if (c is Product<T> p)
				productsLease.Item.Add(p);
			else
				return false;
		}

		// Try and get all the constants, and if a product does not have one, then done.
		using var constantLease = ListPool<T>.Rent();
		var constants = constantLease.Item;
		foreach (var p in productsLease.Item)
		{
			using var c = p.Children.OfType<IConstant<T>>().GetEnumerator();
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
		using var factorsLease = ListPool<T>.Rent();
		foreach (var v in constants)
		{
			var d = T.Abs(v);
			if (d <= T.One || !d.IsInteger()) return false;
			factorsLease.Item.Add(d);
		}
		constantLease.Dispose();

		var gcf = Prime.GreatestFactor(factorsLease.Item);
		Debug.Assert(factorsLease.Item.All(f => f >= gcf));
		factorsLease.Dispose();
		if (gcf <= T.One) return false;

		greatestFactor = catalog.GetConstant(gcf);
		sum = catalog
			.SumOf(catalog.MultiplesExtracted(productsLease.Item)
			.Select(e =>
			{
				var m = e.Multiple ?? one;
				return m != one && TryGetReducedFactor(m.Value, out var f)
					? catalog.ProductOf(in f, e.Entry)
					: e.Entry;
			}));

		return true;

		bool TryGetReducedFactor(T value, out T f)
		{
			var r = value / gcf;
			f = r;
			return r != T.One;
		}
	}
}

public static class Sum
{
	internal static Sum<T> Create<T>(
		ICatalog<IEvaluate<T>> catalog,
		IEnumerable<IEvaluate<T>> param)
		where T : notnull, INumber<T>
		=> Sum<T>.Create(catalog, param);

	static IEvaluate<T> SumOfCollection<T>(
		ICatalog<IEvaluate<T>> catalog,
		List<IEvaluate<T>> childList)
		where T : notnull, INumber<T>
	{
		catalog.ThrowIfNull().OnlyInDebug();
		childList.ThrowIfNull().OnlyInDebug();

		var constants = childList.ExtractType<IConstant<T>>();

		if (constants.Count == 0)
			return Create(catalog, childList);

		var c = constants.Count == 1
			? constants[0]
			: catalog.SumOfConstants(constants);

		ListPool<IConstant<T>>.Shared.Give(constants);

		if (childList.Count == 0)
			return c;

		childList.Add(c);

		return Create(catalog, childList);
	}

	public static IEvaluate<T> SumOf<T>(
		this ICatalog<IEvaluate<T>> catalog,
		IReadOnlyList<IEvaluate<T>> children)
		where T : notnull, INumber<T>
	{
		catalog.ThrowIfNull().OnlyInDebug();
		children.ThrowIfNull().OnlyInDebug();
		Contract.EndContractBlock();

		switch (children.Count)
		{
			case 0:
				return catalog.GetConstant(T.Zero);

			case 1:
				return children[0];

			default:
			{
				using var childListRH = ListPool<IEvaluate<T>>.Rent();
				var childList = childListRH.Item;
				childList.AddRange(children);
				return SumOfCollection(catalog, childList);
			}
		}
	}

	public static IEvaluate<T> SumOf<T>(
		this ICatalog<IEvaluate<T>> catalog,
		IEnumerable<IEvaluate<T>> children)
		where T : notnull, INumber<T>
	{
		catalog.ThrowIfNull().OnlyInDebug();
		children.ThrowIfNull().OnlyInDebug();
		Contract.EndContractBlock();

		if (children is IReadOnlyList<IEvaluate<T>> ch)
			return SumOf(catalog, ch);

		using var e = children.GetEnumerator();
		if (!e.MoveNext()) return catalog.GetConstant(T.Zero);
		var v0 = e.Current;
		if (!e.MoveNext()) return v0;

		using var childListRH = ListPool<IEvaluate<T>>.Rent();
		var childList = childListRH.Item;
		childList.Add(v0);
		do { childList.Add(e.Current); }
		while (e.MoveNext());
		return SumOfCollection(catalog, childList);
	}

	public static IEvaluate<T> SumOf<T>(
		this ICatalog<IEvaluate<T>> catalog,
		IEvaluate<T> child1,
		IEvaluate<T> child2,
		params IEvaluate<T>[] moreChildren)
		where T : notnull, INumber<T>
		=> SumOf(catalog, moreChildren.Prepend(child2).Prepend(child1));

	public static IEvaluate<T> SumOf<T>(
		this ICatalog<IEvaluate<T>> catalog,
		in T multiple,
		IEvaluate<T> child,
		params IEvaluate<T>[] moreChildren)
		where T : notnull, INumber<T>
		=> SumOf(catalog, moreChildren.Prepend(child).Prepend(catalog.GetConstant(multiple)));

	public static Constant<T> SumOfConstants<T>(
		this ICatalog<IEvaluate<T>> catalog,
		in T c1,
		IEnumerable<IConstant<T>> constants)
		where T : notnull, INumber<T>
	{
		catalog.ThrowIfNull().OnlyInDebug();
		constants.ThrowIfNull().OnlyInDebug();
		Contract.EndContractBlock();

		if (T.IsNaN(c1))
			return catalog.GetConstant(c1);

		var result = c1;
		// ReSharper disable once PossibleMultipleEnumeration
		// ReSharper disable once LoopCanBeConvertedToQuery
		foreach (var c in constants)
		{
			var val = c.Value;
			if (T.IsNaN(val))
				return catalog.GetConstant(val);

			result += val;
		}
		return catalog.GetConstant(result);
	}

	public static Constant<T> SumOfConstants<T>(
		this ICatalog<IEvaluate<T>> catalog,
		IEnumerable<IConstant<T>> constants)
		where T : notnull, INumber<T>
		=> SumOfConstants(catalog, T.AdditiveIdentity, constants);

	public static Constant<T> SumOfConstants<T>(
		this ICatalog<IEvaluate<T>> catalog,
		in T c1, in IConstant<T> c2,
		params IConstant<T>[] rest)
		where T : notnull, IComparable<T>, IComparable, INumber<T>
		=> SumOfConstants(catalog, c1, rest.Prepend(c2));

	public static Constant<T> SumOfConstants<T>(
		this ICatalog<IEvaluate<T>> catalog,
		in IConstant<T> c1,
		in IConstant<T> c2,
		params IConstant<T>[] rest)
		where T : notnull, IComparable<T>, IComparable, INumber<T>
	{
		c1.ThrowIfNull().OnlyInDebug();
		c2.ThrowIfNull().OnlyInDebug();
		Contract.EndContractBlock();

		return SumOfConstants(catalog, c1.Value, rest.Prepend(c2));
	}
}