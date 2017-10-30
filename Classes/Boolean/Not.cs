/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Open.Evaluation.BooleanOperators
{
	public class Not : FunctionBase<bool>
	{
		public const char SYMBOL = '!';
		public const string SYMBOL_STRING = "!";

		public Not(IEvaluate<bool> contents)
			: base(SYMBOL, SYMBOL_STRING, contents)
		{

		}

		public override IEvaluate CreateNewFrom(object param, IEnumerable<IEvaluate> children)
		{
			Debug.WriteLineIf(param != null, "A param object was provided to a Not and will be lost. " + param);
			return new Not((IEvaluate<bool>)children.Single());
		}

		protected override bool EvaluateInternal(object context)
		{
			return !base.EvaluateInternal(context);
		}

	}
	
}