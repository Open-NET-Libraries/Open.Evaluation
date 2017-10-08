/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using System;

namespace Open.Evaluation
{
	public interface IConstant : IEvaluate
	{
		IComparable Value { get; }
	}

	public interface IConstant<out TResult> : IEvaluate<TResult>, IConstant
		where TResult : IComparable
	{
		new TResult Value { get; }
	}
}