/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using System;
using System.Collections.Generic;

namespace Open.Evaluation.BooleanOperators
{
	public abstract class CountingBase : OperatorBase<IEvaluate<bool>, bool>
	{
		public const char SYMBOL = ',';
		public const string SEPARATOR = ", ";

		protected CountingBase(string prefix, int count, IEnumerable<IEvaluate<bool>> children = null)
			: base(SYMBOL, SEPARATOR, children)
		{
			if (prefix == null)
				throw new ArgumentNullException("prefix");
			if (count < 0)
				throw new ArgumentOutOfRangeException("count", count, "Count must be at least 0.");
			Prefix = prefix;
			Count = count;

			ReorderChildren();
		}

		protected readonly string Prefix;

		public int Count
		{
			get;
			private set;
		}

		protected override string ToStringInternal(object contents)
		{
			return String.Format("{0}({1}, {2})", Prefix, Count, base.ToStringInternal(contents));
		}

	}


}