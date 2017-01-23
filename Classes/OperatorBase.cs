using System;
using System.Collections.Generic;

namespace EvaluationEngine
{
	public abstract class OperatorBase<TChild, TContext, TResult>
		: FunctionBase<TContext, TResult>, IOperator<TChild, TContext, TResult>
	{

		protected OperatorBase(string symbol, Func<TContext, TResult> evaluator) : base(symbol, evaluator)
		{
			ChildrenInternal = new List<TChild>();
		}

		protected readonly List<TChild> ChildrenInternal;

		public ICollection<TChild> Children
		{
			get
			{
				return ChildrenInternal.AsReadOnly();
			}
		}
	}

}