using System;

namespace EvaluationFramework
{
	public abstract class FunctionBase<TContext, TResult>
		: OperationBase<TContext, TResult>, IFunction<TContext, TResult>
	{

		protected FunctionBase(string symbol, IEvaluate<TContext, TResult> evaluation) : base(symbol)
		{
			if (evaluation == null)
				throw new ArgumentNullException("contents");

			Evaluation = evaluation;
		}

		public IEvaluate<TContext, TResult> Evaluation
		{
			get;
			private set;
		}

		public override TResult Evaluate(TContext context)
		{
			return Evaluation.Evaluate(context);
		}

		protected override string ToStringRepresentationInternal()
		{
			return ToStringInternal(Evaluation.ToStringRepresentation());
		}

	}

}