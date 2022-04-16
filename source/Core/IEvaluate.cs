/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

using System.Diagnostics.CodeAnalysis;

namespace Open.Evaluation.Core;

public interface IEvaluate
{
	/// <summary>
	/// Exectues and returns the calculation/evaluation.
	/// </summary>
	/// <param name="context">The context object that defines the parameters for the evaluation.</param>
	/// <returns>The resultant value of this evaluation</returns>
	[return: NotNull]
	object Evaluate(object context);

	/// <summary>
	/// Returns the string representation of this evaluation using the context parameters.
	/// </summary>
	/// <param name="context">The context object that defines the parameters for the evaluation.</param>
	/// <returns>The resultant string that repesents the actual calcuation being done by .Evaluation().</returns>
	[return: NotNull]
	string ToString(object context);

	/// <summary>
	/// Returns the formulaic representation of this evaluation without using the actual parameter values.
	/// </summary>
	/// <returns>The resultant string that repesents the underlying formula.</returns>
	[return: NotNull]
	string ToStringRepresentation();
}

public interface IEvaluate<out TResult> : IEvaluate
{
	/// <inheritdoc cref="IEvaluate.Evaluate(object)" />
	[return: NotNull]
	new TResult Evaluate(object context);
}
