using System;
using System.Collections.Generic;

namespace EvaluationFramework.BooleanOperators
{
	public class AtLeast<TContext> : CountingBase<TContext>
	{
		public const string PREFIX = "AtLeast";
		public AtLeast(int count, IEnumerable<IEvaluate<TContext, bool>> children = null)
			: base(PREFIX, count, children)
		{
			if (count < 1)
				throw new ArgumentOutOfRangeException("count", count, "Count must be at least 1.");
		}

		public override bool Evaluate(TContext context)
		{
			int count = 0;
			foreach (var result in ChildResults(context))
			{
				if (result) count++;
				if (count == Count) return true;
			}

			return false;
		}

	}

}