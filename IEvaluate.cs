/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

namespace Open.Evaluation
{
	public interface IEvaluate
	{
		object Evaluate(object context);

		string ToString(object context);

		string ToStringRepresentation();
	}

	public interface IEvaluate<out TResult> : IEvaluate
	{
		new TResult Evaluate(object context);
	}

	public interface IEvaluate<in TContext, out TResult> : IEvaluate<TResult>
	{
		TResult Evaluate(TContext context);

		string ToString(TContext context);
	}


}