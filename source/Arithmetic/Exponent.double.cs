/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Evaluation.Core;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Open.Evaluation.Arithmetic
{
	public sealed class Exponent : Exponent<double>
	{
		public const char SYMBOL = '^';
		public const string SEPARATOR = "^";

		protected override double EvaluateInternal(object context)
			=> Math.Pow(Base.Evaluate(context), Power.Evaluate(context));

		Exponent(IEvaluate<double> evaluation, IEvaluate<double> power)
			: base(evaluation, power)
		{ }

		[SuppressMessage("ReSharper", "ConvertIfStatementToSwitchStatement")]
		protected override IEvaluate<double> Reduction(ICatalog<IEvaluate<double>> catalog)
		{
			var pow = catalog.GetReduced(Power);
			if (!(pow is Constant<double> cPow))
				return catalog.Register(NewUsing(catalog, (catalog.GetReduced(Base), pow)));

			dynamic p = cPow.Value;
			if (p == 0)
				return GetConstant(catalog, (dynamic)1);

			if (p == 1)
				return catalog.GetReduced(Base);

			var bas = catalog.GetReduced(Base);
			if (bas is Constant<double> cBas)
				return GetConstant(catalog, Math.Pow(cBas.Value, cPow.Value));

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
