namespace EvaluationEngine
{
	public interface IConstant<out TResult> : IEvaluate<object, TResult>
	{
		TResult Value { get; }
	}
}