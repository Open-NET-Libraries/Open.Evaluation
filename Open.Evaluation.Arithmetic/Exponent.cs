/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

namespace Open.Evaluation.Arithmetic;

// ReSharper disable once PossibleInfiniteInheritance
public class Exponent<T> : OperatorBase<T>,
	IReproducable<(IEvaluate<T>, IEvaluate<T>), IEvaluate<T>>
	where T : notnull, INumber<T>
{
	protected Exponent(
		IEvaluate<T> @base,
		IEvaluate<T> power)
		: base(Symbols.Exponent,
			  // Need to provide to children so a node tree can be built.
			  new[] { @base, power })
	{
		@base.ThrowIfNull().OnlyInDebug();
		power.ThrowIfNull().OnlyInDebug();
		Base = @base;
		Power = power;
	}

	public IEvaluate<T> Base { get; }

	public IEvaluate<T> Power { get; }

	protected override EvaluationResult<T> EvaluateInternal(Context context)
	{
		var bas = Base.Evaluate(context);
		var pow = Power.Evaluate(context);

		return new (bas.Result.Pow(pow), Describe([bas.Description, pow.Description]));
	}

	protected Lazy<string> Describe(Lazy<string> bas, Lazy<string> pow)
		=> new(() =>
		{
			var b = bas.Value;
			var p = pow.Value;
			Debug.Assert(!string.IsNullOrWhiteSpace(b));
			Debug.Assert(!string.IsNullOrWhiteSpace(p));

			var m = Exponent.SquareRootPattern().Match(p);
			if (m.Success)
				return '√' + b;

			m = Exponent.ConstantPowerPattern().Match(p);
			if (!m.Success) return $"({b}^{p})"!;

			var ps = p.Contains('.')
				? '^' + p
				: Exponent.ConvertToSuperScript(p);

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

		var one = catalog.GetConstant(T.One);
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

			if (bas is IConstant<T> cBas)
			{
				// Whenver the bas is one, the result is one (the base)
				var b = cBas.Value;
				if (b == T.One)
					return bas;

				if (b == T.Zero)
				{
					if (p == T.Zero)
						throw new InvalidOperationException("0 to the power of 0 is undefined.");
					if (T.IsNegative(p))
						throw new InvalidOperationException("0 to a negative power is undefined. (Cannot divide by zero.)");
				}

				if (p == T.Zero)
				{
					// If the power is zero, the result is always 1 unless the base is zero.
					return b == T.Zero
						? catalog.GetConstant(T.Zero)
						: one;
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
	public const string SuperScriptDigits = "⁰¹²³⁴⁵⁶⁷⁸⁹";

	[GeneratedRegex(@"^0?\.50*$|^\(0?\.50*\)$",
		RegexOptions.IgnoreCase | RegexOptions.Compiled)]
	internal static partial Regex SquareRootPattern();

	[GeneratedRegex(@"^-?[0-9\.]+|^-?\([-0-9\.]+\)|^\(-?[-0-9\.]+\)$",
		RegexOptions.IgnoreCase | RegexOptions.Compiled)]
	internal static partial Regex ConstantPowerPattern();

	public static string ConvertToSuperScript(string number)
	{
		number.ThrowIfNull().OnlyInDebug();
		return ArrayPool<char>.Shared.Rent(number!.Length, number, (number, r) =>
		{
			var len = number.Length;
			for (var i = 0; i < len; i++)
			{
				var n = char.GetNumericValue(number[i]);
				r[i] = SuperScriptDigits[(int)n];
			}

			return new string(r, 0, len);
		});
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

		if (!exponent.IsInteger())
		{
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

			throw new ArgumentException("No supported calculation for non-integer exponent.", nameof(exponent));
		}

		T result = baseValue;
		if (exponent < T.Zero)
		{
			exponent = -exponent;
			// Division.
			for (var i = T.One; i < exponent; i++)
				result /= baseValue;
		}
		else
		{
			// Multiplication.
			for (var i = T.One; i < exponent; i++)
				result *= baseValue;
		}

		return exponent;
	}
}
