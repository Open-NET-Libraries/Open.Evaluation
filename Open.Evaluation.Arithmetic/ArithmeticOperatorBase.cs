/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

namespace Open.Evaluation.Arithmetic;

public abstract class ArithmeticOperatorBase<T>(
	ICatalog<IEvaluate<T>> catalog,
	Symbol symbol,
	IEnumerable<IEvaluate<T>> children,
	bool reorderChildren = false,
	int minimumChildren = 1)
	: OperatorBase<T>(catalog, symbol, children, reorderChildren, minimumChildren),
	  IReproducable<IEnumerable<IEvaluate<T>>, IEvaluate<T>>
	where T : notnull, INumber<T>
{
	public abstract IEvaluate<T> NewUsing(
		ICatalog<IEvaluate<T>> catalog,
		IEnumerable<IEvaluate<T>> param);

	public IEvaluate<T> NewUsing(
		IEnumerable<IEvaluate<T>> param)
		=> NewUsing(Catalog, param);
}
