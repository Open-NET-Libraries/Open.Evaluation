/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using System.Collections.Generic;

namespace Open.Evaluation.BooleanOperators
{
	public class Exactly : CountingBase
	{
		public const string PREFIX = "Exactly";
		public Exactly(int count, IEnumerable<IEvaluate<bool>> children = null)
			: base(PREFIX, count, children)
		{
		}

		public override OperatorBase<IEvaluate<bool>, bool> CreateNewFrom(object param, IEnumerable<IEvaluate<bool>> children)
		{
			return new Exactly((int)param, children);
		}

		protected override bool EvaluateInternal(object context)
		{
			int count = 0;
			foreach (var result in ChildResults(context))
			{
				if ((bool)result) count++;
				if (count > Count) return false;
			}

			return count == Count;
		}

	}


}