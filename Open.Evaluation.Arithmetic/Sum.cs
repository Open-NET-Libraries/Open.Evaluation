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
		T result = T.AdditiveIdentity;
		foreach (EvaluationResult<T> r in ChildResults(context))
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
            int count = 0;
			IConstant<T>? v = default;
			foreach (IConstant<T> c in aP.Children.OfType<IConstant<T>>())
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

        bool aFound = IsProductWithSingleConstant(x, out IConstant<T>? aConstant);
        bool bFound = IsProductWithSingleConstant(y, out IConstant<T>? bConstant);
		if (aFound && bFound)
		{
            int result = base.Compare(aConstant, bConstant);
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
            string c = child.Value;
            Match m = HasNegativeMultiple.Match(c);
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

	public override IEvaluate<T> GetReduction()
	{
        Constant<T> zero = Catalog.GetConstant(T.Zero);

		// Phase 1: Flatten sums of sums.
		var children = Catalog
			.Flatten(Children
				.Select(a =>
				{
					// Check for products that can be flattened as well.
					if (a is not Product<T> aP || aP.Children.Length != 2) return a;

                    Sum<T>[] aS = aP.Children.OfType<Sum<T>>().ToArray();
					if (aS.Length != 1) return a;

                    IConstant<T>[] aC = aP.Children.OfType<IConstant<T>>().ToArray();
					if (aC.Length != 1) return a;

                    IConstant<T> aCv = aC[0];
					return Catalog.SumOf(aS[0].Children.Select(c => Catalog.ProductOf(aCv, c)));
				}), parent => parent is Sum<T>)
				.Where(c => c != zero)
				.ToList(); // ** children's reduction is done here.

		// Undefined poisons: any undefined term makes the sum undefined. Checked before any
		// collapse or fold so nothing below can mask it.
		if (children.Exists(static c => c is IUndefined))
			return Catalog.GetUndefined();

		// Phase 2: Can we collapse?
		switch (children.Count)
		{
			case 0:
				return Catalog.GetConstant(T.Zero);
			case 1:
				return children[0];
		}

		// Check for NaN.
		foreach (IConstant<T> child in children.OfType<IConstant<T>>())
		{
			T c = child.Value;
			if (T.IsNaN(c)) return Catalog.GetConstant(c);
		}

        Constant<T> one = Catalog.GetConstant(T.One);

        // Phase 3: Look for groupings by "multiples".
        (string Hash, IConstant<T>? Multiple, IEvaluate<T> Entry)[] withMultiples = Catalog.MultiplesExtracted(children, true).ToArray();

		// Phase 4: Replace multipliable products with single merged version.
		return Catalog.SumOf(
			withMultiples
				.GroupBy(g => g.Hash)
				.OrderBy(g => g.Key) // Ensure consistency.
				.Select(g => (
					multiple: Catalog.SumOfConstants(g.Select(t => t.Multiple ?? one)),
					first: g.First().Entry
				))
				.Where(i => i.multiple != zero)
				.Select(i => i.multiple == one
					? i.first
					: Catalog.GetReduced(Catalog.ProductOf(i.multiple, i.first))
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

        Constant<T> one = catalog.GetConstant(T.One);
		greatestFactor = one;
		sum = this;
		// Phase 5: Try and group by GCF:
		using RecycleHelper<List<Product<T>>> productsLease = ListPool<Product<T>>.Rent();
		foreach (IEvaluate<T> c in Children)
		{
			// All of them must be products for GCF to work.
			if (c is Product<T> p)
				productsLease.Item.Add(p);
			else
				return false;
		}

		// Try and get all the constants, and if a product does not have one, then done.
		using RecycleHelper<List<T>> constantLease = ListPool<T>.Rent();
        List<T> constants = constantLease.Item;
		foreach (Product<T> p in productsLease.Item)
		{
			using IEnumerator<IConstant<T>> c = p.Children.OfType<IConstant<T>>().GetEnumerator();
			if (c.MoveNext()) // At least 1. OK.
			{
                IConstant<T> e = c.Current;
				if (!c.MoveNext()) // More than 1? Abort.
				{
					constants.Add(e.Value);
					continue;
				}
			}

			return false;
		}

		// Convert all the constants to factors, and if any are invalid for factoring, then done.
		using RecycleHelper<List<T>> factorsLease = ListPool<T>.Rent();
		foreach (T v in constants)
		{
			var d = T.Abs(v);
			if (d <= T.One || !d.IsInteger()) return false;
			factorsLease.Item.Add(d);
		}

		constantLease.Dispose();

		T gcf = Prime.GreatestFactor(factorsLease.Item);
		Debug.Assert(factorsLease.Item.All(f => f >= gcf));
		factorsLease.Dispose();
		if (gcf <= T.One) return false;

		greatestFactor = catalog.GetConstant(gcf);
		sum = catalog
			.SumOf(catalog.MultiplesExtracted(productsLease.Item)
			.Select(e =>
			{
                IConstant<T> m = e.Multiple ?? one;
				return m != one && TryGetReducedFactor(m.Value, out T? f)
					? catalog.ProductOf(in f, e.Entry)
					: e.Entry;
			}));

		return true;

		bool TryGetReducedFactor(T value, out T f)
		{
			T r = value / gcf;
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

		List<IConstant<T>> constants = childList.ExtractType<IConstant<T>>();

		if (constants.Count == 0)
			return Create(catalog, childList);

        IConstant<T> c = constants.Count == 1
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
				using RecycleHelper<List<IEvaluate<T>>> childListRH = ListPool<IEvaluate<T>>.Rent();
                    List<IEvaluate<T>> childList = childListRH.Item;
				childList.AddRange(children);
				return SumOfCollection(catalog, childList);
			}
		}
	}

	public static IEvaluate<T> SumOf<T>(
		this ICatalog<IEvaluate<T>> catalog,
		params IEnumerable<IEvaluate<T>> children)
		where T : notnull, INumber<T>
	{
		catalog.ThrowIfNull().OnlyInDebug();
		children.ThrowIfNull().OnlyInDebug();
		Contract.EndContractBlock();

		if (children is IReadOnlyList<IEvaluate<T>> ch)
			return SumOf(catalog, ch);

		using IEnumerator<IEvaluate<T>> e = children.GetEnumerator();
		if (!e.MoveNext()) return catalog.GetConstant(T.Zero);
        IEvaluate<T> v0 = e.Current;
		if (!e.MoveNext()) return v0;

		using RecycleHelper<List<IEvaluate<T>>> childListRH = ListPool<IEvaluate<T>>.Rent();
        List<IEvaluate<T>> childList = childListRH.Item;
		childList.Add(v0);
		do { childList.Add(e.Current); }
		while (e.MoveNext());
		return SumOfCollection(catalog, childList);
	}

	public static IEvaluate<T> SumOf<T>(
		this ICatalog<IEvaluate<T>> catalog,
		in T multiple,
		params IEnumerable<IEvaluate<T>> moreChildren)
		where T : notnull, INumber<T>
		=> SumOf(catalog, moreChildren.Prepend(catalog.GetConstant(multiple)));

	public static Constant<T> SumOfConstants<T>(
		this ICatalog<IEvaluate<T>> catalog,
		in T c1,
		params IEnumerable<IConstant<T>> constants)
		where T : notnull, INumber<T>
	{
		catalog.ThrowIfNull().OnlyInDebug();
		constants.ThrowIfNull().OnlyInDebug();
		Contract.EndContractBlock();

		if (T.IsNaN(c1))
			return catalog.GetConstant(c1);

		T result = c1;
		// ReSharper disable once PossibleMultipleEnumeration
		// ReSharper disable once LoopCanBeConvertedToQuery
		foreach (IConstant<T> c in constants)
		{
			T val = c.Value;
			if (T.IsNaN(val))
				return catalog.GetConstant(val);

			result += val;
		}

		return catalog.GetConstant(result);
	}

	public static Constant<T> SumOfConstants<T>(
		this ICatalog<IEvaluate<T>> catalog,
		params IEnumerable<IConstant<T>> constants)
		where T : notnull, INumber<T>
		=> SumOfConstants(catalog, T.AdditiveIdentity, constants);

	public static Constant<T> SumOfConstants<T>(
		this ICatalog<IEvaluate<T>> catalog,
		in T c1, in IConstant<T> c2,
		params IEnumerable<IConstant<T>> rest)
		where T : notnull, INumber<T>
		=> SumOfConstants(catalog, c1, rest.Prepend(c2));

	public static Constant<T> SumOfConstants<T>(
		this ICatalog<IEvaluate<T>> catalog,
		in IConstant<T> c1,
		in IConstant<T> c2,
		params IEnumerable<IConstant<T>> rest)
		where T : notnull, INumber<T>
	{
		c1.ThrowIfNull().OnlyInDebug();
		c2.ThrowIfNull().OnlyInDebug();
		Contract.EndContractBlock();

		return SumOfConstants(catalog, c1.Value, rest.Prepend(c2));
	}

	public static Constant<T> OfConstants<T>(
		in IConstant<T> c1,
		params IEnumerable<IConstant<T>> rest)
		where T : notnull, INumber<T>
	{
		c1.ThrowIfNull().OnlyInDebug();
		var catalog = c1.Catalog;
		catalog.AssertBelongs(rest);
		Contract.EndContractBlock();

		return SumOfConstants(catalog, rest.Prepend(c1));
	}
}