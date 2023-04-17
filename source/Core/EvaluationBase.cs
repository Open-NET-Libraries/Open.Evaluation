/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

namespace Open.Evaluation.Core;

/*
 * The main idea here is to enforce an immutable class and it's related sub classes.  No changes allowed after construction
 * A clone can only be created by 'recreating' or 'reconstructing'.
 */

public abstract class EvaluationBase<TResult> : IEvaluate<TResult>
{
	protected abstract string ToStringRepresentationInternal();
	string? _toStringRepresentation; // Was using a Lazy<string> before, but seems overkill for an immutable structure.
	public string ToStringRepresentation()
		=> LazyInitializer.EnsureInitialized(ref _toStringRepresentation, ToStringRepresentationInternal)!;

	[return: NotNull]
	protected abstract TResult EvaluateInternal(object context); // **

	[return: NotNull]
	protected abstract string ToStringInternal(object context);

	[return: NotNull]
	object IEvaluate.Evaluate(object context) => Evaluate(context);

	/// <inheritdoc />
	[return: NotNull]
	public TResult Evaluate(object context)
	{
		Debug.Assert(context is not null);
		// Use existing context... // Caches results...
		if (context is ParameterContext pc)
			return pc.GetOrAdd(this, () => EvaluateInternal(pc))!; // **

		// Create a new one for this tree...
		using var newPc = new ParameterContext(context!);
		return Evaluate(newPc);
	}

	/// <inheritdoc />
	[return: NotNull]
	public virtual string ToString(object context)
		=> ToStringInternal(Evaluate(context));
}
