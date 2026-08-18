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
				}
				else
				{
					// Exponents of products can be converted into products of exponents.
					// By doing this, any other ungrouped products can be reduced including constants with exponents.
					return Catalog.Register(
						Catalog.ProductOf(
							pProd.Children.Select(c => Catalog.GetReduced(Catalog.GetExponent(c, pow)))));
				}
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
						}
					}
					else if (T.IsNegative(p))
					{
						throw new InvalidOperationException("0 to a negative power is undefined. (Cannot divide by zero.)");
					}

					return Catalog.GetConstant(T.Zero);
				}

				if (pZero)
				{
					// If the power is zero, the result is always 1 unless the base is zero.
					return Catalog.GetConstant(T.Zero);
				}

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
		Throw // Throw if the base is zero.
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
		var pow = exponent.Power;
		if (exponent.Catalog.TryGetItem<IEvaluate<T>>("0.5", out var point5) && pow == point5)
			return true;

		// Issue #19: this used to go through Catalog.Register("(1/2)", ...), but the factory
		// computes GetExponent(2, -1).GetReduction(), which for a float-capable T reduces to a
		// Constant registered under "0.5" -- not "(1/2)". Register's id/hash consistency check
		// then threw ArgumentException on the very first call for a fresh catalog. Computing the
		// reduction directly (without the mismatched id) keeps it self-consistently interned via
		// the normal GetExponent/GetConstant catalog paths; the "0.5" fast path above is
		// unaffected.
		var half = exponent.Catalog.GetExponent(
			exponent.Catalog.GetConstant(Value<T>.Two),
			exponent.Catalog.GetConstant(-T.One))
			.GetReduction();

		return exponent.Power == half;
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

		switch (exponent)
		{
			case double exp:
			{
				return baseValue is double bv
					? (T)(object)Math.Pow(bv, exp)
					: throw new UnreachableException("Strange type mismatch.");
			}

			case float exp:
			{
				return baseValue is float bv
					? (T)(object)(float)Math.Pow(bv, exp)
					: throw new UnreachableException("Strange type mismatch.");
			}

			case decimal exp:
			{
				return baseValue is decimal bv
					? (T)(object)(decimal)Math.Pow(Convert.ToDouble(bv), Convert.ToDouble(exp))
					: throw new UnreachableException("Strange type mismatch.");
			}
		}

		throw new ArgumentException($"No supported calculation for exponent [{exponent.GetType()}]({exponent}).", nameof(exponent));
	}
}
