/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Evaluation.Core;
using System;
using System.Linq;

namespace Open.Evaluation.Boolean
{
	public class Not : OperatorBase<bool>,
		IReproducable<IEvaluate<bool>>
	{
		public const char SYMBOL = '!';
		public const string SYMBOL_STRING = "!";

		internal Not(in IEvaluate<bool> contents)
			: base(SYMBOL, SYMBOL_STRING, Enumerable.Repeat(contents, 1))
		{
			if (contents == null) throw new ArgumentNullException(nameof(contents));
		}

		public IEvaluate NewUsing(
			in ICatalog<IEvaluate> catalog,
			in IEvaluate<bool> param)
		{
			return catalog.Register(new Not(in param));
		}

		protected override bool EvaluateInternal(in object context)
		{
			return !ChildResults(context).Cast<bool>().Single();
		}

	}

}
