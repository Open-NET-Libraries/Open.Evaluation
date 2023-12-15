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
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using Throw;

namespace Open.Evaluation.Arithmetic;

public partial class Product<TResult> :
	OperatorBase<TResult>,
	IReproducable<IEnumerable<IEvaluate<TResult>>, IEvaluate<TResult>>
	where TResult : notnull, INumber<TResult>
{
	protected Product(IEnumerable<IEvaluate<TResult>> children)
		: base(Symbols.Product, children, true, 2) { }

	protected Product(IEvaluate<TResult> first, params IEvaluate<TResult>[] rest)
		: this(Enumerable.Repeat(first, 1).Concat(rest)) { }

	protected override int ConstantPriority => -1;

	[return: NotNull]
	protected override TResult EvaluateInternal(object context)
		=> Children.Length == 0	? throw new NotSupportedException("Cannot resolve product of empty set.")
		: (TResult)ChildResults(context)
			.Aggregate<TResult, dynamic>(1, (current, r) => current * r);

	protected override IEvaluate<TResult> Reduction(
		ICatalog<IEvaluate<TResult>> catalog)
	{
		catalog.ThrowIfNull().OnlyInDebug();
		var one = catalog.GetConstant(TResult.MultiplicativeIdentity);

		using var lease = ListPool<IEvaluate<TResult>>.Shared.Rent();
		// Phase 1: Flatten products of products.
		var children = lease.Item;
		children.AddRange(catalog
			.Flatten<Product<TResult>>(Children)
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
			if (multipleValue != TResult.MultiplicativeIdentity && Math.Floor(multipleValue) == multipleValue)
			{
				var oneNeg = catalog.GetConstant(-TResult.MultiplicativeIdentity);
				var muValue = (long)multipleValue;
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

					var divisor = Convert.ToDouble(expC.Value, CultureInfo.InvariantCulture);
					// ReSharper disable once CompareOfFloatsByEqualityOperator

					// We won't mess with factional divisors yet.
					if (Math.Floor(divisor) != divisor)
						continue;

					var d = (long)divisor;
					if (muValue % d == 0)
					{
						muValue /= d;
						children[i] = one;
					}
					else
					{
						var f = 1L;
						// We might have a potential divisor...
						foreach (var factor in Prime.Factors(d, true))
						{
							if (factor > muValue) break;
							if (muValue % factor != 0) continue;
							Debug.Assert(factor != 0);
							muValue /= factor;
							f *= factor;
						}

						if (f != 1L)
						{
							children[i] = catalog.GetExponent(
								catalog.GetConstant(d / f),
								catalog.GetConstant(-TResult.MultiplicativeIdentity));
						}
					}

					if (muValue == 1)
						break;
				}

				if (muValue != originalMultiple)
					children[multipleIndex] = catalog.GetConstant((TResult)(dynamic)muValue);
			}
		}

		return children.Count == 1
				? children[0]
				: catalog.ProductOf(children);
	}

	[GeneratedRegex("^\\(1/(.+)\\)$", RegexOptions.Compiled)]
	private static partial Regex IsInvertedPattern();

	protected override void ToStringInternal_OnAppendNextChild(StringBuilder result, int index, object child)
	{
		Debug.Assert(result is not null);
		if (index != 0 && child is string c)
		{
			var m = IsInvertedPattern().Match(c);
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
