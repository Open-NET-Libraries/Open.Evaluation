namespace EvaluationEngine
{
	public interface IOperator<TChild, in TContext, out TResult>
		: IFunction<TContext, TResult>, IEvaluationNode<TChild, TContext, TResult>, ISymbolized
		where TChild : IEvaluate<TContext, TResult>
	{ 

	}
}