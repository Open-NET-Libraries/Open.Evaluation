/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

namespace Open.Evaluation.BooleanOperators
{
	public class Not : FunctionBase<bool>
	{
		public const char SYMBOL = '!';
		public const string SYMBOL_STRING = "!";

		public Not(IEvaluate<bool> contents)
			: base(Not.SYMBOL, Not.SYMBOL_STRING, contents)
		{

		}

		protected override bool EvaluateInternal(object context)
		{
			return !base.Evaluate(context);
		}

	}
	
}