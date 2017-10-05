/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

namespace Open.Evaluation
{
	public interface IFunction<out TResult>
		: IEvaluate<TResult>, ISymbolized
	{
	}
}