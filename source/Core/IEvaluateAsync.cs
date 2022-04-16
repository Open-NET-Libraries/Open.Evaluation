/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

using System.Threading.Tasks;

namespace Open.Evaluation.Core;

public interface IEvaluateAsync : IEvaluate
{
	/// <summary>
	/// Exectues and returns the calculation/evaluation.
	/// </summary>
	/// <param name="context">The context object that defines the parameters for the evaluation.</param>
	/// <returns>The resultant value of this evaluation</returns>
	ValueTask<object> EvaluateAsync(object context);

	/// <summary>
	/// Returns the string representation of this evaluation using the context parameters.
	/// </summary>
	/// <param name="context">The context object that defines the parameters for the evaluation.</param>
	/// <returns>The resultant string that repesents the actual calcuation being done by .Evaluation().</returns>
	ValueTask<string> ToStringAsync(object context);
}

public interface IEvaluateAsync<TResult> : IEvaluate<TResult>, IEvaluateAsync
{
	/// <inheritdoc />
	new ValueTask<TResult> EvaluateAsync(object context);
}
