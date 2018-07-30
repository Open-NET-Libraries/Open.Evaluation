/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Evaluation.Core;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;

namespace Open.Evaluation.Arithmetic
{
	public sealed class Exponent : Exponent<double>
	{
		public const char SYMBOL = '^';
		public const string SEPARATOR = "^";

		public const string SuperScriptDigits = "⁰¹²³⁴⁵⁶⁷⁸⁹";

		public static string ConvertToSuperScript(ReadOnlySpan<char> number)
		{
			var len = number.Length;
			var r = new char[len];
			for (var i = 0; i < len; i++)
			{
				var n = char.GetNumericValue(number[i]);
				r[i] = SuperScriptDigits[(int)n];
			}

			return new string(r);
		}

		static readonly Regex SquareRootPattern = new Regex(@"^\((.+)\^0\.5\)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
		static readonly Regex ConstantPowerPattern = new Regex(@"^\((.+)\^(-)?([0-9\.]+)\)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		protected override string ToStringInternal(object contents)
		{
			var value = base.ToStringInternal(contents);

			var m = SquareRootPattern.Match(value);
			if (m.Success)
				return '√' + m.Groups[1].Value;

			m = ConstantPowerPattern.Match(value);
			if (!m.Success) return value;

			var b = m.Groups[1].Value;
			Debug.Assert(!string.IsNullOrWhiteSpace(b));
			var p = m.Groups[3].Value;
			var pspan = p.AsSpan();
			var ps = pspan.IndexOf('.') != -1
				? ('^' + p)
				: ConvertToSuperScript(pspan);

			if (ps == "¹") ps = string.Empty;
			return m.Groups[2].Success ? $"(1/{b}{ps})" : $"({b}{ps})";
		}

		protected override double EvaluateInternal(object context)
			=> Math.Pow(Base.Evaluate(context), Power.Evaluate(context));

		Exponent(IEvaluate<double> evaluation, IEvaluate<double> power)
			: base(evaluation, power)
		{ }

		[SuppressMessage("ReSharper", "ConvertIfStatementToSwitchStatement")]
		[SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
		[SuppressMessage("ReSharper", "InvertIf")]
		protected override IEvaluate<double> Reduction(ICatalog<IEvaluate<double>> catalog)
		{
			var pow = catalog.GetReduced(Power);
			if (!(pow is IConstant<double> cPow))
				return catalog.Register(NewUsing(catalog, (catalog.GetReduced(Base), pow)));

			var p = Convert.ToDouble(cPow.Value);
			if (p == 0)
				return GetConstant(catalog, (dynamic)1);

			var bas = catalog.GetReduced(Base);

			if (p == 1)
				return bas;

			if (bas is Constant<double> cBas)
			{
				if (cBas.Value == 1)
					return cBas;

				var newPow = Math.Pow(cBas.Value, cPow.Value);
				// ReSharper disable once CompareOfFloatsByEqualityOperator
				if (Math.Floor(newPow) == newPow)
					return GetConstant(catalog, newPow);

			}

			// ReSharper disable once InvertIf
			if (bas is Exponent<double> bEx && bEx.Power is Constant<double> cP)
			{
				bas = bEx.Base;
				pow = GetConstant(catalog, cPow.Value * cP.Value);
			}

			while (bas is Product<double> pProd)
			{
				if (pProd.Children.Count == 1)
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
			//if (cPow != null && cPow.Value < 0 && bas is Product<double> pProd)
			//{
			//	var nBas = pProd.ExtractMultiple(catalog, out var multiple);
			//	if (multiple != null && nBas != bas && multiple.Value < 0)
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

}
