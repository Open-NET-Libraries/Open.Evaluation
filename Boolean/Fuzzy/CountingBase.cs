/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Evaluation.Core;
using System;
using System.Collections.Generic;

namespace Open.Evaluation.Boolean
{
	public abstract class CountingBase : OperatorBase<IEvaluate<bool>, bool>
	{
		public const char SYMBOL = ',';
		public const string SEPARATOR = ", ";

		protected CountingBase(string prefix, int count, IEnumerable<IEvaluate<bool>> children)
			: base(SYMBOL, SEPARATOR, children, true)
		{
			if (count < 0)
				throw new ArgumentOutOfRangeException("count", count, "Count must be at least 0.");
			Prefix = prefix ?? throw new ArgumentNullException("prefix");
			Count = count;
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