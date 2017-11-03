/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Evaluation.Core;
using System;

namespace Open.Evaluation.Arithmetic
{
	public class Exponent<TResult> : OperatorBase<TResult>,
		IReproducable<(IEvaluate<TResult>, IEvaluate<TResult>)>
		where TResult : struct, IComparable
	{
		protected Exponent(
			IEvaluate<TResult> @base,
			IEvaluate<TResult> power)
			: base(
				  Exponent.SYMBOL,
				  Exponent.SEPARATOR,
				  // Need to provide to children so a node tree can be built.
				  new IEvaluate<TResult>[] { @base, power }
			)
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

		internal static Exponent<TResult> Create(
			ICatalog<IEvaluate<TResult>> catalog,
			IEvaluate<TResult> @base,
			IEvaluate<TResult> power)
		{
			return catalog.Register(new Exponent<TResult>(@base, power));
		}

		public virtual IEvaluate NewUsing(
			ICatalog<IEvaluate> catalog,
			(IEvaluate<TResult>, IEvaluate<TResult>) param)
		{
			return catalog.Register(new Exponent<TResult>(param.Item1, param.Item2));
		}
	}

	public static partial class ExponentExtensions
	{
		public static Exponent<TResult> GetExponent<TResult>(
			this ICatalog<IEvaluate<TResult>> catalog,
			IEvaluate<TResult> @base,
			IEvaluate<TResult> power)
			where TResult : struct, IComparable
		{
			return Exponent<TResult>.Create(catalog, @base, power);
		}
	}

}