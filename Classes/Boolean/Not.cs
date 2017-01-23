namespace EvaluationEngine.BooleanOperators
{
	public class Not<TContext> : FunctionBase<TContext, bool>
	{
		public const string SYMBOL = "!";
		public Not(IEvaluate<TContext, bool> contents)
			: base(SYMBOL, contents)
		{

		}

		public override bool Evaluate(TContext context)
		{
			return !base.Evaluate(context);
		}

	}

	public static class Not
	{
		public static Not<TContext> Inverse<TContext>(this IEvaluate<TContext, bool> evaluation)
		{
			return new Not<TContext>(evaluation);
		}
	}


}