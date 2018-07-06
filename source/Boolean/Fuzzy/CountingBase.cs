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

		protected CountingBase(in string prefix, in int count, in IEnumerable<IEvaluate<bool>> children)
			: base(SYMBOL, SEPARATOR, in children, true)
		{
			if (count < 0)
				throw new ArgumentOutOfRangeException(nameof(count), count, "Count must be at least 0.");
			Prefix = prefix ?? throw new ArgumentNullException(nameof(prefix));
			Count = count;
		}

		protected readonly string Prefix;

		public int Count
		{
			get;
			private set;
		}

		protected override string ToStringInternal(in object contents)
		{
			return $"{Prefix}({Count}, {base.ToStringInternal(in contents)})";
		}

	}

}
