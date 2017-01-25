using System;
using System.Collections.Generic;

namespace EvaluationFramework.BooleanOperators
{
	public class Or<TContext> : OperatorBase<IEvaluate<TContext, bool>, TContext, bool>
	{
		public const string SYMBOL = " | ";
		public Or(IEnumerable<IEvaluate<TContext, bool>> children = null)
			: base(SYMBOL, children)
		{

		}

		public override bool Evaluate(TContext context)
		{
			if (ChildrenInternal.Count == 0)
				throw new InvalidOperationException("Cannot resolve boolean of empty set.");

			foreach (var result in ChildResults(context))
			{
				if (result) return true;
			}

			return false;
		}


	}

	public static class Or
	{
		public static Or<TContext> Using<TContext>(IEnumerable<IEvaluate<TContext, bool>> evaluations)
		{
			return new Or<TContext>(evaluations);
		}
	}


}