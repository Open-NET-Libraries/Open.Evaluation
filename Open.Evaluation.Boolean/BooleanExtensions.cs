/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Evaluation.Core;

namespace Open.Evaluation.Boolean;

public static partial class BooleanExtensions
{
	public static IEvaluate<bool> CountAtLeast(
		this ICatalog<IEvaluate<bool>> catalog,
		(int count, IEnumerable<IEvaluate<bool>> children) param)
		=> Counting.AtLeast.Create(catalog, param);
}
