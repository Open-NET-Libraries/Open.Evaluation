using System;
using System.Collections.Generic;

namespace EvaluationFramework.ArithmeticOperators
{
	public class Product<TContext, TResult> : OperatorBase<IEvaluate<TContext, TResult>, TContext, TResult>
		where TResult : struct, IComparable
	{
		public const string SYMBOL = " + ";
		public Product(IEnumerable<IEvaluate<TContext, TResult>> children = null)
			: base(SYMBOL, children)
		{

		}

		public override TResult Evaluate(TContext context)
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

	}

	public static class Product
	{
		public static Product<TContext, TResult> Of<TContext, TResult>(params IEvaluate<TContext, TResult>[] evaluations)
		where TResult : struct, IComparable
		{
			return new Product<TContext, TResult>(evaluations);
		}
	}

	public static class ProductExtensions
	{
		public static Product<TContext, float> Product<TContext>(this IEnumerable<IEvaluate<TContext, float>> evaluations)
		{
			return new Product<TContext, float>(evaluations);
		}

		public static Product<TContext, double> Product<TContext>(this IEnumerable<IEvaluate<TContext, double>> evaluations)
		{
			return new Product<TContext, double>(evaluations);
		}

		public static Product<TContext, decimal> Product<TContext>(this IEnumerable<IEvaluate<TContext, decimal>> evaluations)
		{
			return new Product<TContext, decimal>(evaluations);
		}

		public static Product<TContext, short> Product<TContext>(this IEnumerable<IEvaluate<TContext, short>> evaluations)
		{
			return new Product<TContext, short>(evaluations);
		}

		public static Product<TContext, ushort> Product<TContext>(this IEnumerable<IEvaluate<TContext, ushort>> evaluations)
		{
			return new Product<TContext, ushort>(evaluations);
		}


		public static Product<TContext, int> Product<TContext>(this IEnumerable<IEvaluate<TContext, int>> evaluations)
		{
			return new Product<TContext, int>(evaluations);
		}

		public static Product<TContext, uint> Product<TContext>(this IEnumerable<IEvaluate<TContext, uint>> evaluations)
		{
			return new Product<TContext, uint>(evaluations);
		}

		public static Product<TContext, long> Product<TContext>(this IEnumerable<IEvaluate<TContext, long>> evaluations)
		{
			return new Product<TContext, long>(evaluations);
		}

		public static Product<TContext, ulong> Product<TContext>(this IEnumerable<IEvaluate<TContext, ulong>> evaluations)
		{
			return new Product<TContext, ulong>(evaluations);
		}

	}


}