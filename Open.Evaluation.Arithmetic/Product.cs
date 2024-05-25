/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

namespace Open.Evaluation.Arithmetic;

public partial class Product<TResult> :
	OperatorBase<TResult>,
	IReproducable<IEnumerable<IEvaluate<TResult>>, IEvaluate<TResult>>
	where TResult : notnull, INumber<TResult>
{
	protected Product(IEnumerable<IEvaluate<TResult>> children)
		: base(Symbols.Product, children, true, 2) { }

	protected Product(IEvaluate<TResult> first, params IEvaluate<TResult>[] rest)
		: this([first, ..rest]) { }

	protected override int ConstantPriority => -1;

	protected override EvaluationResult<TResult> EvaluateInternal(Context context)
	{
		Children.Length.Throw("Cannot resolve boolean of empty set.").IfEquals(0);

		var results = ChildResults(context).Memoize();

		var result = TResult.MultiplicativeIdentity;
		foreach (var e in results)
		{
			var r = e.Result;
			if (TResult.IsZero(r))
			{
				result = TResult.Zero;
				break;
			}

			// Check for NaN.
			if (r.IsNaN())
			{
				result = r;
				break;
			}

			result *= r;
		}

		return new(
			result,
			Describe(results.Select(r => r.Description)));
	}

	protected override IEvaluate<TResult> Reduction(
		ICatalog<IEvaluate<TResult>> catalog)
	{
		catalog.ThrowIfNull().OnlyInDebug();
		var one = catalog.GetConstant(TResult.MultiplicativeIdentity);

		using var lease = ListPool<IEvaluate<TResult>>.Shared.Rent();
		// Phase 1: Flatten products of products.
		var children = lease.Item;
		children.AddRange(catalog
			.Flatten(Children, static parent => parent is Product<TResult>)
			.Where(c => c != one)); // ** children's reduction is done here.

		// Phase 3: Try to extract common multiples...
		var len = children.Count;
		for (var i = 0; i < len; i++)
		{
			var child = children[i];
			if (child is not Sum<TResult> sum ||
				!sum.TryExtractGreatestFactor(catalog, out var newSum, out var gcf))
			{
				continue;
			}

			children[i] = newSum; // Replace with extraction...
			children.Add(gcf); // Append the constant..
		}

		// Phase 3: Can we collapse?
		switch (children.Count)
		{
			case 0:
				if (Children.Length == 0)
					throw new NotSupportedException("Cannot reduce product of empty set.");
				return one;

			case 1:
				return children[0];
		}

		// Phase 4: Deal with special case constants.
		IConstant<TResult>? zero = null;
		foreach (var c in children.OfType<IConstant<TResult>>())
		{
			var cValue = c.Value;
			if (TResult.IsNaN(cValue)) return c;
			if (TResult.IsZero(cValue)) zero = c;
		}

		if (zero is not null)
			return zero;

		var cat = catalog;
		// Phase 5: Convert to exponents.
		using var lease2 = ListPool<(IEvaluate<TResult> Base, IEvaluate<TResult> Power)>.Shared.Rent();
		var exponents = lease2.Item;
		exponents.AddRange(children.Select(c =>
			c is Exponent<TResult> e
			? (Base: cat.GetReduced(e.Base), e.Power)
			: (Base: c, Power: one)));

		zero = exponents
			.Select(e => e.Base)
			.OfType<IConstant<TResult>>()
			.FirstOrDefault(c => TResult.IsZero(c.Value));

		if (zero is not null)
			return zero;

		children.Clear();
		children.AddRange(exponents
			.Where(e => e.Power != zero)
			.GroupBy(e => e.Base.Description)
			.Select(g =>
			{
				var @base = g.First().Base;
				var sumPower = cat.SumOf(g.Select(t => t.Power));
				var power = cat.GetReduced(sumPower);
				return power == zero ? one
					: power == one ? @base
					: GetExponent(catalog, @base, power);
			}));
		lease2.Dispose(); // release early.

		var multiple = children
			.OfType<IConstant<TResult>>()
			.FirstOrDefault();

		if (multiple is not null)
		{
			var multipleValue = multiple.Value;
			// ReSharper disable once InvertIf
			if (multipleValue != TResult.MultiplicativeIdentity
				&& multipleValue % TResult.One == TResult.Zero)
			{
				var oneNeg = catalog.GetConstant(-TResult.MultiplicativeIdentity);
				var muValue = multipleValue;
				var originalMultiple = muValue;
				var multipleIndex = children.IndexOf(multiple);
				var count = children.Count;
				for (var i = 0; i < count; i++)
				{
					// Let's start with simple division of constants.
					if (children[i] is not Exponent<TResult> ex
						|| ex.Power != oneNeg
						|| ex.Base is not IConstant<TResult> expC)
					{
						continue;
					}

					var divisor = expC.Value;
					// ReSharper disable once CompareOfFloatsByEqualityOperator

					// We won't mess with factional divisors yet.
					if (divisor % TResult.One != TResult.Zero)
						continue;

					if (muValue % divisor == TResult.Zero)
					{
						muValue /= divisor;
						children[i] = one;
					}
					else
					{
						var f = TResult.One;
						// We might have a potential divisor...
						foreach (var factor in Prime.Factors(divisor, true))
						{
							if (factor > muValue) break;
							if (muValue % factor != TResult.Zero) continue;
							Debug.Assert(factor != TResult.Zero);
							muValue /= factor;
							f *= factor;
						}

						if (f != TResult.One)
						{
							children[i] = catalog.GetExponent(
								catalog.GetConstant(divisor / f),
								catalog.GetConstant(-TResult.MultiplicativeIdentity));
						}
					}

					if (muValue == TResult.One)
						break;
				}

				if (muValue != originalMultiple)
					children[multipleIndex] = catalog.GetConstant(muValue);
			}
		}

		return children.Count == 1
				? children[0]
				: catalog.ProductOf(children);
	}

	[GeneratedRegex("^\\(1/(.+)\\)$", RegexOptions.Compiled)]
	private static partial Regex IsInvertedPattern();

	protected override void ToStringInternal_OnAppendNextChild(StringBuilder result, int index, Lazy<string> child)
	{
		Debug.Assert(result is not null);
		if (index != 0)
		{
			var m = IsInvertedPattern().Match(child.Value);
			if (m.Success)
			{
				result.Append(" / ");
				result.Append(m.Groups[1].Value);
				return;
			}
		}

		base.ToStringInternal_OnAppendNextChild(result, index, child);
	}

	protected virtual Exponent<TResult> GetExponent(ICatalog<IEvaluate<TResult>> catalog,
		IEvaluate<TResult> baseValue,
		IEvaluate<TResult> power)
		=> Exponent<TResult>.Create(catalog, baseValue, power);

	public IEvaluate<TResult> ExtractMultiple(ICatalog<IEvaluate<TResult>> catalog, out IConstant<TResult>? multiple)
	{
		multiple = null;

		if (!Children.OfType<IConstant<TResult>>().Any()) return this;

		var children = Children.ToList(); // Make a copy to be worked on...
		var constants = children.ExtractType<IConstant<TResult>>();
		if (constants.Count == 0) return this;

		multiple = catalog.ProductOfConstants(constants);
		return NewUsing(catalog, children);
	}

	public IEvaluate<TResult> ReductionWithMutlipleExtracted(ICatalog<IEvaluate<TResult>> catalog, out IConstant<TResult>? multiple)
	{
		multiple = null;
		Debug.Assert(catalog is not null);
		var reduced = catalog!.GetReduced(this);
		return reduced is Product<TResult> product
			? product.ExtractMultiple(catalog, out multiple)
			: reduced;
	}

	static bool IsExponentWithConstantPower(
		IEvaluate<TResult> a,
		[NotNullWhen(true)] out IConstant<TResult> value)
	{
		if (a is Exponent<TResult> aP && aP.Power is IConstant<TResult> c)
		{
			value = c;
			return true;
		}

		value = default!;
		return false;
	}

	public override int Compare(IEvaluate<TResult>? x, IEvaluate<TResult>? y)
	{
		if (x is null) return y is null ? 0 : -1;
		if (y is null) return +1;

		// Constants always get priority in products and are moved to the front.  They should collapse in reduction to a 'multiple'.
		if (x is IConstant<TResult> || y is IConstant<TResult>)
			return base.Compare(x, y);

		var aFound = IsExponentWithConstantPower(x, out var aConstant);
		var bFound = IsExponentWithConstantPower(y, out var bConstant);
		if (aFound && bFound)
		{
			var result = base.Compare(aConstant, bConstant);
			if (result != 0) return result;
		}
		else if (aFound)
		{
			if (TResult.MultiplicativeIdentity > aConstant.Value)
				return +1;
			if (TResult.MultiplicativeIdentity < aConstant.Value)
				return -1;
		}
		else if (bFound)
		{
			if (TResult.MultiplicativeIdentity > bConstant.Value)
				return -1;
			if (TResult.MultiplicativeIdentity < bConstant.Value)
				return +1;
		}

		return base.Compare(x, y);
	}

	internal static Product<TResult> Create(
		ICatalog<IEvaluate<TResult>> catalog,
		IEnumerable<IEvaluate<TResult>> param)
	{
		catalog.ThrowIfNull();
		param.ThrowIfNull();
		Contract.EndContractBlock();

		return catalog.Register(new Product<TResult>(param));
	}

	public virtual IEvaluate<TResult> NewUsing(
		ICatalog<IEvaluate<TResult>> catalog,
		IReadOnlyList<IEvaluate<TResult>> param)
	{
		param.ThrowIfNull();
		return param.Count == 1 ? param[0] : Create(catalog, param);
	}

	public virtual IEvaluate<TResult> NewUsing(
		ICatalog<IEvaluate<TResult>> catalog,
		IEnumerable<IEvaluate<TResult>> param)
		=> param is IReadOnlyList<IEvaluate<TResult>> p
		? NewUsing(catalog, p)
		: ConditionalTransform(param, p => Create(catalog, p));
}

public static class Product
{
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

			if (cValue != TResult.MultiplicativeIdentity)
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
		if (sums.Count == 0) return catalog.GetConstant(TResult.One);
		using var e = sums.GetEnumerator();
		if (!e.MoveNext()) throw new NotSupportedException("Collection empty with count > 0.");
		IEvaluate<TResult> p = e.Current;
		while (e.MoveNext()) p = ProductOfSum(catalog, p, e.Current);
		return catalog.GetReduced(p);
	}

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
}
