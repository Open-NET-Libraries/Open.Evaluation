using System.Diagnostics.CodeAnalysis;

namespace Open.Evaluation.Core;

public abstract class OperationBase<T>(ICatalog<IEvaluate<T>> catalog, Symbol symbol)
	: EvaluationBase<T>(catalog), IFunction<T>, IReducibleEvaluation<IEvaluate<T>>
	where T : notnull, IEquatable<T>, IComparable<T>
{
	public Symbol Symbol { get; } = symbol;

	public virtual IEvaluate<T> GetReduction()
		=> this;

	// Override this if reduction is possible.  Return null if you can't reduce.
	public bool TryGetReduced([NotNull] out IEvaluate<T> reduction)
	{
		reduction = GetReduction();
		return reduction != this;
	}
}
