/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Open.Evaluation.BooleanOperators
{
	public class Or : OperatorBase<IEvaluate<bool>, bool>
	{
		public const char SYMBOL = '|';
		public const string SEPARATOR = " | ";

		public Or(IEnumerable<IEvaluate<bool>> children = null)
			: base(SYMBOL, SEPARATOR, children)
		{
			ReorderChildren();
		}

		protected override bool EvaluateInternal(object context)
		{
			if (ChildrenInternal.Count == 0)
				throw new InvalidOperationException("Cannot resolve boolean of empty set.");

			foreach (var result in ChildResults(context))
			{
				if ((bool)result) return true;
			}

			return false;
		}

		public override IEvaluate CreateNewFrom(object param, IEnumerable<IEvaluate> children)
		{
			Debug.WriteLineIf(param != null, "A param object was provided to a Or and will be lost. " + param);
			return new Or(children.Cast<IEvaluate<bool>>());
		}

	}


}