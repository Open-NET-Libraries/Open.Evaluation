/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

namespace Open.Evaluation
{
	public interface IParameter<TResult> : IEvaluate<TResult>
	{
		ushort ID { get; }
	}
}