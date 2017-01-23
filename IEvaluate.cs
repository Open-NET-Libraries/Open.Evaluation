namespace EvaluationEngine
{
	public interface IEvaluate<in TContext, out TResult>
	{
		TResult Evaluate(TContext context);

		string ToString(TContext context);

		string ToStringRepresentation();
	}
}