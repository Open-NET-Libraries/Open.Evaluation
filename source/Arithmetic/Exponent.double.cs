/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Evaluation.Core;
using System;

namespace Open.Evaluation.Arithmetic
{
	public sealed class Exponent : Exponent<double>
	{
		public const char SYMBOL = '^';
		public const string SEPARATOR = "^";

		protected override double EvaluateInternal(object context)
		{
			return Math.Pow(Base.Evaluate(context), Power.Evaluate(context));
		}

		Exponent(IEvaluate<double> evaluation, IEvaluate<double> power)
			: base(evaluation, power)
		{ }

		internal new static Exponent Create(
			ICatalog<IEvaluate<double>> catalog,
			IEvaluate<double> @base,
			IEvaluate<double> power)
		{
			return catalog.Register(new Exponent(@base, power));
		}

		public override IEvaluate NewUsing(
			ICatalog<IEvaluate> catalog,
			in (IEvaluate<double>, IEvaluate<double>) param)
		{
			return catalog.Register(new Exponent(param.Item1, param.Item2));
		}
	}

	public static partial class ExponentExtensions
	{
		public static Exponent GetExponent(
			this ICatalog<IEvaluate<double>> catalog,
			IEvaluate<double> @base,
			IEvaluate<double> power)
		{
			return Exponent.Create(catalog, @base, power);
		}

		public static Exponent GetExponent(
			this ICatalog<IEvaluate<double>> catalog,
			IEvaluate<double> @base,
			in double power)
		{
			return Exponent.Create(catalog, @base, catalog.GetConstant(in power));
		}
	}

}
