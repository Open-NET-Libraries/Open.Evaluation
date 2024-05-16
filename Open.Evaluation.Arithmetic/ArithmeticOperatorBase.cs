/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Evaluation.Core;
using System.Numerics;

namespace Open.Evaluation.Arithmetic;
public abstract class ArithmeticOperatorBase<TResult>(
	Symbol symbol, IEnumerable<IEvaluate<TResult>> children,
	bool reorderChildren = false, int minimumChildren = 1)
	: OperatorBase<TResult>(symbol, children, reorderChildren, minimumChildren),
	IReproducable<IEnumerable<IEvaluate<TResult>>, IEvaluate<TResult>>
	where TResult : notnull, INumber<TResult>
{
	public abstract IEvaluate<TResult> NewUsing(
		ICatalog<IEvaluate<TResult>> catalog,
		IEnumerable<IEvaluate<TResult>> param);
}
