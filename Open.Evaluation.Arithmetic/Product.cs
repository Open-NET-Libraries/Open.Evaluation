/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

namespace Open.Evaluation.Arithmetic;

public partial class Product<T> :
	OperatorBase<T>,
	IReproducable<IEnumerable<IEvaluate<T>>, IEvaluate<T>>
	where T : notnull, INumber<T>
{
	protected Product(IEnumerable<IEvaluate<T>> children)
		: base(Symbols.Product, children, true, 2) { }

	protected Product(IEvaluate<T> first, params IEvaluate<T>[] rest)
		: this([first, ..rest]) { }

	protected override int ConstantPriority => -1;

	protected override EvaluationResult<T> EvaluateInternal(Context context)
	{
		Children.Length.Throw("Cannot resolve boolean of empty set.").IfEquals(0);

		var results = ChildResults(context).Memoize();

		var result = T.MultiplicativeIdentity;
		foreach (var e in results)
		{
			var r = e.Result;
			if (T.IsZero(r))
			{
				result = T.Zero;
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

	protected override IEvaluate<T> Reduction(
		ICatalog<IEvaluate<T>> catalog)
	{
		catalog.ThrowIfNull().OnlyInDebug();
		var one = catalog.GetConstant(T.MultiplicativeIdentity);

		using var lease = ListPool<IEvaluate<T>>.Shared.Rent();
		// Phase 1: Flatten products of products.
		var children = lease.Item;
		children.AddRange(catalog
			.Flatten(Children, static parent => parent is Product<T>)
			.Where(c => c != one)); // ** children's reduction is done here.

		// Phase 3: Try to extract common multiples...
		var len = children.Count;
		for (var i = 0; i < len; i++)
		{
			var child = children[i];
			if (child is not Sum<T> sum ||
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
		IConstant<T>? zero = null;
		foreach (var c in children.OfType<IConstant<T>>())
		{
			var cValue = c.Value;
			if (T.IsNaN(cValue)) return c;
			if (T.IsZero(cValue)) zero = c;
		}

		if (zero is not null)
			return zero;

		var cat = catalog;
		// Phase 5: Convert to exponents.
		using var lease2 = ListPool<(IEvaluate<T> Base, IEvaluate<T> Power)>.Shared.Rent();
		var exponents = lease2.Item;
		exponents.AddRange(children.Select(c =>
			c is Exponent<T> e
			? (Base: cat.GetReduced(e.Base), e.Power)
			: (Base: c, Power: one)));

		zero = exponents
			.Select(e => e.Base)
			.OfType<IConstant<T>>()
			.FirstOrDefault(c => T.IsZero(c.Value));

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
			.OfType<IConstant<T>>()
			.FirstOrDefault();

		if (multiple is not null)
		{
			var multipleValue = multiple.Value;
			// ReSharper disable once InvertIf
			if (multipleValue != T.MultiplicativeIdentity
				&& multipleValue % T.One == T.Zero)
			{
				var oneNeg = catalog.GetConstant(-T.MultiplicativeIdentity);
				var muValue = multipleValue;
				var originalMultiple = muValue;
				var multipleIndex = children.IndexOf(multiple);
				var count = children.Count;
				for (var i = 0; i < count; i++)
				{
					// Let's start with simple division of constants.
					if (children[i] is not Exponent<T> ex
						|| ex.Power != oneNeg
						|| ex.Base is not IConstant<T> expC)
					{
						continue;
					}

					var divisor = expC.Value;
					// ReSharper disable once CompareOfFloatsByEqualityOperator

					// We won't mess with factional divisors yet.
					if (divisor % T.One != T.Zero)
						continue;

					if (muValue % divisor == T.Zero)
					{
						muValue /= divisor;
						children[i] = one;
					}
					else
					{
						var f = T.One;
						// We might have a potential divisor...
						foreach (var factor in Prime.Factors(divisor, true))
						{
							if (factor > muValue) break;
							if (muValue % factor != T.Zero) continue;
							Debug.Assert(factor != T.Zero);
							muValue /= factor;
							f *= factor;
						}

						if (f != T.One)
						{
							children[i] = catalog.GetExponent(
								catalog.GetConstant(divisor / f),
								catalog.GetConstant(-T.MultiplicativeIdentity));
						}
					}

					if (muValue == T.One)
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

	protected virtual Exponent<T> GetExponent(ICatalog<IEvaluate<T>> catalog,
		IEvaluate<T> baseValue,
		IEvaluate<T> power)
		=> Exponent<T>.Create(catalog, baseValue, power);

	public IEvaluate<T> ExtractMultiple(ICatalog<IEvaluate<T>> catalog, out IConstant<T>? multiple)
	{
		multiple = null;

		if (!Children.OfType<IConstant<T>>().Any()) return this;

		var children = Children.ToList(); // Make a copy to be worked on...
		var constants = children.ExtractType<IConstant<T>>();
		if (constants.Count == 0) return this;

		multiple = catalog.ProductOfConstants(constants);
		return NewUsing(catalog, children);
	}

	public IEvaluate<T> ReductionWithMutlipleExtracted(ICatalog<IEvaluate<T>> catalog, out IConstant<T>? multiple)
	{
		multiple = null;
		Debug.Assert(catalog is not null);
		var reduced = catalog!.GetReduced(this);
		return reduced is Product<T> product
			? product.ExtractMultiple(catalog, out multiple)
			: reduced;
	}

	static bool IsExponentWithConstantPower(
		IEvaluate<T> a,
		[NotNullWhen(true)] out IConstant<T> value)
	{
		if (a is Exponent<T> aP && aP.Power is IConstant<T> c)
		{
			value = c;
			return true;
		}

		value = default!;
		return false;
	}

	public override int Compare(IEvaluate<T>? x, IEvaluate<T>? y)
	{
		if (x is null) return y is null ? 0 : -1;
		if (y is null) return +1;

		// Constants always get priority in products and are moved to the front.  They should collapse in reduction to a 'multiple'.
		if (x is IConstant<T> || y is IConstant<T>)
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
			if (T.MultiplicativeIdentity > aConstant.Value)
				return +1;
			if (T.MultiplicativeIdentity < aConstant.Value)
				return -1;
		}
		else if (bFound)
		{
			if (T.MultiplicativeIdentity > bConstant.Value)
				return -1;
			if (T.MultiplicativeIdentity < bConstant.Value)
				return +1;
		}

		return base.Compare(x, y);
	}

	internal static Product<T> Create(
		ICatalog<IEvaluate<T>> catalog,
		IEnumerable<IEvaluate<T>> param)
	{
		catalog.ThrowIfNull();
		param.ThrowIfNull();
		Contract.EndContractBlock();

		return catalog.Register(new Product<T>(param));
	}

	public virtual IEvaluate<T> NewUsing(
		ICatalog<IEvaluate<T>> catalog,
		IReadOnlyList<IEvaluate<T>> param)
	{
		param.ThrowIfNull();
		return param.Count == 1 ? param[0] : Create(catalog, param);
	}

	public virtual IEvaluate<T> NewUsing(
		ICatalog<IEvaluate<T>> catalog,
		IEnumerable<IEvaluate<T>> param)
		=> param is IReadOnlyList<IEvaluate<T>> p
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
