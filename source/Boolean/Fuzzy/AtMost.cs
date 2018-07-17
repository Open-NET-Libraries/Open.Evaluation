/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Evaluation.Core;
using System.Collections.Generic;

namespace Open.Evaluation.Boolean
{
	public class AtMost : CountingBase,
		IReproducable<(int, IEnumerable<IEvaluate<bool>>), IEvaluate<bool>>
	{
		public const string PREFIX = "AtMost";
		internal AtMost(int count, IEnumerable<IEvaluate<bool>> children)
			: base(PREFIX, count, children)
		{ }

		public IEvaluate<bool> NewUsing(
			ICatalog<IEvaluate<bool>> catalog,
			(int, IEnumerable<IEvaluate<bool>>) param)
		{
			return catalog.Register(new AtMost(param.Item1, param.Item2));
		}

		protected override bool EvaluateInternal(object context)
		{
			var count = 0;
			foreach (var result in ChildResults(context))
			{
				if ((bool)result) count++;
				if (count > Count) return false;
			}

			return true;
		}

	}


}
