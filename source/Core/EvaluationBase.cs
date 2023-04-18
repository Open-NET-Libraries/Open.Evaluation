/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using Throw;

namespace Open.Evaluation.Core;

/*
 * The main idea here is to enforce an immutable class and it's related sub classes.  No changes allowed after construction
 * A clone can only be created by 'recreating' or 'reconstructing'.
 */

public abstract class EvaluationBase<TResult> : IEvaluate<TResult>
	where TResult : notnull, IEquatable<TResult>, IComparable<TResult>
{
	protected EvaluationBase() => Description = new(Describe);

	[return: NotNull]
	protected abstract string Describe();

	[NotNull]
	public Lazy<string> Description { get; }

	protected abstract EvaluationResult<TResult> EvaluateInternal(Context context); // **

	/// <inheritdoc />
	public EvaluationResult<TResult> Evaluate([DisallowNull, NotNull] Context context)
	{
		context.ThrowIfNull().OnlyInDebug();
		Contract.EndContractBlock();

		// Cache results...
		return context.GetOrAdd(this, () => EvaluateInternal(context))!;
	}
}
