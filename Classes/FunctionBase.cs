using System;

namespace EvaluationEngine
{
	public abstract class FunctionBase<TContext, TResult>
		: OperationBase<TContext, TResult>, IFunction<TContext, TResult>
	{

		protected FunctionBase(string symbol, IEvaluate<TContext, TResult> contents) : base(symbol)
		{
			if (contents == null)
				throw new ArgumentNullException("contents");

			Contents = contents;
		}

		public IEvaluate<TContext, TResult> Contents
		{
			get;
			private set;
		}

		public override TResult Evaluate(TContext context)
		{
			return Contents.Evaluate(context);
		}

		protected override string ToStringRepresentationInternal()
		{
			return ToStringInternal(Contents.ToStringRepresentation());
		}

	}

}