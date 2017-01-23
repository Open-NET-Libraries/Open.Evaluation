using System;

namespace EvaluationEngine
{
    public abstract class FunctionBase<TContext, TResult>
		: EvaluationBase<TContext, TResult>, IFunction<TContext, TResult>
	{

		protected FunctionBase(string symbol, Func<TContext, TResult> evaluator)
		{
			if (symbol == null)
				throw new ArgumentNullException("symbol");
			if (evaluator == null)
				throw new ArgumentNullException("evaluator");
			Symbol = symbol;
			_evaluator = evaluator;
		}

		Func<TContext, TResult> _evaluator;

		public string Symbol { get; private set; }

		public override TResult Evaluate(TContext context)
		{
			return _evaluator(context);
		}

		public override string ToString(TContext context)
		{
			return Symbol + "(" + Evaluate(context) + ")";
		}

	}

}