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
			: base(Product.SYMBOL, Product.SEPARATOR, children)
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

		public override IEvaluate<TResult> Reduction()
		{
			// Phase 1: Flatten products of products.
			var children = ChildrenInternal.Flatten<Product<TResult>, TResult>().ToList();

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
				.GroupBy(g => g.Evaluation.ToStringRepresentation())
				.Where(g => g.Count() > 1))
			{
				var e1 = exponents.First();
				var power = new Sum<TResult>(exponents.Select(t => t.Power));
				foreach (var e in exponents)
					children.Remove(e);

				children.Add(new Exponent<TResult>(e1.Evaluation, power.AsReduced()));
			}

			// Phase 5: Combine constants.
			var constants = children.ExtractConstants();
			if (constants.Length != 0)
				children.Add(constants.Length == 1 ? constants[0] : constants.Product());

			// Phase 6: Check if collapsable?
			if (children.Count == 1)
				return children[0];

			// Lastly: Sort and return if different.
			children.Sort(Compare);
			var result = new Product<TResult>(children);

			return result.ToStringRepresentation() == result.ToStringRepresentation() ? null : result;
		}

		public IEvaluate<TResult> ReductionWithMutlipleExtracted(out Constant<TResult> multiple)
		{
			multiple = null;
			var reduced = this.AsReduced();
			var product = reduced as Product<TResult>;
			if (product != null)
			{
				var children = product.ChildrenInternal.ToList();
				var constants = product.ChildrenInternal.OfType<Constant<TResult>>().ToArray();
				Debug.Assert(constants.Length <= 1, "Reduction should have collapsed constants.");
				if (constants.Length == 0)
					return product;
				multiple = constants.Single();
				children.Remove(multiple);
				return new Product<TResult>(children);
			}
			return reduced;
		}

		public override IEvaluate CreateNewFrom(object param, IEnumerable<IEvaluate> children)
		{
			Debug.WriteLineIf(param != null, "A param object was provided to a Product and will be lost. " + param);
			return new Product<TResult>(children.Cast<IEvaluate<TResult>>());
		}

	}

	public class Product : Product<double>
	{
		public const char SYMBOL = '*';
		public const string SEPARATOR = " * ";

		public Product(IEnumerable<IEvaluate<double>> children = null) : base(children)
		{
		}

		public Product(IEvaluate<double> first, params IEvaluate<double>[] rest)
			: this(Enumerable.Repeat(first, 1).Concat(rest))
		{ }

		public static Product<TResult> Of<TResult>(params IEvaluate<TResult>[] evaluations)
		where TResult : struct, IComparable
		{
			return new Product<TResult>(evaluations);
		}

		public override IEvaluate CreateNewFrom(object param, IEnumerable<IEvaluate> children)
		{
			Debug.WriteLineIf(param != null, "A param object was provided to a Product and will be lost. " + param);
			return new Product(children.Cast<IEvaluate<double>>());
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