namespace EvaluationEngine
{
	public abstract class EvaluationBase<TContext, TResult> : IEvaluate<TContext, TResult>
	{
		public abstract TResult Evaluate(TContext context);

		public abstract string ToString(TContext context);
		
		public abstract string ToStringRepresentation();
	}
}