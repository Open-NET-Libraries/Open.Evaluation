using System;

namespace EvaluationEngine.ArithmeticOperators
{
	public class Exponent<TContext> : FunctionBase<TContext, double>
	{
		public const string SYMBOL = "^";
		public Exponent(
			IEvaluate<TContext, double> evaluation,
			IEvaluate<TContext, double> power)
			: base(SYMBOL, evaluation)
		{
			Power = power;
		}

		public IEvaluate<TContext, double> Power
		{
			get;
			private set;
		}

		public override double Evaluate(TContext context)
		{
			return Math.Pow(base.Evaluate(context), Power.Evaluate(context));
		}
	}


}