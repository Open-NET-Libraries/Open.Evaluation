using System.Collections.Generic;

namespace EvaluationFramework.BooleanOperators
{
	public class Exactly<TContext> : CountingBase<TContext>
	{
		public const string PREFIX = "Exactly";
		public Exactly(int count, IEnumerable<IEvaluate<TContext, bool>> children = null)
			: base(PREFIX, count, children)
		{
		}

		public override bool Evaluate(TContext context)
		{
			int count = 0;
			foreach (var result in ChildResults(context))
			{
				if (result) count++;
				if (count > Count) return false;
			}

			return count == Count;
		}

	}


}