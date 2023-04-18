/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

using System.Diagnostics.CodeAnalysis;

namespace Open.Evaluation.Core;

public interface IEvaluate : IDescribe
{
	/// <summary>
	/// Executes and returns the calculation/evaluation.
	/// </summary>
	/// <param name="context">The context object that defines the parameters for the evaluation.</param>
	/// <returns>The result containing the value and representation of the evaluation.</returns>
	EvaluationResult<object> Evaluate([DisallowNull, NotNull] Context context);
}

public interface IEvaluate<TResult> : IEvaluate
	where TResult : notnull, IEquatable<TResult>, IComparable<TResult>
{
	/// <inheritdoc cref="IEvaluate.Evaluate(object)"/>
	[return: NotNull]
	new EvaluationResult<TResult> Evaluate([DisallowNull, NotNull] Context context);

	[return: NotNull]
	EvaluationResult<object> IEvaluate.Evaluate([DisallowNull, NotNull] Context context) => Evaluate(context);
}
