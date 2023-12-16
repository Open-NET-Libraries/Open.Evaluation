/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

namespace Open.Evaluation.Core;

/// <summary>
/// An evaluation that can be executed.
/// </summary>
public interface IEvaluate : IDescribe
{
	/// <summary>
	/// Executes and returns the calculation/evaluation.
	/// </summary>
	/// <param name="context">The context object that defines the parameters for the evaluation.</param>
	/// <returns>The result containing the value and representation of the evaluation.</returns>
	EvaluationResult<object> Evaluate(Context context);
}

/// <inheritdoc cref="IEvaluate"/>
public interface IEvaluate<TResult> : IEvaluate
	where TResult : notnull, IEquatable<TResult>, IComparable<TResult>
{
	/// <inheritdoc cref="IEvaluate.Evaluate(object)"/>
	new EvaluationResult<TResult> Evaluate(Context context);

	EvaluationResult<object> IEvaluate.Evaluate(Context context) => Evaluate(context);
}
