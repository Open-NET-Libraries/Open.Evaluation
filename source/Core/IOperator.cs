/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Hierarchy;

namespace Open.Evaluation.Core;

public interface IOperator<out TChild, TResult>
	: IFunction<TResult>, IParent<TChild>
	where TChild : class, IEvaluate
	where TResult : notnull, IEquatable<TResult>, IComparable<TResult>
{
}
