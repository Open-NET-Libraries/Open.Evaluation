/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

using System.Diagnostics.CodeAnalysis;
using Throw;

namespace Open.Evaluation.Core;

public abstract class OperationBase<TResult>
	: EvaluationBase<TResult>, IFunction<TResult>, IReducibleEvaluation<IEvaluate<TResult>>
	where TResult : notnull, IEquatable<TResult>, IComparable<TResult>
{
	protected OperationBase(Symbol symbol) => Symbol = symbol;

	public Symbol Symbol { get; }

	[return: NotNull]
	protected virtual IEvaluate<TResult> Reduction(
		[DisallowNull, NotNull] ICatalog<IEvaluate<TResult>> catalog)
		=> this;

	// Override this if reduction is possible.  Return null if you can't reduce.
	public bool TryGetReduced(
		[DisallowNull, NotNull] ICatalog<IEvaluate<TResult>> catalog,
		[NotNull] out IEvaluate<TResult> reduction)
	{
		catalog.ThrowIfNull().OnlyInDebug();
		reduction = Reduction(catalog);
		return reduction != this;
	}
}
