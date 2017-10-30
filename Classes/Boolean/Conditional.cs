/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using System.Collections.Generic;
using System.Linq;

namespace Open.Evaluation.BooleanOperators
{
	public class Conditional<TResult> : OperationBase<TResult>
	{

		public Conditional(
			IEvaluate<bool> condition,
			IEvaluate<TResult> ifTrue,
			IEvaluate<TResult> ifFalse)
			: base(Conditional.SYMBOL, Conditional.SEPARATOR)
		{
			Condition = condition;
			IfTrue = ifTrue;
			IfFalse = ifFalse;
		}

		public IEvaluate<bool> Condition
		{
			get;
			private set;
		}

		public IEvaluate<TResult> IfTrue
		{
			get;
			private set;
		}

		public IEvaluate<TResult> IfFalse
		{
			get;
			private set;
		}


		const string FormatString = "{0} ? {1} : {2}";

		protected string ToStringInternal(object condition, object ifTrue, object ifFalse)
		{
			return string.Format(
				FormatString,
				condition,
				ifTrue,
				ifFalse);
		}

		public override string ToString(object context)
		{
			return ToStringInternal(
				Condition.Evaluate(context),
				IfTrue.Evaluate(context),
				IfFalse.Evaluate(context));
		}

		protected override string ToStringRepresentationInternal()
		{
			return ToStringInternal(
				Condition.ToStringRepresentation(),
				IfTrue.ToStringRepresentation(),
				IfFalse.ToStringRepresentation());
		}

		protected override TResult EvaluateInternal(object context)
		{
			return Condition.Evaluate(context)
			? IfTrue.Evaluate(context)
			: IfFalse.Evaluate(context);
		}
	}

	public static class Conditional
	{
		public const char SYMBOL = '?';
		public const string SEPARATOR = " ? ";
	}

}