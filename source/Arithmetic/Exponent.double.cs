/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Evaluation.Core;
using System;
using System.Buffers;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace Open.Evaluation.Arithmetic;

public sealed class Exponent : Exponent<double>
{
	public const char SYMBOL = '^';
	public const string SEPARATOR = "^";

	public const string SuperScriptDigits = "⁰¹²³⁴⁵⁶⁷⁸⁹";

	public static string ConvertToSuperScript(string number)
	{
		Debug.Assert(number is not null);
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

	static readonly Regex SquareRootPattern = new(@"^\((.+)\^0\.5\)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
	static readonly Regex ConstantPowerPattern = new(@"^\((.+)\^(-)?([0-9\.]+)\)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	protected override string ToStringInternal(object context)
	{
		var value = base.ToStringInternal(context);
		Debug.Assert(value is not null);
		var m = SquareRootPattern.Match(value);
		if (m.Success)
			return '√' + m.Groups[1].Value;

		m = ConstantPowerPattern.Match(value);
		if (!m.Success) return value!;

		var b = m.Groups[1].Value;
		Debug.Assert(!string.IsNullOrWhiteSpace(b));
		var p = m.Groups[3].Value;
		var ps = p.Contains('.')
			? ('^' + p)
			: ConvertToSuperScript(p);

		var success = m.Groups[2].Success;
		if (success && ps == "¹") ps = string.Empty;
		return success ? $"(1/{b}{ps})" : $"({b}{ps})";
	}

	protected override double EvaluateInternal(object context)
		=> Math.Pow(Base.Evaluate(context), Power.Evaluate(context));

	Exponent(IEvaluate<double> evaluation, IEvaluate<double> power)
		: base(evaluation, power)
	{ }

	protected override IEvaluate<double> Reduction(ICatalog<IEvaluate<double>> catalog)
	{
		var pow = catalog.GetReduced(Power);
		if (pow is not IConstant<double> cPow)
			return catalog.Register(NewUsing(catalog, (catalog.GetReduced(Base), pow)));

		var zero = catalog.GetConstant(0);
		var one = catalog.GetConstant(1);
		if (cPow == zero)
			return one;

		var bas = catalog.GetReduced(Base);

		if (cPow == one)
			return bas;

		if (bas is Constant<double> cBas)
		{
			if (cBas == one)
				return cBas;

			var newExp = Math.Pow(cBas.Value, cPow.Value);
			// ReSharper disable once CompareOfFloatsByEqualityOperator
			if (Math.Floor(newExp) == newExp)
				return GetConstant(catalog, newExp);
		}

		// ReSharper disable once InvertIf
		if (bas is Exponent<double> bEx
		&& bEx.Power is Constant<double> cP)
		{
			bas = bEx.Base;
			pow = GetConstant(catalog, cPow.Value * cP.Value);
		}

		while (bas is Product<double> pProd)
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

	internal new static Exponent Create(
		ICatalog<IEvaluate<double>> catalog,
		IEvaluate<double> @base,
		IEvaluate<double> power)
		=> catalog.Register(new Exponent(@base, power));

	public override IEvaluate<double> NewUsing(
		ICatalog<IEvaluate<double>> catalog,
		(IEvaluate<double>, IEvaluate<double>) param)
		=> Create(catalog, param.Item1, param.Item2);
}

public static partial class ExponentExtensions
{
	public static Exponent GetExponent(
		this ICatalog<IEvaluate<double>> catalog,
		IEvaluate<double> @base,
		IEvaluate<double> power)
		=> Exponent.Create(catalog, @base, power);

	public static Exponent GetExponent(
		this ICatalog<IEvaluate<double>> catalog,
		IEvaluate<double> @base,
		double power)
		=> Exponent.Create(catalog, @base, catalog.GetConstant(power));
}
