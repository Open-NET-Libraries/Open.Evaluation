using System.Diagnostics.CodeAnalysis;

namespace Open.Evaluation.Core;

public abstract class OperationBase<T>(ICatalog<IEvaluate<T>> catalog, Symbol symbol)
	: EvaluationBase<T>(catalog), IFunction<T>, IReducibleEvaluation<IEvaluate<T>>
	where T : notnull, IEquatable<T>, IComparable<T>
{
	public Symbol Symbol { get; } = symbol;

	protected virtual IEvaluate<T> Reduction(
		ICatalog<IEvaluate<T>> catalog)
		=> this;

	// Override this if reduction is possible.  Return null if you can't reduce.
	public bool TryGetReduced(
		ICatalog<IEvaluate<T>> catalog,
		[NotNull] out IEvaluate<T> reduction)
	{
		catalog.ThrowIfNull().OnlyInDebug();
		reduction = Reduction(catalog);
		return reduction != this;
	}
}
