﻿/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Evaluation.Core;

namespace Open.Evaluation.Boolean
{
	// ReSharper disable once PossibleInfiniteInheritance
	public class Conditional<TResult> : OperationBase<TResult>,
		IReproducable<(IEvaluate<bool>, IEvaluate<TResult>, IEvaluate<TResult>)>
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
		}

		public IEvaluate<TResult> IfTrue
		{
			get;
		}

		public IEvaluate<TResult> IfFalse
		{
			get;
		}


		protected string ToStringInternal(object condition, object ifTrue, object ifFalse)
			=> $"{condition} ? {ifTrue} : {ifFalse}";

		public override string ToString(object context)
			=> ToStringInternal(
				Condition.Evaluate(context),
				IfTrue.Evaluate(context),
				IfFalse.Evaluate(context));

		protected override string ToStringRepresentationInternal()
			=> ToStringInternal(
				Condition.ToStringRepresentation(),
				IfTrue.ToStringRepresentation(),
				IfFalse.ToStringRepresentation());

		protected override TResult EvaluateInternal(object context)
			=> Condition.Evaluate(context)
				? IfTrue.Evaluate(context)
				: IfFalse.Evaluate(context);

		public IEvaluate NewUsing(
			ICatalog<IEvaluate> catalog,
			(IEvaluate<bool>, IEvaluate<TResult>, IEvaluate<TResult>) param)
		{
			return catalog.Register(
				new Conditional<TResult>(
					param.Item1,
					param.Item2,
					param.Item3));
		}
	}

	public static class Conditional
	{
		public const char SYMBOL = '?';
		public const string SEPARATOR = " ? ";
	}

}
