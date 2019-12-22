﻿/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Evaluation.Core;
using System;
using System.Collections.Generic;

namespace Open.Evaluation.Boolean
{
	public abstract class CountingBase : OperatorBase<bool>
	{
		public const char SYMBOL = ',';
		public const string SEPARATOR = ", ";

		protected CountingBase(string prefix, int count, IEnumerable<IEvaluate<bool>> children)
			: base(SYMBOL, SEPARATOR, children, true)
		{
			if (count < 0)
				throw new ArgumentOutOfRangeException(nameof(count), count, "Count must be at least 0.");
			Prefix = prefix ?? throw new ArgumentNullException(nameof(prefix));
			Count = count;
		}

		protected readonly string Prefix;

		// ReSharper disable once MemberCanBeProtected.Global
		public int Count
		{
			get;
		}

		protected override string ToStringInternal(object contents)
			=> $"{Prefix}({Count}, {base.ToStringInternal(contents)})";

	}

}
