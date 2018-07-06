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

		protected override double EvaluateInternal(in object context)
		{
			return Math.Pow(Base.Evaluate(in context), Power.Evaluate(in context));
		}

		Exponent(in IEvaluate<double> evaluation, in IEvaluate<double> power)
			: base(in evaluation, in power)
		{ }

		internal new static Exponent Create(
			in ICatalog<IEvaluate<double>> catalog,
			in IEvaluate<double> @base,
			in IEvaluate<double> power)
		{
			return catalog.Register(new Exponent(in @base, in power));
		}

		public override IEvaluate NewUsing(
			in ICatalog<IEvaluate> catalog,
			in (IEvaluate<double>, IEvaluate<double>) param)
		{
			return catalog.Register(new Exponent(param.Item1, param.Item2));
		}
	}

	public static partial class ExponentExtensions
	{
		public static Exponent GetExponent(
			this ICatalog<IEvaluate<double>> catalog,
			in IEvaluate<double> @base,
			in IEvaluate<double> power)
		{
			return Exponent.Create(in catalog, in @base, in power);
		}

		public static Exponent GetExponent(
			this ICatalog<IEvaluate<double>> catalog,
			in IEvaluate<double> @base,
			in double power)
		{
			return Exponent.Create(in catalog, in @base, catalog.GetConstant(in power));
		}
	}

}