/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Evaluation.Core;
using System;
using System.Collections.Generic;

namespace Open.Evaluation.Boolean
{
	public class AtLeast : CountingBase,
		IReproducable<(int, IEnumerable<IEvaluate<bool>>)>
	{
		public const string PREFIX = "AtLeast";
		internal AtLeast(int count, IEnumerable<IEvaluate<bool>> children = null)
			: base(PREFIX, count, children)
		{
			if (count < 1)
				throw new ArgumentOutOfRangeException("count", count, "Count must be at least 1.");
		}

		public IEvaluate NewUsing(
			ICatalog<IEvaluate> catalog,
			(int, IEnumerable<IEvaluate<bool>>) param)
			=> catalog.Register(new AtLeast(param.Item1, param.Item2));

		protected override bool EvaluateInternal(object context)
		{
			int count = 0;
			foreach (var result in ChildResults(context))
			{
				if ((bool)result) count++;
				if (count == Count) return true;
			}

			return false;
		}

	}

}
