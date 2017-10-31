/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Open.Evaluation.ArithmeticOperators
{
	public class Product<TResult> : OperatorBase<IEvaluate<TResult>, TResult>
		where TResult : struct, IComparable
	{
		public Product(IEnumerable<IEvaluate<TResult>> children = null)
			: base(Product.SYMBOL, Product.SEPARATOR, children, true)
		{ }

		public Product(IEvaluate<TResult> first, params IEvaluate<TResult>[] rest)
			: this(Enumerable.Repeat(first,1).Concat(rest))
		{ }

		protected override TResult EvaluateInternal(object context)
		{
			if (ChildrenInternal.Count == 0)
				throw new InvalidOperationException("Cannot resolve product of empty set.");

			dynamic result = 1;
			foreach (var r in ChildResults(context))
			{
				result *= r;
			}

			return result;
		}

		protected override IEvaluate<TResult> Reduction(ICatalog<IEvaluate<TResult>> catalog)
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

			// Phase 3&4: Sum compatible exponents together.
			foreach (var exponents in children.OfType<Exponent<TResult>>()
				.GroupBy(g => g.Base.ToStringRepresentation())
				.Where(g => g.Count() > 1))
			{
				var newBase = exponents.First().Base; // reduction already done above during flatten...
				var power = catalog.GetReduced(new Sum<TResult>(exponents.Select(t => t.Power)));
				foreach (var e in exponents)
					children.Remove(e);

				children.Add(new Exponent<TResult>(newBase, power));
			}

			// Phase 5: Combine constants.
			var constants = children.ExtractType<Constant<TResult>>();
			if (constants.Count != 0)
				children.Add(constants.Count == 1 ? constants[0] : constants.Product());

			// Phase 6: Check if collapsable and return.
			return catalog.Register(children.Count == 1 ? children[0] : new Product<TResult>(children));
		}

		public IEvaluate<TResult> ReductionWithMutlipleExtracted(ICatalog<IEvaluate<TResult>> catalog, out Constant<TResult> multiple)
		{
			multiple = null;
			var reduced = catalog.GetReduced(this);
			if (reduced is Product<TResult> product)
			{
				var children = product.ChildrenInternal.ToList(); // Make a copy to be worked on...
				var constants = children.ExtractType<Constant<TResult>>();
				Debug.Assert(constants.Count <= 1, "Reduction should have collapsed constants.");
				if (constants.Count == 0)
				{
					return product;
				}
				multiple = constants.Single();
				return new Product<TResult>(children);
			}
			return reduced;
		}


		public override IEvaluate NewUsing(IEnumerable<IEvaluate<TResult>> param)
		{
			return new Product<TResult>(param);
		}
	}

	public class Product : Product<double>
	{
		public static Product<TResult> Of<TResult>(IEvaluate<TResult> first, params IEvaluate<TResult>[] rest)
			where TResult : struct, IComparable
		{
			return new Product<TResult>(first, rest);
		}

		public static Product<TResult> Of<TResult>(IEnumerable<IEvaluate<TResult>> children)
			where TResult : struct, IComparable
		{
			return new Product<TResult>(children);
		}

		public static Product Of(IEvaluate<double> first, params IEvaluate<double>[] rest)
		{
			return new Product(first, rest);
		}

		public static Product Of(IEnumerable<IEvaluate<double>> children)
		{
			return new Product(children);
		}


		public const char SYMBOL = '*';
		public const string SEPARATOR = " * ";

		public Product(IEnumerable<IEvaluate<double>> children = null) : base(children)
		{
		}

		public Product(IEvaluate<double> first, params IEvaluate<double>[] rest)
			: this(Enumerable.Repeat(first, 1).Concat(rest))
		{ }

		public override IEvaluate CreateNewUsing(IEnumerable<IEvaluate<double>> param)
		{
			return new Product(param);
		}
	}

	public static class ProductExtensions
	{
		public static Constant<TResult> Product<TResult>(this IEnumerable<Constant<TResult>> constants)
			where TResult : struct, IComparable
		{
			var list = constants as IList<Constant<TResult>> ?? constants.ToList();
			switch (list.Count)
			{
				case 0:
					return new Constant<TResult>(default(TResult));
				case 1:
					return list[0];
			}

			dynamic result = 1;
			foreach (var c in constants)
			{
				result *= c.Value;
			}

			return new Constant<TResult>(result);
		}


		public static Product<float> Product<TContext>(this IEnumerable<IEvaluate<float>> evaluations)
		{
			return new Product<float>(evaluations);
		}

		public static Product<double> Product<TContext>(this IEnumerable<IEvaluate<double>> evaluations)
		{
			return new Product<double>(evaluations);
		}

		public static Product<decimal> Product<TContext>(this IEnumerable<IEvaluate<decimal>> evaluations)
		{
			return new Product<decimal>(evaluations);
		}

		public static Product<short> Product<TContext>(this IEnumerable<IEvaluate<short>> evaluations)
		{
			return new Product<short>(evaluations);
		}

		public static Product<ushort> Product<TContext>(this IEnumerable<IEvaluate<ushort>> evaluations)
		{
			return new Product<ushort>(evaluations);
		}

		public static Product<int> Product<TContext>(this IEnumerable<IEvaluate<int>> evaluations)
		{
			return new Product<int>(evaluations);
		}

		public static Product<uint> Product<TContext>(this IEnumerable<IEvaluate<uint>> evaluations)
		{
			return new Product<uint>(evaluations);
		}

		public static Product<long> Product<TContext>(this IEnumerable<IEvaluate<long>> evaluations)
		{
			return new Product<long>(evaluations);
		}

		public static Product<ulong> Product<TContext>(this IEnumerable<IEvaluate<ulong>> evaluations)
		{
			return new Product<ulong>(evaluations);
		}

	}


}