using System;
using System.Collections.Generic;

namespace EvaluationFramework.ArithmeticOperators
{
	public class Sum<TContext, TResult> : OperatorBase<IEvaluate<TContext, TResult>, TContext, TResult>
		where TResult : struct, IComparable
	{
		public const string SYMBOL = " + ";
		public Sum(IEnumerable<IEvaluate<TContext, TResult>> children = null)
			: base(SYMBOL, children)
		{

		}

		public override TResult Evaluate(TContext context)
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

	}

	public static class Sum
	{
		public static Sum<TContext, TResult> Of<TContext, TResult>(params IEvaluate<TContext, TResult>[] evaluations)
		where TResult : struct, IComparable
		{
			return new Sum<TContext, TResult>(evaluations);
		}
	}

	public static class SumExtensions
	{
		public static Sum<TContext, float> Sum<TContext>(this IEnumerable<IEvaluate<TContext, float>> evaluations)
		{
			return new Sum<TContext, float>(evaluations);
		}

		public static Sum<TContext, double> Sum<TContext>(this IEnumerable<IEvaluate<TContext, double>> evaluations)
		{
			return new Sum<TContext, double>(evaluations);
		}

		public static Sum<TContext, decimal> Sum<TContext>(this IEnumerable<IEvaluate<TContext, decimal>> evaluations)
		{
			return new Sum<TContext, decimal>(evaluations);
		}

		public static Sum<TContext, short> Sum<TContext>(this IEnumerable<IEvaluate<TContext, short>> evaluations)
		{
			return new Sum<TContext, short>(evaluations);
		}

		public static Sum<TContext, ushort> Sum<TContext>(this IEnumerable<IEvaluate<TContext, ushort>> evaluations)
		{
			return new Sum<TContext, ushort>(evaluations);
		}


		public static Sum<TContext, int> Sum<TContext>(this IEnumerable<IEvaluate<TContext, int>> evaluations)
		{
			return new Sum<TContext, int>(evaluations);
		}

		public static Sum<TContext, uint> Sum<TContext>(this IEnumerable<IEvaluate<TContext, uint>> evaluations)
		{
			return new Sum<TContext, uint>(evaluations);
		}

		public static Sum<TContext, long> Sum<TContext>(this IEnumerable<IEvaluate<TContext, long>> evaluations)
		{
			return new Sum<TContext, long>(evaluations);
		}

		public static Sum<TContext, ulong> Sum<TContext>(this IEnumerable<IEvaluate<TContext, ulong>> evaluations)
		{
			return new Sum<TContext, ulong>(evaluations);
		}

	}


}