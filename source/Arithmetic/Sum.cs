/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Evaluation.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Open.Evaluation.Arithmetic
{
	public class Sum<TResult> :
		OperatorBase<IEvaluate<TResult>, TResult>,
		IReproducable<IEnumerable<IEvaluate<TResult>>>
		where TResult : struct, IComparable
	{
		protected Sum(IEnumerable<IEvaluate<TResult>> children = null)
			: base(Sum.SYMBOL, Sum.SEPARATOR, children, true)
		{ }

		protected override TResult EvaluateInternal(object context)
		{
			if (ChildrenInternal.Count == 0)
				throw new InvalidOperationException("Cannot resolve sum of empty set.");

			dynamic result = 0;
			foreach (var r in ChildResults(context).Cast<TResult>())
			{
				result += r;
			}

			return result;
		}

		protected override IEvaluate<TResult> Reduction(ICatalog<IEvaluate<TResult>> catalog)
		{
			// Phase 1: Flatten sums of sums.
			var children = catalog.Flatten<Sum<TResult>>(ChildrenInternal).ToList(); // ** chidren's reduction is done here.

			// Phase 2: Can we collapse?
			switch (children.Count)
			{
				case 0:
					return catalog.GetConstant((TResult)(dynamic)0);
				case 1:
					return children[0];
			}

			if (typeof(TResult) == typeof(float) && children.Any(c => c is IConstant<float> d && float.IsNaN(d.Value)))
				return catalog.GetConstant((TResult)(dynamic)float.NaN);

			if (typeof(TResult) == typeof(double) && children.Any(c => c is IConstant<double> d && double.IsNaN(d.Value)))
				return catalog.GetConstant((TResult)(dynamic)double.NaN);

			var one = catalog.GetConstant((TResult)(dynamic)1);

			// Phase 3: Look for groupings by "multiples".
			var withMultiples = children.Select(c =>
			{
				if (c is Product<TResult> p)
				{
					var reduced = p.ReductionWithMutlipleExtracted(catalog, out IConstant<TResult> multiple);
					if (multiple == null) multiple = one;

					return (
						reduced.ToStringRepresentation(),
						multiple,
						reduced
					);
				}
				else
				{
					return (
						c.ToStringRepresentation(),
						one,
						c
					);
				}
			});

			var zero = catalog.GetConstant((TResult)(dynamic)0);

			// Phase 4: Replace multipliable products with single merged version.
			return catalog.SumOf(
				withMultiples
					.GroupBy(g => g.Item1)
					.Select(g => (
						catalog.SumOfConstants(g.Select(t => t.multiple)),
						g.First().reduced
					))
					.Where(i => i.Item1 != zero)
					.Select(i => i.Item1 == one
						? i.reduced
						: catalog.GetReduced(catalog.ProductOf(i.Item1, i.reduced))
					)
			);
		}

		internal static Sum<TResult> Create(
			ICatalog<IEvaluate<TResult>> catalog,
			IEnumerable<IEvaluate<TResult>> param)
		{
			return catalog.Register(new Sum<TResult>(param));
		}

		public virtual IEvaluate NewUsing(
			ICatalog<IEvaluate> catalog,
			IEnumerable<IEvaluate<TResult>> param)
		{
			return catalog.Register(new Sum<TResult>(param));
		}
	}

	public static partial class SumExtensions
	{
		public static IEvaluate<TResult> SumOf<TResult>(
			this ICatalog<IEvaluate<TResult>> catalog,
			IEnumerable<IEvaluate<TResult>> children)
			where TResult : struct, IComparable
		{
			var childList = children.ToList();
			var constants = childList.ExtractType<IConstant<TResult>>();
			if (constants.Count > 0)
			{
				var c = constants.Count == 1 ? constants.Single() : catalog.SumOfConstants(constants);
				if (childList.Count == 0)
					return c;

				childList.Add(c);
			}
			else if (childList.Count == 0)
			{
				return ConstantExtensions.GetConstant<TResult>(catalog, (dynamic)0);
			}
			else if (childList.Count == 1)
			{
				return childList.Single();
			}

			return Sum<TResult>.Create(catalog, childList);
		}

		public static IEvaluate<TResult> SumOf<TResult>(
			this ICatalog<IEvaluate<TResult>> catalog,
			params IEvaluate<TResult>[] children)
			where TResult : struct, IComparable
		{
			return SumOf(catalog, (IEnumerable<IEvaluate<TResult>>)children);
		}

	}

}
