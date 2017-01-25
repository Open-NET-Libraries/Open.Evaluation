namespace EvaluationFramework
{
	public interface IParameter<TContext, TResult> : IEvaluate<TContext, TResult>
	{
		ushort ID { get; }
	}
}