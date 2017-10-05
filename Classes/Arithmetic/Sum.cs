/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace Open.Evaluation.ArithmeticOperators
{
	public class Sum<TResult> : OperatorBase<IEvaluate<TResult>, TResult>
		where TResult : struct, IComparable
	{
		public Sum(IEnumerable<IEvaluate<TResult>> children = null)
			: base(Sum.SYMBOL, Sum.SEPARATOR, children)
		{

		}

		protected override TResult EvaluateInternal(object context)
		{
			if (ChildrenInternal.Count == 0)
				throw new InvalidOperationException("Cannot resolve sum of empty set.");

			dynamic result = 0;
			foreach (var r in ChildResults(context))
			{
				result += r;
			}

			return result;
		}

		public override IEvaluate<TResult> Reduction()
		{
			// Phase 1: Flatten sums of sums.
			var children = ChildrenInternal.Flatten<Sum<TResult>, TResult>().ToList();

			// Phase 2: Can we collapse?
			switch(children.Count)
			{
				case 0:
					return new Constant<TResult>((TResult)(dynamic)0);
				case 1:
					return children[0];
			}

			// Phase 3: Look for groupings: constant multplied products
			var productsWithConstants = new List<Tuple<string, Constant<TResult>, IEvaluate<TResult>, Product<TResult>>>();
			foreach (var p in children.OfType<Product<TResult>>())
			{
				Constant<TResult> multiple;
				var reduced = p.ReductionWithMutlipleExtracted(out multiple);
				if (multiple != null)
				{
					productsWithConstants.Add(new Tuple<string, Constant<TResult>, IEvaluate<TResult>, Product<TResult>>(
						reduced.ToStringRepresentation(),
						multiple,
						reduced,
						p
					));
				}
			}

			// Phase 4: Replace multipliable products with single merged version.
			foreach (var p in productsWithConstants
				.GroupBy(g => g.Item1)
				.Where(g => g.Count() > 1))
			{
				var p1 = p.First();
				var multiple = p.Select(t => t.Item2).Sum();
				foreach (var px in p.Select(t => t.Item4))
					children.Remove(px);

				var replacement = new List<IEvaluate<TResult>>();
				replacement.Add(p1.Item3);
				replacement.Add(multiple);
				children.Add(new Product<TResult>(replacement));
			}


			// Phase 5: Combine constants.
			var constants = children.ExtractConstants();
			if (constants.Length!=0)
				children.Add(constants.Length==1 ? constants[0] : constants.Sum());

			// Phase 6: Check if collapsable?
			if (children.Count == 1)
				return children[0];

			// Lastly: Sort and return if different.
			children.Sort(Compare);
			var result = new Sum<TResult>(children);

			return result.ToStringRepresentation() == result.ToStringRepresentation() ? null : result;
		}

	}

	public class Sum : Sum<double>
	{
		public const char SYMBOL = '+';
		public const string SEPARATOR = " + ";

		public Sum(IEnumerable<IEvaluate<double>> children = null)
			: base(children)
		{

		}

		public static Sum<TResult> Of<TResult>(params IEvaluate<TResult>[] evaluations)
		where TResult : struct, IComparable
		{
			return new Sum<TResult>(evaluations);
		}
	}

	public static class SumExtensions
	{
		public static Constant<TResult> Sum<TResult>(this IEnumerable<Constant<TResult>> constants)
		where TResult : struct, IComparable
		{
			var list = constants as IList<Constant<TResult>> ?? constants.ToList();
			switch (list.Count)
			{
				case 0:
					return new Constant<TResult>((TResult)(dynamic)0);
				case 1:
					return list[0];
			}

			dynamic result = 0;
			foreach (var c in constants)
			{
				result += c.Value;
			}

			return new Constant<TResult>(result);
		}

		public static Sum<float> Sum<TContext>(this IEnumerable<IEvaluate<float>> evaluations)
		{
			return new Sum<float>(evaluations);
		}

		public static Sum<double> Sum<TContext>(this IEnumerable<IEvaluate<double>> evaluations)
		{
			return new Sum<double>(evaluations);
		}

		public static Sum<decimal> Sum<TContext>(this IEnumerable<IEvaluate<decimal>> evaluations)
		{
			return new Sum<decimal>(evaluations);
		}

		public static Sum<short> Sum<TContext>(this IEnumerable<IEvaluate<short>> evaluations)
		{
			return new Sum<short>(evaluations);
		}

		public static Sum<ushort> Sum<TContext>(this IEnumerable<IEvaluate<ushort>> evaluations)
		{
			return new Sum<ushort>(evaluations);
		}


		public static Sum<int> Sum<TContext>(this IEnumerable<IEvaluate<int>> evaluations)
		{
			return new Sum<int>(evaluations);
		}

		public static Sum<uint> Sum<TContext>(this IEnumerable<IEvaluate<uint>> evaluations)
		{
			return new Sum<uint>(evaluations);
		}

		public static Sum<long> Sum<TContext>(this IEnumerable<IEvaluate<long>> evaluations)
		{
			return new Sum<long>(evaluations);
		}

		public static Sum<ulong> Sum<TContext>(this IEnumerable<IEvaluate<ulong>> evaluations)
		{
			return new Sum<ulong>(evaluations);
		}

	}


}