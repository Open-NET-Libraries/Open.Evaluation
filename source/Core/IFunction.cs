/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

namespace Open.Evaluation.Core;

public interface IFunction<TResult>
	: IEvaluate<TResult>, ISymbolized
	where TResult : notnull, IEquatable<TResult>, IComparable<TResult>
{
}
