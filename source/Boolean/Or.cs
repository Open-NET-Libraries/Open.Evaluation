/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Evaluation.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Open.Evaluation.Boolean
{
	public class Or : OperatorBase<IEvaluate<bool>, bool>,
		IReproducable<IEnumerable<IEvaluate<bool>>, IEvaluate<bool>>
	{
		public const char SYMBOL = '|';
		public const string SEPARATOR = " | ";

		public Or(IEnumerable<IEvaluate<bool>> children = null)
			: base(SYMBOL, SEPARATOR, children, true)
		{ }

		protected override bool EvaluateInternal(object context)
		{
			if (ChildrenInternal.Count == 0)
				throw new InvalidOperationException("Cannot resolve boolean of empty set.");

			return ChildResults(context).Cast<bool>().Any();
		}

		public IEvaluate<bool> NewUsing(
			ICatalog<IEvaluate<bool>> catalog,
			IEnumerable<IEvaluate<bool>> param)
			=> catalog.Register(new Or(param));
	}


}
