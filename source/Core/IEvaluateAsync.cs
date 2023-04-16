/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

namespace Open.Evaluation.Core;

public interface IEvaluateAsync
{
	/// <inheritdoc cref="IEvaluate.Evaluate(object)"/>
	ValueTask<object> EvaluateAsync(object context);
}

public interface IEvaluateAsync<TResult> : IEvaluateAsync
	where TResult : notnull, IEquatable<TResult>, IComparable<TResult>
{
	/// <inheritdoc cref="IEvaluate.Evaluate(object)"/>
	new ValueTask<TResult> EvaluateAsync(object context);
}
