/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

using static Open.Evaluation.Arithmetic.Exponent;

namespace Open.Evaluation.Arithmetic;

// ReSharper disable once PossibleInfiniteInheritance
public class Exponent<T> : OperatorBase<T>,
	IReproducable<(IEvaluate<T>, IEvaluate<T>), IEvaluate<T>>
	where T : notnull, INumber<T>
{
	// Since zero to the power of zero can be undefined or zero, we can leave the formula intact instead of reducing it.
	private readonly PowerOfZeroReduction _powerOfZeroReduction;

	protected Exponent(
		ICatalog<IEvaluate<T>> catalog,
		IEvaluate<T> @base,
		IEvaluate<T> power,
		PowerOfZeroReduction powerOfZeroReduction = PowerOfZeroReduction.One)
		: base(catalog, Symbols.Exponent,
			  // Need to provide to children so a node tree can be built.
			  [@base, power])
	{
		@base.ThrowIfNull().OnlyInDebug();
		power.ThrowIfNull().OnlyInDebug();
		Base = @base;
		Power = power;
		_powerOfZeroReduction = powerOfZeroReduction;
	}

	public IEvaluate<T> Base { get; }

	public IEvaluate<T> Power { get; }

	protected override EvaluationResult<T> EvaluateInternal(Context context)
	{
        EvaluationResult<T> bas = Base.Evaluate(context);
        EvaluationResult<T> pow = Power.Evaluate(context);

		return new(bas.Result.Pow(pow.Result), Describe([bas.Description, pow.Description]));
	}

	protected Lazy<string> Describe(Lazy<string> bas, Lazy<string> pow)
		=> new(() =>
		{
            string b = bas.Value;
            string p = pow.Value;
			Debug.Assert(!string.IsNullOrWhiteSpace(b));
			Debug.Assert(!string.IsNullOrWhiteSpace(p));

			if(p == "-1")
				return $"(1/{b})";

            Match m = SquareRootPattern().Match(p);
			if (m.Success)
				return '√' + b;

			m = ConstantPowerPattern().Match(p);
			if (!m.Success) return $"({b}^{p})"!;

            string ps = p.Contains('.', StringComparison.Ordinal) || p.StartsWith('-')
				? '^' + p
				: ConvertToSuperScript(p);

			// An unreduced power-of-one must render distinctly from the bare base: rendering
			// it as just `b` would collide with the base's own catalog key, causing
			// Catalog.Register to hand back the base (wrong type) on an interning hit.
			// GetReduction() still collapses x^1 to x; only the unreduced string changes.
			if (ps == "¹") return $"({b}^{p})";

			// Check for negative to invert the base.
			return ps.StartsWith('-') || ps.StartsWith("(-", StringComparison.Ordinal) ? $"(1/{b}{ps})" : $"({b}{ps})";
		});

	protected override Lazy<string> Describe(IEnumerable<Lazy<string>> children)
	{
		Lazy<string>? bas = null;
		Lazy<string>? pow = null;
		int count = 0;
		foreach (Lazy<string> e in children)
		{
			switch (count++)
			{
				case 0:
					bas = e;
					break;

				case 1:
					pow = e;
					break;

				case 2:
					throw new InvalidOperationException("Describe for exponent should only have two children.");
			}
		}

		return count < 2
			? throw new InvalidOperationException("Describe for exponent needs two children.")
			: Describe(bas!, pow!);
	}

	public override IEvaluate<T> GetReduction()
	{
		IEvaluate<T> bas = Catalog.GetReduced(Base);
		IEvaluate<T> pow = Catalog.GetReduced(Power);

		// Undefined poisons: an exponent over an undefined base or power is undefined.
		// Checked before every other rule so no fold below can mask it.
		if (bas is IUndefined || pow is IUndefined)
			return Catalog.GetUndefined();

        Constant<T> one = Catalog.GetConstant(T.MultiplicativeIdentity);
		Debug.Assert(one.Value == T.One);
		// No need to reduce if the power is already 1.
		if (pow == one)
			return bas;

		// The above check should suffice and if the power is still one, then it's a bug.
		Debug.Assert(pow is not IConstant<T> pc || pc.Value != T.One,
			"A stray 'one' constant was introduced instead of from the same catalog.");

		IEvaluate<T> VerifyDifferences(IEvaluate<T> b, IEvaluate<T> p)
			=> b == Base && p == Power ? this : Catalog.GetExponent(b, p);

		IEvaluate<T> FinalStep(IEvaluate<T> bas, IEvaluate<T> pow)
		{
			while (bas is Product<T> pProd)
			{
				if (pProd.Children.Length == 1)
				{
					bas = pProd.Children[0];
					continue;
				}

				// Exponents of products can be converted into products of exponents -- but over
				// the reals that is only sound for an INTEGER power. For any other power,
				// (a·b)^p = a^p·b^p fails whenever a factor can be negative: √(-1·x) is defined
				// for x ≤ 0, while (-1)^½ · x^½ is defined nowhere. Distributing there would
				// manufacture an undefined form from a valid one -- exactly the false positive
				// the Undefined detector must never produce. So a non-integer power is
				// distributed only over POSITIVE constant factors (always sound); the rest of
				// the product, sign and all, stays under the power.
				if (pow is IConstant<T> pc && pc.Value.IsInteger())
				{
					return Catalog.Register(
						Catalog.ProductOf(
							pProd.Children.Select(c => Catalog.GetReduced(Catalog.GetExponent(c, pow)))));
				}

				using var positivesLease = ListPool<IEvaluate<T>>.Shared.Rent();
				using var restLease = ListPool<IEvaluate<T>>.Shared.Rent();
				List<IEvaluate<T>> positives = positivesLease.Item;
				List<IEvaluate<T>> rest = restLease.Item;
				foreach (IEvaluate<T> c in pProd.Children)
				{
					if (c is IConstant<T> k && T.IsPositive(k.Value) && !T.IsZero(k.Value))
						positives.Add(Catalog.GetReduced(Catalog.GetExponent(c, pow)));
					else
						rest.Add(c);
				}

				if (positives.Count == 0 || rest.Count == 0)
					break; // Nothing to pull out soundly (or nothing left under the power).

				IEvaluate<T> remainder = rest.Count == 1 ? rest[0] : Catalog.ProductOf(rest);
				positives.Add(Catalog.GetExponent(remainder, pow));
				return Catalog.Register(Catalog.ProductOf(positives));
			}

			return VerifyDifferences(bas, pow);
		}

		IEvaluate<T> ReduceWherePowIsConstant(IEvaluate<T> bas, IConstant<T> pow)
		{
			T p = pow.Value;
			Debug.Assert(p != T.One, "The case where the power is one have already been done.");

			bool pZero = p == T.Zero;
			if (pZero && _powerOfZeroReduction == PowerOfZeroReduction.One)
				return one;

			if (bas is IConstant<T> cBas)
			{
				// Whenver the bas is one, the result is one (the base)
				T b = cBas.Value;
				if (b == T.One)
					return bas;

				// Base is zero? No way out. :)
				if (b == T.Zero)
				{
					if (pZero)
					{
						switch (_powerOfZeroReduction)
						{
							case PowerOfZeroReduction.Retain:
								return VerifyDifferences(bas, pow);

							case PowerOfZeroReduction.Throw:
								throw new InvalidOperationException("0 to the power of 0 is undefined.");

							case PowerOfZeroReduction.Undefined:
								return Catalog.GetUndefined();
						}
					}
					else if (T.IsNegative(p))
					{
						// Zero to a negative power is division by zero: undefined everywhere, for
						// every T. Reduction reports that as a value rather than throwing -- it is
						// the validity detector, and it must stay total for callers that reduce
						// speculatively (mutation, variation, predicates).
						return Catalog.GetUndefined();
					}

					return Catalog.GetConstant(T.Zero);
				}

				if (pZero)
				{
					// If the power is zero, the result is always 1 unless the base is zero
					// (handled above). Only reachable under a non-default PowerOfZeroReduction
					// policy -- the default returns `one` before ever getting here.
					return one;
				}

				// A negative base to a non-integer power has no real value (√(-4) is complex):
				// undefined everywhere, so it is Undefined -- decided symbolically here, before
				// any numeric fold could turn it into a NaN constant or throw for a type that
				// cannot represent NaN.
				if (T.IsNegative(b) && !p.IsInteger())
					return Catalog.GetUndefined();

				// Division by a type that can't divide accurately?
				if (T.IsNegative(p) && !Value<T>.IsFloatingPoint)
					return this;

				T newExp = cBas.Value.Pow(pow.Value);
				return Catalog.GetConstant(newExp);
			}

			if (bas is Exponent<T> bEx
				&& bEx.Power is IConstant<T> cP)
			{
				bas = bEx.Base;
				pow = Catalog.GetConstant(pow.Value * cP.Value);
			}

			return FinalStep(bas, pow);
		}

		IEvaluate<T> ReduceWhereBaseIsConstant(IConstant<T> bas, IEvaluate<T> pow)
		{
			T b = bas.Value;
			Debug.Assert(pow is not IConstant<T> cPow, "The case where the power is constant should be handled first.");

			// Whenver the bas is one, the result is one (the base)
			if (b == T.One)
				return bas;

			// Not constants? Then just check if a reduction occurred.
			return VerifyDifferences(bas, pow);
		}

		if (pow is IConstant<T> cPow)
			return ReduceWherePowIsConstant(bas, cPow);

		if (bas is IConstant<T> cBase)
			return ReduceWhereBaseIsConstant(cBase, pow);

		// No constants? Then do the main flow.
		return FinalStep(bas, pow);
	}

	internal static Exponent<T> Create(
		ICatalog<IEvaluate<T>> catalog,
		IEvaluate<T> @base,
		IEvaluate<T> power)
	{
		catalog.ThrowIfNull().OnlyInDebug();
		@base.ThrowIfNull().OnlyInDebug();
		power.ThrowIfNull().OnlyInDebug();
		Contract.EndContractBlock();

		return catalog.Register(new Exponent<T>(catalog, @base, power));
	}

	public virtual IEvaluate<T> NewUsing(
		ICatalog<IEvaluate<T>> catalog,
		(IEvaluate<T>, IEvaluate<T>) param)
		=> Create(catalog, param.Item1, param.Item2);

	public IEvaluate<T> NewUsing(
		(IEvaluate<T>, IEvaluate<T>) param)
		=> NewUsing(Catalog, param);
}

public static partial class Exponent
{
	public enum PowerOfZeroReduction
	{
		One, // Any power of zero results in 1.
		Zero, // Evaluate 0^0 as 0.
		Retain, // Don't reduce.
		Throw, // Throw if the base is zero.
		Undefined // Reduce 0^0 to the catalog's Undefined expression (see Undefined<T>).
	}

	public const string SuperScriptDigits = "⁰¹²³⁴⁵⁶⁷⁸⁹";

	[GeneratedRegex(@"^0?\.50*$|^\(0?\.50*\)$",
		RegexOptions.IgnoreCase | RegexOptions.Compiled)]
	internal static partial Regex SquareRootPattern();

	[GeneratedRegex(@"^-?[0-9\.]+|^-?\([-0-9\.]+\)|^\(-?[-0-9\.]+\)$",
		RegexOptions.IgnoreCase | RegexOptions.Compiled)]
	internal static partial Regex ConstantPowerPattern();

	public static string ConvertToSuperScript(ReadOnlySpan<char> number)
	{
        int len = number.Length;
		Span<char> span = stackalloc char[len];
		for (int i = 0; i < len; i++)
		{
            double n = char.GetNumericValue(number[i]);
			span[i] = SuperScriptDigits[(int)n];
		}

		return new string(span);
	}

	public static Exponent<TResult> GetExponent<TResult>(
		this ICatalog<IEvaluate<TResult>> catalog,
		IEvaluate<TResult> @base,
		IEvaluate<TResult> power)
		where TResult : notnull, INumber<TResult>
		=> Exponent<TResult>.Create(catalog, @base, power);

	public static Exponent<TResult> GetExponent<TResult>(
		this ICatalog<IEvaluate<TResult>> catalog,
		IEvaluate<TResult> @base,
		Constant<TResult> power)
		where TResult : notnull, INumber<TResult>
		=> Exponent<TResult>.Create(catalog, @base, catalog.GetConstant(power));

	public static Exponent<TResult> GetExponent<TResult>(
		this ICatalog<IEvaluate<TResult>> catalog,
		IEvaluate<TResult> @base,
		TResult power)
		where TResult : notnull, INumber<TResult>
		=> Exponent<TResult>.Create(catalog, @base, catalog.GetConstant(power));

	public static bool IsPowerOf<T>(this Exponent<T> exponent, in T power)
		where T : notnull, INumber<T>
	{
		exponent.ThrowIfNull().OnlyInDebug();
		return exponent.Power is Constant<T> p && p.Value == power;
	}

	public static bool IsSquareRoot<T>(this Exponent<T> exponent)
		where T : notnull, INumber<T>
	{
		// Square roots only exist for floating-point-capable types. For integer T the
		// interned symbolic (1/2) still EXISTS as a node (construction is unrestricted), so
		// an integer x^(2^-1) would match it by reference below and answer "true" without
		// this gate. (NOT about x^0: for non-float T the reduction machinery refuses the
		// half division and keeps (1/2) symbolic -- it never truncates to a constant 0.)
		if (!Value<T>.IsFloatingPoint)
			return false;

		var power = exponent.Power;

		// Constant arm, zero catalog machinery: value + value == 1 identifies one half
		// exactly (doubling is exact for binary floats and for decimal; no division, so
		// integer truncation cannot manufacture a false positive on this arm even without
		// the gate above). Value comparison also correctly accepts equal-valued constants
		// interned under different renderings (e.g. decimal "0.5" vs "0.500").
		if (power is Constant<T> constant)
			return constant.Value + constant.Value == T.One;

		// Symbolic arm: the UNREDUCED exponent 2^-1, which renders exactly "(1/2)", is
		// deliberately interned in the catalog under its own honest key, so anything that
		// looks up "(1/2)" finds it. Register is find-or-create: after the first call the
		// hit path is a pure lookup, and interning makes this a single reference compare.
		// Deliberately NEVER GetReduced(Power): reducing an arbitrary subtree can THROW on
		// degenerate trees (a 0^negative anywhere inside -> "cannot divide by zero"), and a
		// predicate on the mutation path must answer, not throw. The cost of that safety:
		// an exotic power that merely REDUCES to one half (without being the constant or
		// the symbolic) answers false -- same as every prior version of this method.
		var half = exponent.Catalog.Register("(1/2)", static (_, c) =>
			c.GetExponent(c.GetConstant(Value<T>.Two), c.GetConstant(-T.One)));
		return power == half;
	}

	internal static T Pow<T>(this T baseValue, T exponent)
		where T : notnull, INumber<T>
	{
		if (baseValue == T.Zero || baseValue == T.One)
			return baseValue;

		if (exponent == T.Zero)
			return T.MultiplicativeIdentity;

		Debug.Assert(baseValue != T.Zero || exponent > T.Zero, "Cannot divide by zero.");

		if (exponent.IsInteger())
		{
			T result;
			if (exponent < T.Zero)
			{
				result = T.One;
				exponent = -exponent;
				// Division.
				for (T i = T.One; i <= exponent; i++)
				{
					result /= baseValue;

					// Canary: baseValue is never 0 or 1 here (both short-circuit above),
					// so dividing 1 by baseValue exactly once can only reproduce 1 if this
					// T's division is incapable (e.g. a no-op or non-reciprocal type).
					// Checked on the first division only: base == -1 legitimately RETURNS
					// to 1 every second division (a 2-cycle), which would falsely trip an
					// every-iteration check without indicating incapable division.
					if (i == T.One)
						Debug.Assert(result != T.One, "Type must be capable of division.");
				}
			}
			else if (Value<T>.IsBinaryInteger)
			{
				// Exponentiation by squaring: O(log exponent) multiplications instead of
				// O(exponent). Safe here -- and ONLY here -- because T : IBinaryInteger<T>'s
				// unchecked multiplication is exact commutative-ring arithmetic (mod 2^n for
				// fixed-width integers, exact for arbitrary-precision types such as
				// BigInteger). In a commutative ring, the product of `exponent` copies of
				// `baseValue` is independent of the order/grouping in which the
				// multiplications are performed -- including how/when wraparound overflow
				// occurs, since modular reduction commutes with ring addition/multiplication:
				// (x mod m) * (y mod m) mod m == (x * y) mod m regardless of grouping. That
				// makes this loop's result bit-identical to the sequential loop below, just
				// computed in fewer multiplications. This does NOT hold for floating-point T
				// (double/float/decimal): float multiplication is not associative under
				// rounding, so a different grouping can produce a different result. Those
				// types must keep taking the sequential-loop path (the final `else` below) --
				// deliberately left untouched here; author decision pending on whether to
				// adopt squaring there too (rounding-path reproducibility trade-off).
				T two = Value<T>.Two;
				T e = exponent;
				T b = baseValue;
				result = T.One;
				while (e > T.Zero)
				{
					if (!T.IsEvenInteger(e))
						result *= b;
					b *= b;
					e /= two;
				}
			}
			else
			{
				result = baseValue;
				// Multiplication.
				for (T i = T.One; i < exponent; i++)
					result *= baseValue;
			}

			return result;
		}

		// Non-integer exponent: computed in double, the way the previous per-type switch
		// already did for double, float and decimal -- now for EVERY numeric T (Half and NFloat
		// used to throw "no supported calculation"), with no boxing on this hot path (the old
		// switch allocated a box per call). Saturating conversions on both sides: values that
		// do not fit T clamp instead of throwing (a NaN result becomes NaN where T has one and
		// zero otherwise -- the same rule as Undefined; decimal used to throw OverflowException).
		// Reduction never reaches this for the everywhere-undefined cases (negative base to a
		// non-integer power) -- those are decided symbolically as Undefined first.
		return T.CreateSaturating(Math.Pow(double.CreateSaturating(baseValue), double.CreateSaturating(exponent)));
	}
}
