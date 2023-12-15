/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Evaluation.Core;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;

namespace Open.Evaluation.Arithmetic;
public abstract class ArithmeticOperatorBase<TResult>
	: OperatorBase<TResult>,
	IReproducable<IEnumerable<IEvaluate<TResult>>, IEvaluate<TResult>>
	where TResult : notnull, INumber<TResult>
{
	protected ArithmeticOperatorBase(
		Symbol symbol,
		IEnumerable<IEvaluate<TResult>> children,
		bool reorderChildren = false,
		int minimumChildren = 1)
		: base(symbol, children, reorderChildren, minimumChildren)
	{ }

	public abstract IEvaluate<TResult> NewUsing(
		ICatalog<IEvaluate<TResult>> catalog,
		IEnumerable<IEvaluate<TResult>> param);
}
