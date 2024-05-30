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

	/// <summary>
	/// The catalog this evaluation is associated with.
	/// </summary>
	/// <remarks>
	/// This is needed to ensure that evaluations are not duplicated and subsequently provides a simpler pathway to evaluation.
	/// </remarks>
	object Catalog { get; }
}

/// <inheritdoc cref="IEvaluate"/>
public interface IEvaluate<T> : IEvaluate
	where T : notnull, IEquatable<T>, IComparable<T>
{
	/// <inheritdoc cref="IEvaluate.Evaluate(object)"/>
	new EvaluationResult<T> Evaluate(Context context);

	EvaluationResult<object> IEvaluate.Evaluate(Context context) => Evaluate(context);

	/// <inheritdoc cref="IEvaluate.Catalog"/>
	new ICatalog<IEvaluate<T>> Catalog { get; }
}
