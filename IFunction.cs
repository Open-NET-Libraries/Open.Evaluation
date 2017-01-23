namespace EvaluationEngine
{
	public interface IFunction<in TContext, out TResult>
		: IEvaluation
	{
		string Symbol { get; }
	}
}