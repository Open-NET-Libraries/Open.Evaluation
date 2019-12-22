/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Evaluation.Core;
using System;
using System.Collections.Generic;

namespace Open.Evaluation.Boolean
{
	public static partial class Counting
	{
		public class AtLeast : CountingBase,
			IReproducable<(int, IEnumerable<IEvaluate<bool>>), IEvaluate<bool>>
		{
			public const string PREFIX = "AtLeast";

			internal AtLeast(int count, IEnumerable<IEvaluate<bool>> children)
				: base(PREFIX, count, children)
			{
				if (count < 1)
					throw new ArgumentOutOfRangeException("count", count, "Count must be at least 1.");
			}

			internal static IEvaluate<bool> Create(
				ICatalog<IEvaluate<bool>> catalog,
				(int count, IEnumerable<IEvaluate<bool>> children) param)
				=> catalog.Register(new AtLeast(param.count, param.children));

			public IEvaluate<bool> NewUsing(
				ICatalog<IEvaluate<bool>> catalog,
				(int, IEnumerable<IEvaluate<bool>>) param)
				=> Create(catalog, param);

			protected override bool EvaluateInternal(object context)
			{
				var count = 0;
				foreach (var result in ChildResults(context))
				{
					if ((bool)result) count++;
					if (count == Count) return true;
				}

				return false;
			}

		}
	}

	public static partial class BooleanExtensions
	{
		public static IEvaluate<bool> CountAtLeast(
			this ICatalog<IEvaluate<bool>> catalog,
			(int count, IEnumerable<IEvaluate<bool>> children) param)
			=> Counting.AtLeast.Create(catalog, param);
	}
}
