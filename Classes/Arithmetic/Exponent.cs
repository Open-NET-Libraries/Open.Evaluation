/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace Open.Evaluation.ArithmeticOperators
{
	public class Exponent<TResult> : OperatorBase<TResult>
		where TResult : struct, IComparable
	{
		public Exponent(
			IEvaluate<TResult> @base,
			IEvaluate<TResult> power)
			: base(Exponent.SYMBOL, Exponent.SEPARATOR, new IEvaluate<TResult>[] { @base, power })
		{
			Base = @base;
			Power = power;
		}

		public IEvaluate<TResult> Base
		{
			get;
			private set;
		}

		public IEvaluate<TResult> Power
		{
			get;
			private set;
		}

		protected static double ConvertToDouble(dynamic value)
		{
			return (double)value;
		}
		protected override TResult EvaluateInternal(object context)
		{
			var evaluation = ConvertToDouble(Base.Evaluate(context));
			var power = ConvertToDouble(Power.Evaluate(context));

			return (TResult)(dynamic)Math.Pow(evaluation, power);
		}

		protected override IEvaluate<TResult> Reduction(ICatalog<IEvaluate<TResult>> catalog)
		{
			var pow = catalog.GetReduced(Power);
			if (pow is Constant<TResult> cPow)
			{
				dynamic p = cPow.Value;
				if (p == 0) return ConstantExtensions.GetConstant<TResult>(catalog, (dynamic)1);
				if (p == 1) return catalog.GetReduced(Base);
			}

			return catalog.Register(new Exponent<TResult>(catalog.GetReduced(Base), pow));
		}

		protected string ToStringInternal(object contents, object power)
		{
			return string.Format("({0}^{1})", contents, power);
		}

		public override IEvaluate NewUsing(IEnumerable<IEvaluate<TResult>> param)
		{
			return new Exponent<TResult>(param.First(), param.Skip(1).Single());
		}
	}


	public sealed class Exponent : Exponent<double>
	{
		public static Exponent Of(
			IEvaluate<double> @base,
			IEvaluate<double> power)
		{
			return new Exponent(@base, power);
		}

		public static Exponent Of(
			IEvaluate<double> evaluation,
			double power)
		{
			return new Exponent(evaluation, power);
		}

		public const char SYMBOL = '^';
		public const string SEPARATOR = "^";

		protected override double EvaluateInternal(object context)
		{
			return Math.Pow(Base.Evaluate(context), Power.Evaluate(context));
		}

		public Exponent(IEvaluate<double> evaluation, IEvaluate<double> power) : base(evaluation, power)
		{
		}

		public Exponent(IEvaluate<double> evaluation, double power) : base(evaluation, new Constant<double>(power))
		{
		}

		public override IEvaluate NewUsing(IEnumerable<IEvaluate<double>> param)
		{
			return new Exponent(param.First(), param.Skip(1).Single());
		}
	}


}