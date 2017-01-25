namespace EvaluationFramework.BooleanOperators
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
		public static Not<TContext> Using<TContext>(IEvaluate<TContext, bool> evaluation)
		{
			return new Not<TContext>(evaluation);
		}
	}

	public static class NotExtensions
	{
		public static Not<TContext> Not<TContext>(this IEvaluate<TContext, bool> evaluation)
		{
			return new Not<TContext>(evaluation);
		}
	}

}