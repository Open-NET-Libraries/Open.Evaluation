﻿/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Evaluation.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Open.Evaluation.Arithmetic
{
	public class Sum<TResult> :
		OperatorBase<IEvaluate<TResult>, TResult>,
		IReproducable<IEnumerable<IEvaluate<TResult>>, IEvaluate<TResult>>
		where TResult : struct, IComparable
	{
		protected Sum(IEnumerable<IEvaluate<TResult>> children = null)
			: base(Sum.SYMBOL, Sum.SEPARATOR, children, true)
		{ }

		protected override TResult EvaluateInternal(object context)
			=> ChildResults(context)
				.Cast<TResult>()
				.Aggregate<TResult, dynamic>(0, (current, r) => current + r);

		protected override IEvaluate<TResult> Reduction(ICatalog<IEvaluate<TResult>> catalog)
		{
			var zero = GetConstant(catalog, (TResult)(dynamic)0);

			// Phase 1: Flatten sums of sums.
			var children = catalog.Flatten<Sum<TResult>>(ChildrenInternal).Where(c => c != zero).ToList(); // ** chidren's reduction is done here.

			// Phase 2: Can we collapse?
			switch (children.Count)
			{
				case 0:
					return GetConstant(catalog, (TResult)(dynamic)0);
				case 1:
					return children[0];
			}

			if (typeof(TResult) == typeof(float) && children.Any(c => c is IConstant<float> d && float.IsNaN(d.Value)))
				return GetConstant(catalog, (TResult)(dynamic)float.NaN);

			if (typeof(TResult) == typeof(double) && children.Any(c => c is IConstant<double> d && double.IsNaN(d.Value)))
				return GetConstant(catalog, (TResult)(dynamic)double.NaN);

			var one = GetConstant(catalog, (TResult)(dynamic)1);

			// Phase 3: Look for groupings by "multiples".
			var withMultiples = children.Select(c =>
			{
				if (!(c is Product<TResult> p))
					return (c.ToStringRepresentation(), one, c);

				var reduced = p.ReductionWithMutlipleExtracted(catalog, out var multiple);
				if (multiple == null) multiple = one;

				return (
					hash: reduced.ToStringRepresentation(),
					multiple,
					reduced
				);

			});



			// Phase 4: Replace multipliable products with single merged version.
			return catalog.SumOf(
				withMultiples
					.GroupBy(g => g.hash)
					.Select(g => (
						multiple: catalog.SumOfConstants(g.Select(t => t.multiple)),
						g.First().reduced
					))
					.Where(i => i.multiple != zero)
					.Select(i => i.multiple == one
						? i.reduced
						: catalog.GetReduced(catalog.ProductOf(i.multiple, i.reduced))
					)
			);
		}

		internal static Sum<TResult> Create(
			ICatalog<IEvaluate<TResult>> catalog,
			IEnumerable<IEvaluate<TResult>> param)
		{
			Debug.Assert(catalog != null);
			Debug.Assert(param != null);

			// ReSharper disable once SuspiciousTypeConversion.Global
			if (catalog is ICatalog<IEvaluate<double>> dCat && param is IEnumerable<IEvaluate<double>> p)
				return (dynamic)Sum.Create(dCat, p);

			return catalog.Register(new Sum<TResult>(param));
		}

		public virtual IEvaluate<TResult> NewUsing(
			ICatalog<IEvaluate<TResult>> catalog,
			IEnumerable<IEvaluate<TResult>> param)
		{
			Debug.Assert(param != null);
			var p = param as IEvaluate<TResult>[] ?? param.ToArray();
			return p.Length == 1 ? p[0] : Create(catalog, p);
		}

	}

	public static partial class SumExtensions
	{
		public static IEvaluate<TResult> SumOf<TResult>(
			this ICatalog<IEvaluate<TResult>> catalog,
			IEnumerable<IEvaluate<TResult>> children)
			where TResult : struct, IComparable
		{
			if (catalog == null) throw new ArgumentNullException(nameof(catalog));
			if (children == null) throw new ArgumentNullException(nameof(children));
			Contract.EndContractBlock();

			var childList = children.ToList();
			switch (childList.Count)
			{
				case 0:
					return ConstantExtensions.GetConstant<TResult>(catalog, (dynamic)0);
				case 1:
					return childList.Single();
				default:
					var constants = childList.ExtractType<IConstant<TResult>>();

					if (constants.Count == 0)
						return Sum<TResult>.Create(catalog, childList);

					var c = constants.Count == 1
						? constants[0]
						: catalog.SumOfConstants(constants);

					if (childList.Count == 0)
						return c;

					childList.Add(c);

					return Sum<TResult>.Create(catalog, childList);
			}
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
