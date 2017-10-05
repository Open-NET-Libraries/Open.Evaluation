/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using System;
using System.Collections.Generic;

namespace Open.Evaluation.BooleanOperators
{
	public class And : OperatorBase<IEvaluate<bool>, bool>
	{
		public const char SYMBOL = '&';
		public const string SEPARATOR = " & ";

		public And(IEnumerable<IEvaluate<bool>> children = null)
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
				if (!(bool)result) return false;
			}

			return true;
		}

	}

}