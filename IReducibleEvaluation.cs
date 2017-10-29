/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Hierarchy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Open.Evaluation
{
	public interface IReducibleEvaluation<out TResult> : IEvaluate<TResult>
	{
		/// <returns>Null if no reduction possible.  Otherwise returns the reduction.</returns>
		IEvaluate<TResult> Reduction();

		/// <returns>The reduced version if possible otherwise returns the current instance.</returns>
		IEvaluate<TResult> AsReduced();
	}

	public static class ReductionExtensions
	{
		public static IEvaluate<TResult> Reduction<TContext, TResult>(this IEvaluate<TResult> target)
		{
			if (target == null) throw new NullReferenceException();
			return (target as IReducibleEvaluation<TResult>)?.Reduction();
		}

		public static IEvaluate<TResult> AsReduced<TResult>(this IEvaluate<TResult> target)
		{
			if (target == null) throw new NullReferenceException();
			return (target as IReducibleEvaluation<TResult>)?.AsReduced() ?? target;
		}

		public static bool IsReducible<TResult>(this IEvaluate<TResult> target)
		{
			if (target == null) throw new NullReferenceException();
			return target.AsReduced() != target;
		}

		public static IEnumerable<IEvaluate<TResult>> Flatten<TFlat, TResult>(this IEnumerable<IEvaluate<TResult>> source)
			where TFlat : class, IParent<IEvaluate<TResult>>
		{

			// Phase 1: Flatten products of products.
			foreach (var child in source)
			{
				var c = child.AsReduced();
				var f = c as TFlat;
				if (f == null)
				{
					yield return c;
				}
				else
				{
					foreach (var sc in f.Children)
						yield return sc;
				}
			}
		}

		public static Constant<TResult>[] ExtractConstants<TResult>(this List<IEvaluate<TResult>> target)
			where TResult : IComparable
		{
			var constants = target.OfType<Constant<TResult>>().ToArray();
			foreach (var c in constants)
				target.Remove(c);

			return constants;
		}
	}
}
