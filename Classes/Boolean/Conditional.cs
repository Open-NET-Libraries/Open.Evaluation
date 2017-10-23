/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using System.Collections.Generic;
using System.Linq;

namespace Open.Evaluation.BooleanOperators
{
	public class Conditional<TResult> : FunctionBase<bool>, IFunction<TResult>
	{

		public Conditional(
			IEvaluate<bool> evaluation,
			IEvaluate<TResult> ifTrue,
			IEvaluate<TResult> ifFalse)
			: base(Conditional.SYMBOL, Conditional.SEPARATOR, evaluation)
		{
			IfTrue = ifTrue;
			IfFalse = ifFalse;
			ChildrenInternal.Add(ifTrue);
			ChildrenInternal.Add(ifFalse);
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

		public new TResult Evaluate(object context)
		{
			return base.Evaluate(context)
				? IfTrue.Evaluate(context)
				: IfFalse.Evaluate(context);
		}

		const string FormatString = "{0} ? {1} : {2}";

		protected override string ToStringInternal(object evaluation)
		{
			return string.Format(
				FormatString,
				evaluation,
				IfTrue.ToStringRepresentation(),
				IfFalse.ToStringRepresentation());
		}

		public override string ToString(object context)
		{
			return string.Format(
				FormatString,
				base.Evaluate(context),
				IfTrue.Evaluate(context),
				IfFalse.Evaluate(context));
		}

		public override OperatorBase<IEvaluate, bool> CreateNewFrom(object param, IEnumerable<IEvaluate> children)
		{
			return new Conditional<TResult>(
				(IEvaluate<bool>)param,
				(IEvaluate<TResult>)children.First(),
				(IEvaluate<TResult>)children.Skip(1).Single()
			);
		}
	}

	public static class Conditional
	{
		public const char SYMBOL = '?';
		public const string SEPARATOR = " ? ";
	}

}