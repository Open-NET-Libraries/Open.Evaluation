/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

namespace Open.Evaluation.Core
{
	public interface IParameter : IEvaluate
	{
		ushort ID { get; }
	}

	public interface IParameter<TResult> : IEvaluate<TResult>, IParameter
	{
	}
}