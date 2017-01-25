using System;

namespace EvaluationFramework
{
	public abstract class OperationBase<TContext, TResult>
		: EvaluationBase<TContext, TResult>, IFunction<TContext, TResult>, ISymbolized
	{

		protected OperationBase(string symbol) : base()
		{
			if (symbol == null)
				throw new ArgumentNullException("symbol");
			Symbol = symbol;
		}

		public string Symbol { get; private set; }

		protected virtual string ToStringInternal(object contents)
		{
			return string.Format("{0}({1})", Symbol, contents);
		}
		
		public override string ToString(TContext context)
		{
			return ToStringInternal(Evaluate(context));
		}

	}

}