/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Evaluation.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Open.Evaluation.Arithmetic
{
	public class Product<TResult> :
		OperatorBase<IEvaluate<TResult>, TResult>,
		IReproducable<IEnumerable<IEvaluate<TResult>>, IEvaluate<TResult>>
		where TResult : struct, IComparable
	{
		protected Product(IEnumerable<IEvaluate<TResult>> children)
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

			return ChildResults(context)
				.Cast<TResult>()
				.Aggregate<TResult, dynamic>(1, (current, r) => current * r);
		}

		protected override IEvaluate<TResult> Reduction(
			ICatalog<IEvaluate<TResult>> catalog)
		{
			var one = catalog.GetConstant((TResult)(dynamic)1);

			// Phase 1: Flatten products of products.
			var children = catalog
				.Flatten<Product<TResult>>(ChildrenInternal)
				.Where(c => c != one)
				.ToList(); // ** chidren's reduction is done here.

			// Phase 3: Try to extract common multiples...
			var len = children.Count;
			for (var i = 0; i < len; i++)
			{
				var child = children[i];
				if (!(child is Sum<TResult> sum) ||
					!sum.TryExtractGreatestFactor(catalog, out var newSum, out var gcf))
					continue;

				children[i] = newSum; // Replace with extraction...
				children.Add(gcf); // Append the constant..
			}

			// Phase 3: Can we collapse?
			switch (children.Count)
			{
				case 0:
					if (ChildrenInternal.Count == 0)
						throw new InvalidOperationException("Cannot reduce product of empty set.");

					return one;
				case 1:
					return children[0];
			}

			// Phase 4: Deal with special case constants.
			if (typeof(TResult) == typeof(float) && children.Any(c => c is IConstant<float> d && float.IsNaN(d.Value)))
				return GetConstant(catalog, (TResult)(dynamic)float.NaN);

			if (typeof(TResult) == typeof(double) && children.Any(c => c is IConstant<double> d && double.IsNaN(d.Value)))
				return GetConstant(catalog, (TResult)(dynamic)double.NaN);

			var zero = catalog.GetConstant((TResult)(dynamic)0); // This could be a problem in the future. What zero?  0d?  0f? :/
			if (children.Any(c => c == zero))
				return zero;

			var cat = catalog;
			// Phase 5: Convert to exponents.
			var exponents = children.Select(c =>
				c is Exponent<TResult> e
				? (cat.GetReduced(e.Base), e.Power)
				: (c, one)).ToArray();

			if (exponents.Any(c => c.Item1 == zero))
				return zero;

			children = exponents
				.Where(e => e.Power != zero)
				.GroupBy(e => e.Item1.ToStringRepresentation())
				.Select(g =>
				{
					var @base = g.First().Item1;
					var sumPower = cat.SumOf(g.Select(t => t.Power));
					var power = cat.GetReduced(sumPower);
					if (power == zero) return one;
					return power == one
						? @base
						: GetExponent(catalog, @base, power);

				}).ToList();

			return children.Count == 1
				? children[0]
				: catalog.ProductOf(children);

		}

		protected virtual Exponent<TResult> GetExponent(ICatalog<IEvaluate<TResult>> catalog,
			IEvaluate<TResult> @base,
			IEvaluate<TResult> power)
			=> Exponent<TResult>.Create(catalog, @base, power);

		public IEvaluate<TResult> ExtractMultiple(ICatalog<IEvaluate<TResult>> catalog, out IConstant<TResult> multiple)
		{
			multiple = null;

			if (!ChildrenInternal.OfType<IConstant<TResult>>().Any()) return this;

			var children = ChildrenInternal.ToList(); // Make a copy to be worked on...
			var constants = children.ExtractType<IConstant<TResult>>();
			if (constants.Count == 0) return this;

			multiple = catalog.ProductOfConstants(constants);
			return NewUsing(catalog, children);
		}

		public IEvaluate<TResult> ReductionWithMutlipleExtracted(ICatalog<IEvaluate<TResult>> catalog, out IConstant<TResult> multiple)
		{
			multiple = null;
			var reduced = catalog.GetReduced(this);
			return reduced is Product<TResult> product
				? product.ExtractMultiple(catalog, out multiple)
				: reduced;
		}

		static (bool found, IConstant<TResult> value) IsExponentWithConstantPower(IEvaluate<TResult> a)
			=> (a is Exponent<TResult> aP && aP.Power is IConstant<TResult> c) ? (true, c) : (false, null);

		protected override int Compare(IEvaluate<TResult> a, IEvaluate<TResult> b)
		{
			var (aFound, aConstant) = IsExponentWithConstantPower(a);
			var (bFound, bConstant) = IsExponentWithConstantPower(b);
			if (aFound && bFound)
			{
				var result = base.Compare(aConstant, bConstant);
				if (result != 0) return result;
			}
			else if (aFound)
			{
				var v = (dynamic)aConstant;
				if (1 > v)
					return +1;
				if (1 < v)
					return -1;

			}
			else if (bFound)
			{
				var v = (dynamic)bConstant;
				if (1 > v)
					return -1;
				if (1 < v)
					return +1;
			}

			return base.Compare(a, b);
		}

		public static Product<TResult> Create(
			ICatalog<IEvaluate<TResult>> catalog,
			IEnumerable<IEvaluate<TResult>> param)
		{
			Debug.Assert(catalog != null);
			Debug.Assert(param != null);
			// ReSharper disable once SuspiciousTypeConversion.Global
			if (catalog is ICatalog<IEvaluate<double>> dCat && param is IEnumerable<IEvaluate<double>> p)
				return (dynamic)Product.Create(dCat, p);

			return catalog.Register(new Product<TResult>(param));
		}

		public virtual IEvaluate<TResult> NewUsing(
			ICatalog<IEvaluate<TResult>> catalog,
			IEnumerable<IEvaluate<TResult>> param)
		{
			Debug.Assert(catalog != null);
			var p = param as IEvaluate<TResult>[] ?? param.ToArray();
			return p.Length == 1 ? p[0] : Create(catalog, p);
		}
	}

	public static partial class ProductExtensions
	{
		public static IEnumerable<(string Hash, IConstant<TResult> Multiple, IEvaluate<TResult> Entry)> MultiplesExtracted<TResult>(
			this ICatalog<IEvaluate<TResult>> catalog,
			IEnumerable<IEvaluate<TResult>> source, bool reduce = false)
			where TResult : struct, IComparable
		{
			return source.Select(c =>
			{
				if (!(c is Product<TResult> p))
					return (c.ToStringRepresentation(), null, c);

				var reduced = reduce
					? p.ReductionWithMutlipleExtracted(catalog, out var multiple)
					: p.ExtractMultiple(catalog, out multiple);

				return (
					reduced.ToStringRepresentation(),
					multiple,
					reduced
				);
			});
		}

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
				var c = constants.Count == 1
					? constants.Single()
					: catalog.ProductOfConstants(constants);
				if (childList.Count == 0)
					return c;

				switch (c)
				{
					case IConstant<float> f when float.IsNaN(f.Value):
						return catalog.GetConstant((TResult)(dynamic)float.NaN);
					case IConstant<double> d when double.IsNaN(d.Value):
						return catalog.GetConstant((TResult)(dynamic)double.NaN);
				}

				// No need to multiply by 1.
				if (c != catalog.GetConstant((TResult)(dynamic)1))
					childList.Add(c);
			}
			else
			{
				switch (childList.Count)
				{
					case 0:
						//Debug.Fail("Extraction failure.", "Should not have occured.");
						throw new Exception("Extraction failure.");
					case 1:
						return childList.Single();
				}
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

		[SuppressMessage("ReSharper", "RedundantCast")]
		public static IEvaluate<TResult> ProductOf<TResult>(
			this ICatalog<IEvaluate<TResult>> catalog,
			in TResult multiple,
			params IEvaluate<TResult>[] rest)
			where TResult : struct, IComparable
			=> ProductOf(catalog, (IEvaluate<TResult>)catalog.GetConstant(multiple), (IEnumerable<IEvaluate<TResult>>)rest);

	}
}
