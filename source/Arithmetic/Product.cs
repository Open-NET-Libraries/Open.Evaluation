/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Evaluation.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Open.Evaluation.Arithmetic
{
	public class Product<TResult> :
		OperatorBase<IEvaluate<TResult>, TResult>,
		IReproducable<IEnumerable<IEvaluate<TResult>>>
		where TResult : struct, IComparable
	{
		protected Product(IEnumerable<IEvaluate<TResult>> children = null)
			: base(Product.SYMBOL, Product.SEPARATOR, children, true)
		{ }

		protected Product(IEvaluate<TResult> first, params IEvaluate<TResult>[] rest)
			: this(Enumerable.Repeat(first, 1).Concat(rest))
		{ }

		protected override int ConstantPriority => -1;

		protected override TResult EvaluateInternal(object context)
		{
			if (ChildrenInternal.Count == 0)
				throw new InvalidOperationException("Cannot resolve product of empty set.");

			dynamic result = 1;
			foreach (var r in ChildResults(context).Cast<TResult>())
			{
				result *= r;
			}

			return result;
		}

		protected override IEvaluate<TResult> Reduction(
			ICatalog<IEvaluate<TResult>> catalog)
		{
			// Phase 1: Flatten products of products.
			var children = catalog.Flatten<Product<TResult>>(ChildrenInternal).ToList(); // ** chidren's reduction is done here.

			// Phase 2: Can we collapse?
			switch (children.Count)
			{
				case 0:
					throw new InvalidOperationException("Cannot reduce product of empty set.");
				case 1:
					return children[0];
			}

			if (typeof(TResult) == typeof(float) && children.Any(c => c is IConstant<float> d && float.IsNaN(d.Value)))
				return catalog.GetConstant((TResult)(dynamic)float.NaN);

			if (typeof(TResult) == typeof(double) && children.Any(c => c is IConstant<double> d && double.IsNaN(d.Value)))
				return catalog.GetConstant((TResult)(dynamic)double.NaN);

			var zero = catalog.GetConstant((TResult)(dynamic)0);
			if (children.Any(c => c == zero)) return zero;

			var one = catalog.GetConstant((TResult)(dynamic)1);

			var cat = catalog;
			// Phase 3: Convert to exponents.
			var exponents = children.Select(c =>
				c is Exponent<TResult> e
				? (cat.GetReduced(e.Base), e.Power)
				: (c, one)).ToArray();

			if (exponents.Any(c => c.Item1 == zero)) return zero;

			children = exponents
				.Where(e => e.Power != zero)
				.GroupBy(e => e.Item1.ToStringRepresentation())
				.Select(g =>
				{
					var @base = g.First().Item1;
					var power = cat.GetReduced(cat.SumOf(g.Select(t => t.Power)));
					if (power == one) return @base;
					return cat.GetExponent(@base, power);
				}).ToList();

			return children.Count == 1 ? children[0] : catalog.ProductOf(children);

		}

		public IEvaluate<TResult> ExtractMultiple(ICatalog<IEvaluate<TResult>> catalog, out IConstant<TResult> multiple)
		{
			multiple = null;

			if(ChildrenInternal.OfType<IConstant<TResult>>().Any())
			{
				var children = ChildrenInternal.ToList(); // Make a copy to be worked on...
				var constants = children.ExtractType<IConstant<TResult>>();
				if (constants.Count != 0)
				{
					multiple = catalog.ProductOfConstants(constants);
					return new Product<TResult>(children);
				}
			}

			return this;
		}

		public IEvaluate<TResult> ReductionWithMutlipleExtracted(ICatalog<IEvaluate<TResult>> catalog, out IConstant<TResult> multiple)
		{
			multiple = null;
			var reduced = catalog.GetReduced(this);
			return reduced is Product<TResult> product
				? product.ExtractMultiple(catalog, out multiple)
				: reduced;
		}

		public static Product<TResult> Create(
			ICatalog<IEvaluate<TResult>> catalog,
			IEnumerable<IEvaluate<TResult>> param)
			=> catalog.Register(new Product<TResult>(param));

		public virtual IEvaluate NewUsing(
			ICatalog<IEvaluate> catalog,
			IEnumerable<IEvaluate<TResult>> param)
			=> catalog.Register(new Product<TResult>(param));

	}

	public static partial class ProductExtensions
	{
		public static IEvaluate<TResult> ProductOf<TResult>(
			this ICatalog<IEvaluate<TResult>> catalog,
			IEnumerable<IEvaluate<TResult>> children)
			where TResult : struct, IComparable
		{
			var childList = children.ToList();
			if (childList.Count == 0)
				throw new InvalidOperationException("Cannot produce a product of an empty set.");

			var constants = childList.ExtractType<IConstant<TResult>>();
			if (constants.Count > 0)
			{
				var c = constants.Count == 1 ? constants.Single() : catalog.ProductOfConstants(constants);
				if (childList.Count == 0)
					return c;

				if (c is IConstant<float> f && float.IsNaN(f.Value))
					return catalog.GetConstant((TResult)(dynamic)float.NaN);

				if (c is IConstant<double> d && double.IsNaN(d.Value))
					return catalog.GetConstant((TResult)(dynamic)double.NaN);

				var zero = catalog.GetConstant((TResult)(dynamic)0);

				if (c == zero) return zero;

				// No need to multiply by 1.
				if (c != catalog.GetConstant((TResult)(dynamic)1))
					childList.Add(c);
			}
			else if (childList.Count == 0)
			{
				Debug.Fail("Extraction failure.", "Should not have occured.");
				throw new Exception("Extraction failure.");
			}
			else if (childList.Count == 1)
			{
				return childList.Single();
			}

			return Product<TResult>.Create(catalog, childList);
		}

		public static IEvaluate<TResult> ProductOf<TResult>(
			this ICatalog<IEvaluate<TResult>> catalog,
			IEvaluate<TResult> multiple,
			IEnumerable<IEvaluate<TResult>> children)
			where TResult : struct, IComparable
			=> ProductOf(catalog, children.Concat(multiple));

		public static IEvaluate<TResult> ProductOf<TResult>(
			this ICatalog<IEvaluate<TResult>> catalog,
			params IEvaluate<TResult>[] children)
			where TResult : struct, IComparable
			=> ProductOf(catalog, (IEnumerable<IEvaluate<TResult>>)children);

		public static IEvaluate<TResult> ProductOf<TResult>(
			this ICatalog<IEvaluate<TResult>> catalog,
			in TResult multiple,
			IEnumerable<IEvaluate<TResult>> children)
			where TResult : struct, IComparable
			=> ProductOf(catalog, catalog.GetConstant(multiple), children);

		public static IEvaluate<TResult> ProductOf<TResult>(
			this ICatalog<IEvaluate<TResult>> catalog,
			in TResult multiple,
			params IEvaluate<TResult>[] rest)
			where TResult : struct, IComparable
			=> ProductOf(catalog, catalog.GetConstant(multiple), rest);

	}
}
