namespace EvaluationEngine
{
	public interface IEvaluationNode<TChild, in TContext, out TResult>
		: IParent<TChild>, IEvaluate<TContext, TResult>
		where TChild : IEvaluate<TContext, TResult>
	{

	}
}