namespace EvaluationFramework.BooleanOperators
{
	public class Conditional<TContext> : FunctionBase<TContext, bool>
	{
		public const string SYMBOL = " ? ";
		public Conditional(
			IEvaluate<TContext, bool> evaluation,
			IEvaluate<TContext, bool> ifTrue,
			IEvaluate<TContext, bool> ifFalse)
			: base(SYMBOL, evaluation)
		{
			IfTrue = ifTrue;
			IfFalse = ifFalse;
		}

		public IEvaluate<TContext, bool> IfTrue
		{
			get;
			private set;
		}
		public IEvaluate<TContext, bool> IfFalse
		{
			get;
			private set;
		}

		public override bool Evaluate(TContext context)
		{
			return base.Evaluate(context)
				? IfTrue.Evaluate(context)
				: IfFalse.Evaluate(context);
		}

		protected override string ToStringInternal(object evaluation)
		{
			return string.Format(
				"{0} ? {1} : {2}",
				evaluation,
				IfTrue.ToStringRepresentation(),
				IfFalse.ToStringRepresentation());
		}

		public override string ToString(TContext context)
		{
			return string.Format(
				"{0} ? {1} : {2}",
				base.Evaluate(context),
				IfTrue.Evaluate(context),
				IfFalse.Evaluate(context));
		}

	}

}