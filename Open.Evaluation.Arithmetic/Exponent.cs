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
		IEvaluate<T> @base,
		IEvaluate<T> power,
		PowerOfZeroReduction powerOfZeroReduction = PowerOfZeroReduction.One)
		: base(Symbols.Exponent,
			  // Need to provide to children so a node tree can be built.
			  new[] { @base, power })
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
		var bas = Base.Evaluate(context);
		var pow = Power.Evaluate(context);

		return new(bas.Result.Pow(pow.Result), Describe([bas.Description, pow.Description]));
	}

	protected Lazy<string> Describe(Lazy<string> bas, Lazy<string> pow)
		=> new(() =>
		{
			var b = bas.Value;
			var p = pow.Value;
			Debug.Assert(!string.IsNullOrWhiteSpace(b));
			Debug.Assert(!string.IsNullOrWhiteSpace(p));

			if(p == "-1")
				return $"(1/{b})";

			var m = SquareRootPattern().Match(p);
			if (m.Success)
				return '√' + b;

			m = ConstantPowerPattern().Match(p);
			if (!m.Success) return $"({b}^{p})"!;

			var ps = p.Contains('.') || p.StartsWith('-')
				? '^' + p
				: ConvertToSuperScript(p);

			if (ps == "¹") return b;

			// Check for negative to invert the base.
			return ps.StartsWith('-') || ps.StartsWith("(-") ? $"(1/{b}{ps})" : $"({b}{ps})";
		});

	protected override Lazy<string> Describe(IEnumerable<Lazy<string>> children)
	{
		Lazy<string>? bas = null;
		Lazy<string>? pow = null;
		int count = 0;
		foreach (var e in children)
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

	protected override IEvaluate<T> Reduction(
		ICatalog<IEvaluate<T>> catalog)
	{
		catalog.ThrowIfNull().OnlyInDebug();
		IEvaluate<T> bas = catalog.GetReduced(Base);
		IEvaluate<T> pow = catalog.GetReduced(Power);

		var one = catalog.GetConstant(T.MultiplicativeIdentity);
		Debug.Assert(one.Value == T.One);
		// No need to reduce if the power is already 1.
		if (pow == one)
			return bas;

		// The above check should suffice and if the power is still one, then it's a bug.
		Debug.Assert(pow is not IConstant<T> pc || pc.Value != T.One,
			"A stray 'one' constant was introduced instead of from the same catalog.");

		IEvaluate<T> VerifyDifferences(IEvaluate<T> b, IEvaluate<T> p)
			=> b == Base && p == Power ? this : catalog.GetExponent(b, p);

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
					return catalog.Register(
						catalog.ProductOf(
							pProd.Children.Select(c => catalog.GetReduced(catalog.GetExponent(c, pow)))));
				}
			}

			return VerifyDifferences(bas, pow);
		}

		IEvaluate<T> ReduceWherePowIsConstant(IEvaluate<T> bas, IConstant<T> pow)
		{
			var p = pow.Value;
			Debug.Assert(p != T.One, "The case where the power is one have already been done.");

			bool pZero = p == T.Zero;
			if (pZero && _powerOfZeroReduction == PowerOfZeroReduction.One)
				return one;

			if (bas is IConstant<T> cBas)
			{
				// Whenver the bas is one, the result is one (the base)
				var b = cBas.Value;
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

					return catalog.GetConstant(T.Zero);
				}

				if (pZero)
				{
					// If the power is zero, the result is always 1 unless the base is zero.
					return catalog.GetConstant(T.Zero);
				}

				T newExp = cBas.Value.Pow(pow.Value);
				return catalog.GetConstant(newExp);
			}

			if (bas is Exponent<T> bEx
				&& bEx.Power is IConstant<T> cP)
			{
				bas = bEx.Base;
				pow = catalog.GetConstant(pow.Value * cP.Value);
			}

			return FinalStep(bas, pow);
		}

		IEvaluate<T> ReduceWhereBaseIsConstant(IConstant<T> bas, IEvaluate<T> pow)
		{
			var b = bas.Value;
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

		return catalog.Register(new Exponent<T>(@base, power));
	}

	public virtual IEvaluate<T> NewUsing(
		ICatalog<IEvaluate<T>> catalog,
		(IEvaluate<T>, IEvaluate<T>) param)
		=> Create(catalog, param.Item1, param.Item2);
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
		var len = number.Length;
		Span<char> span = stackalloc char[len];
		for (var i = 0; i < len; i++)
		{
			var n = char.GetNumericValue(number[i]);
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
		where T : notnull, INumber<T>, IFloatingPoint<T>
		=> exponent.IsPowerOf(ValueFloat<T>.Half);

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
				for (var i = T.One; i <= exponent; i++)
					result /= baseValue;

				Debug.Assert(result != T.One, "Type must be capable of division.");
			}
			else
			{
				result = baseValue;
				// Multiplication.
				for (var i = T.One; i < exponent; i++)
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
