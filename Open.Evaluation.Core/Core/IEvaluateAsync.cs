/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

namespace Open.Evaluation.Core;

/// <summary>
/// An asynchronous evaluation that can be executed.
/// </summary>
public interface IEvaluateAsync
{
	/// <inheritdoc cref="IEvaluate.Evaluate(object)"/>
	ValueTask<object> EvaluateAsync(object context);
}

/// <inheritdoc cref="IEvaluateAsync"/>
public interface IEvaluateAsync<TResult> : IEvaluateAsync
	where TResult : notnull, IEquatable<TResult>, IComparable<TResult>
{
	/// <inheritdoc cref="IEvaluate.Evaluate(object)"/>
	new ValueTask<TResult> EvaluateAsync(object context);
}
