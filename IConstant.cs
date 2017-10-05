/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using System;

namespace Open.Evaluation
{
	public interface IConstant<out TResult> : IEvaluate<TResult>
		where TResult : IComparable
	{
		TResult Value { get; }
	}
}