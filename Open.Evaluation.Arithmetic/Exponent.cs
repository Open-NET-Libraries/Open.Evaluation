/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Evaluation.Core;
using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Numerics;
using System.Text.RegularExpressions;
using Throw;

namespace Open.Evaluation.Arithmetic;

// ReSharper disable once PossibleInfiniteInheritance
public class Exponent<TResult> : OperatorBase<TResult>,
	IReproducable<(IEvaluate<TResult>, IEvaluate<TResult>), IEvaluate<TResult>>
	where TResult : notnull, INumber<TResult>
{
	protected Exponent(
		IEvaluate<TResult> @base,
		IEvaluate<TResult> power)
		: base(Symbols.Exponent,
			  // Need to provide to children so a node tree can be built.
			  new[] { @base, power })
	{
		@base.ThrowIfNull().OnlyInDebug();
		power.ThrowIfNull().OnlyInDebug();
		Base = @base;
		Power = power;
	}

	public IEvaluate<TResult> Base { get; }

	public IEvaluate<TResult> Power { get; }

	protected override EvaluationResult<TResult> EvaluateInternal(Context context)
	{
		var evaluation = ConvertToDouble(Base.Evaluate(context));
		var power = ConvertToDouble(Power.Evaluate(context));

		return (TResult)(dynamic)Math.Pow(evaluation, power);
	}

	protected override IEvaluate<TResult> Reduction(
		ICatalog<IEvaluate<TResult>> catalog)
	{
		catalog.ThrowIfNull().OnlyInDebug();

		var pow = catalog.GetReduced(Power);
		IEvaluate<TResult> bas;
		if (pow is Constant<TResult> cPow)
		{
			var p = cPow.Value;
			if (p == TResult.Zero)
				return catalog.GetConstant(TResult.One);

			if (p == TResult.One)
				return catalog.GetReduced(Base);

			bas = catalog.GetReduced(Base);
			if (bas is Exponent<TResult> basExp && basExp.Power is Constant<TResult> basePow)
			{
				// Multiply cascaded exponents.
				return catalog.Register(NewUsing(catalog, (basExp.Base, catalog.GetConstant(p * basePow.Value))));
			}
		}
		else
		{
			bas = catalog.GetReduced(Base);
		}

		return catalog.Register(NewUsing(catalog, (bas, pow)));
	}

	protected override string ToStringInternal(object context)
	{
		var value = base.ToStringInternal(context);
		Debug.Assert(value is not null);
		var m = Exponent.SquareRootPattern().Match(value);
		if (m.Success)
			return '√' + m.Groups[1].Value;

		m = Exponent.ConstantPowerPattern().Match(value);
		if (!m.Success) return value!;

		var b = m.Groups[1].Value;
		Debug.Assert(!string.IsNullOrWhiteSpace(b));
		var p = m.Groups[3].Value;
		var ps = p.Contains('.')
			? '^' + p
			: Exponent.ConvertToSuperScript(p);

		var success = m.Groups[2].Success;
		if (success && ps == "¹") ps = string.Empty;
		return success ? $"(1/{b}{ps})" : $"({b}{ps})";
	}

	protected override IEvaluate<TResult> Reduction(
		ICatalog<IEvaluate<TResult>> catalog)
	{
		var pow = catalog.GetReduced(Power);
		if (pow is not IConstant<TResult> cPow)
			return catalog.Register(NewUsing(catalog, (catalog.GetReduced(Base), pow)));

		var zero = catalog.GetConstant(TResult.Zero);
		var one = catalog.GetConstant(TResult.One);
		if (cPow == zero)
			return one;

		var bas = catalog.GetReduced(Base);

		if (cPow == one)
			return bas;

		if (bas is Constant<TResult> cBas)
		{
			if (cBas == one)
				return cBas;

			TResult newExp = cBas.Value.Pow(cPow.Value);
			// ReSharper disable once CompareOfFloatsByEqualityOperator
			if (newExp.IsInteger())
				return catalog.GetConstant(newExp);
		}

		// ReSharper disable once InvertIf
		if (bas is Exponent<TResult> bEx
		&& bEx.Power is Constant<TResult> cP)
		{
			bas = bEx.Base;
			pow = catalog.GetConstant(cPow.Value * cP.Value);
		}

		while (bas is Product<TResult> pProd)
		{
			if (pProd.Children.Length == 1)
			{
				bas = pProd.Children[0];
			}
			else
			{
				// Exponents of products can be converted into products of exponents.
				return catalog.Register(
					catalog.ProductOf(
						pProd.Children.Select(c => catalog.GetReduced(NewUsing(catalog, (c, pow))))));
			}
		}

		//cPow = pow as IConstant<double>;
		//if (cPow is not null && cPow.Value < 0 && bas is Product<double> pProd)
		//{
		//	var nBas = pProd.ExtractMultiple(catalog, out var multiple);
		//	if (multiple is not null && nBas != bas && multiple.Value < 0)
		//	{
		//		multiple = catalog.ProductOfConstants(-1, multiple);
		//		var divisor = catalog.ProductOf(multiple, nBas);
		//		return catalog.Register(
		//			catalog.ProductOf(-1,
		//				NewUsing(catalog, (divisor, pow))));
		//	}
		//}

		return catalog.Register(NewUsing(catalog, (bas, pow)));
	}

	internal static Exponent<TResult> Create(
		ICatalog<IEvaluate<TResult>> catalog,
		IEvaluate<TResult> @base,
		IEvaluate<TResult> power)
	{
		catalog.ThrowIfNull().OnlyInDebug();
		@base.ThrowIfNull().OnlyInDebug();
		power.ThrowIfNull().OnlyInDebug();
		Contract.EndContractBlock();

		// ReSharper disable once SuspiciousTypeConversion.Global
		return catalog.Register(new Exponent<TResult>(@base, power));
	}

	public virtual IEvaluate<TResult> NewUsing(
		ICatalog<IEvaluate<TResult>> catalog,
		(IEvaluate<TResult>, IEvaluate<TResult>) param)
		=> Create(catalog, param.Item1, param.Item2);
}

public static partial class Exponent
{
	public const string SuperScriptDigits = "⁰¹²³⁴⁵⁶⁷⁸⁹";

	[GeneratedRegex("^\\((.+)\\^0\\.5\\)$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
	internal static partial Regex SquareRootPattern();

	[GeneratedRegex("^\\((.+)\\^(-)?([0-9\\.]+)\\)$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
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
			if (exponent < T.Zero)
				throw new ArgumentException("Cannot calculate a negative decimal power.", nameof(exponent));

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
