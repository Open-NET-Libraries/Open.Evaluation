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

		internal Not(IEvaluate<bool> contents)
			: base(SYMBOL, SYMBOL_STRING,
				  Enumerable.Repeat(contents ?? throw new ArgumentNullException(nameof(contents)), 1))
		{ }

		public IEvaluate NewUsing(
			ICatalog<IEvaluate> catalog,
			IEvaluate<bool> param)
			=> catalog.Register(new Not(param));

		protected override bool EvaluateInternal(object context)
			=> !ChildResults(context).Cast<bool>().Single();

	}

}
