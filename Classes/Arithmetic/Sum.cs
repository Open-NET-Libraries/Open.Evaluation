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
		internal Sum(IEnumerable<IEvaluate<TResult>> children = null)
			: base(Sum.SYMBOL, Sum.SEPARATOR, children, true)
		{ }

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

		protected override IEvaluate<TResult> Reduction(ICatalog<IEvaluate<TResult>> catalog)
		{
			// Phase 1: Flatten sums of sums.
			var children = catalog.Flatten<Sum<TResult>>(ChildrenInternal).ToList(); // ** chidren's reduction is done here.

			// Phase 2: Can we collapse?
			switch (children.Count)
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
				var reduced = p.ReductionWithMutlipleExtracted(catalog, out Constant<TResult> multiple);
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
				var multiple = catalog.SumConstants(p.Select(t => t.Item2));
				foreach (var px in p.Select(t => t.Item4))
					children.Remove(px);

				children.Add(new Product<TResult>(
					p1.Item3,
					multiple));
			}

			return catalog.SumOf(children);
		}

		public override IEvaluate NewUsing(ICatalog<IEvaluate> catalog, IEnumerable<IEvaluate<TResult>> param)
		{
			return catalog.Register( new Sum<TResult>(param) );
		}
	}

	public class Sum : Sum<double>
	{
		public const char SYMBOL = '+';
		public const string SEPARATOR = " + ";

		internal Sum(IEnumerable<IEvaluate<double>> children = null)
			: base(children)
		{ }

		public override IEvaluate NewUsing(ICatalog<IEvaluate> catalog, IEnumerable<IEvaluate<double>> param)
		{
			return catalog.Register(new Sum(param));
		}
	}

	public static class SumExtensions
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
				var c = constants.Count == 1 ? constants.Single() : catalog.SumConstants(constants);
				if (childList.Count == 0)
					return c;

				childList.Add(c);
			}
			else if (childList.Count == 0)
			{
				return ConstantExtensions.GetConstant<TResult>(catalog, (dynamic)0);
			}
			else if(childList.Count==1)
			{
				return childList.Single();
			}

			return catalog.Register(new Sum<TResult>(childList));
		}

		public static IEvaluate<double> SumOf(
			this ICatalog<IEvaluate<double>> catalog,
			IEnumerable<IEvaluate<double>> children)
		{
			var childList = children.ToList();
			var constants = childList.ExtractType<IConstant<double>>();
			if (constants.Count > 0)
			{
				var c = constants.Count == 1 ? constants.Single() : catalog.SumConstants(constants);
				if (childList.Count == 0)
					return c;

				childList.Add(c);
			}
			else if (childList.Count == 0)
			{
				return catalog.GetConstant(0);
			}
			else if (childList.Count == 1)
			{
				return childList.Single();
			}

			return catalog.Register(new Sum(childList));
		}
	}

}