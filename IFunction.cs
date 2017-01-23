namespace EvaluationEngine
{
	public interface IFunction<in TContext, out TResult>
		: IEvaluate<TContext, TResult>, ISymbolized
	{
	}
}