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
	[return: NotNull]
	protected abstract string Describe();

	string? _description; // Was using a Lazy<string> before, but seems overkill for an immutable structure.

	[NotNull]
	public string Description
		=> LazyInitializer.EnsureInitialized(ref _description, Describe)!;

	protected abstract EvaluationResult<TResult> EvaluateInternal(object context); // **

	/// <inheritdoc />
	public EvaluationResult<TResult> Evaluate([DisallowNull, NotNull] object context)
	{
		context.ThrowIfNull().OnlyInDebug();
		Contract.EndContractBlock();

		// Use existing context... // Caches results...
		if (context is ParameterContext pc)
			return pc.GetOrAdd(this, () => EvaluateInternal(pc))!; // **

		// Create a new one for this tree...
		using var newPc = new ParameterContext(context!);
		return Evaluate(newPc);
	}
}
