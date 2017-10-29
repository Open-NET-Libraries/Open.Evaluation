/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Hierarchy;

namespace Open.Evaluation
{
	public interface IEvaluationNode<TChild, in TContext, out TResult>
		: IParent<TChild>, IEvaluate<TContext, TResult>
		where TChild : IEvaluate<TContext, TResult>
	{

	}
}