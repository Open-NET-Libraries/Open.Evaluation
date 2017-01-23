namespace EvaluationEngine
{
	public interface IEvaluate<in TContext, out TResult> : IEvaluation
	{
		TResult Evaluate(TContext context);

		string ToString(TContext context);
	}
}