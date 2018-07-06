/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

namespace Open.Evaluation.Core
{
	public interface IEvaluate
	{
		/// <summary>
		/// Exectues and returns the calculation/evaluation.
		/// </summary>
		/// <param name="context">The context object that defines the parameters for the evaluation.</param>
		/// <returns>The resultant value of this evaluation</returns>
		object Evaluate(in object context);

		/// <summary>
		/// Returns the string representation of this evaluation using the context parameters.
		/// </summary>
		/// <param name="context">The context object that defines the parameters for the evaluation.</param>
		/// <returns>The resultant string that repesents the actual calcuation being done by .Evaluation().</returns>
		string ToString(in object context);

		/// <summary>
		/// Returns the formulaic representation of this evaluation without using the actual parameter values.
		/// </summary>
		/// <returns>The resultant string that repesents the underlying formula.</returns>
		string ToStringRepresentation();
	}

	public interface IEvaluate<out TResult> : IEvaluate
	{
		new TResult Evaluate(in object context);
	}

}