/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

namespace Open.Evaluation.Core;

public interface IParameter : IEvaluate
{
	ushort ID { get; }
}

public interface IParameter<out TResult> : IEvaluate<TResult>, IParameter
{
}
