/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Hierarchy;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Open.Evaluation
{
	public interface IReducibleEvaluation<T> : IEvaluate
		where T : IEvaluate
	{
		/// <returns>Null if no reduction possible.  Otherwise returns the reduction.</returns>
		bool TryGetReduced(ICatalog<T> catalog, out T reduction);
	}

}
