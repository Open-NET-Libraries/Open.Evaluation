/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Evaluation.Core;
using Open.Numeric.Primes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

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

		static (bool found, IConstant<TResult> value) IsProductWithSingleConstant(IEvaluate<TResult> a)
		{
			if (!(a is Product<TResult> aP)) return (false, null);
			var constants = aP.Children.OfType<IConstant<TResult>>().ToArray();
			return constants.Length == 1 ? (true, constants[0]) : (false, null);
		}

		protected override int Compare(IEvaluate<TResult> a, IEvaluate<TResult> b)
		{
			var (aFound, aConstant) = IsProductWithSingleConstant(a);
			var (bFound, bConstant) = IsProductWithSingleConstant(b);
			if (aFound && bFound)
			{
				var result = base.Compare(aConstant, bConstant);
				if (result != 0) return result;
			}
			else if (aFound)
			{
				if (0 > (dynamic)aConstant)
					return +1;
			}
			else if (bFound)
			{
				if (0 > (dynamic)bConstant)
					return -1;
			}

			return base.Compare(a, b);
		}

		// ReSharper disable once StaticMemberInGenericType
		static readonly Regex HasNegativeMultiple = new Regex(@"^\(-(\d+)(\s*[\*\/]\s*)(.+)\)$|^-(\d+)$", RegexOptions.Compiled);

		protected override void ToStringInternal_OnAppendNextChild(StringBuilder result, int index, object child)
		{
			if (index != 0 && child is string c)
			{
				var m = HasNegativeMultiple.Match(c);
				if (m.Success)
				{
					result.Append(" - ");
					result.Append(m.Groups[4].Success
						? m.Groups[4].Value
						: m.Groups[1].Value == "1"
							? $"({m.Groups[3].Value})"
							: $"({m.Groups[1].Value}{m.Groups[2].Value}{m.Groups[3].Value})");
					return;
				}
			}

			base.ToStringInternal_OnAppendNextChild(result, index, child);
		}

		protected override TResult EvaluateInternal(object context)
			=> ChildResults(context)
				.Cast<TResult>()
				.Aggregate<TResult, dynamic>(0, (current, r) => current + r);

		protected override IEvaluate<TResult> Reduction(ICatalog<IEvaluate<TResult>> catalog)
		{
			var zero = GetConstant(catalog, (TResult)(dynamic)0);

			// Phase 1: Flatten sums of sums.
			var children = catalog.Flatten<Sum<TResult>>(ChildrenInternal.Select(a =>
			{
				// Check for products that can be flattened as well.
				if (!(a is Product<TResult> aP) || aP.Children.Count != 2) return a;

				var aS = aP.Children.OfType<Sum<TResult>>().ToArray();
				if (aS.Length != 1) return a;

				var aC = aP.Children.OfType<IConstant<TResult>>().ToArray();
				if (aC.Length != 1) return a;

				var aCv = aC[0];
				return catalog.SumOf(aS[0].Children.Select(c => catalog.ProductOf(aCv, c)));

			})).Where(c => c != zero).ToList(); // ** chidren's reduction is done here.

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
			var withMultiples = catalog.MultiplesExtracted(children, true).ToArray();

			// Phase 4: Replace multipliable products with single merged version.
			return catalog.SumOf(
				withMultiples
					.GroupBy(g => g.Hash)
					.Select(g => (
						multiple: catalog.SumOfConstants(g.Select(t => t.Multiple ?? one)),
						first: g.First().Entry
					))
					.Where(i => i.multiple != zero)
					.Select(i => i.multiple == one
						? i.first
						: catalog.GetReduced(catalog.ProductOf(i.multiple, i.first))
					));
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

		public bool TryExtractGreatestFactor(
			ICatalog<IEvaluate<TResult>> catalog,
			out IEvaluate<TResult> sum,
			out IConstant<TResult> greatestFactor)
		{
			var one = GetConstant(catalog, (TResult)(dynamic)1);
			greatestFactor = one;
			sum = this;
			// Phase 5: Try and group by GCF:
			var products = new List<Product<TResult>>();
			foreach (var c in Children)
			{
				// All of them must be products for GCF to work.
				if (c is Product<TResult> p)
					products.Add(p);
				else
					return false;
			}

			// Try and get all the constants, and if a product does not have one, then done.
			var constants = new List<TResult>();
			foreach (var p in products)
			{
				var c = p.Children.OfType<IConstant<TResult>>().ToArray();
				if (c.Length != 1) return false;
				constants.Add(c[0].Value);
			}

			// Convert all the constants to factors, and if any are invalid for factoring, then done.
			var factors = new List<ulong>();
			foreach (var v in constants)
			{
				var d = Math.Abs(Convert.ToDecimal(v));
				if (d <= decimal.One || decimal.Floor(d) != d) return false;
				factors.Add(Convert.ToUInt64(d));
			}
			var gcf = Prime.GreatestFactor(factors);
			Debug.Assert(factors.All(f => f >= gcf));
			if (gcf <= 1) return false;

			bool tryGetReducedFactor(TResult value, out TResult f)
			{
				var r = (dynamic)value / gcf;
				f = r;
				return r != 1;
			}

			greatestFactor = GetConstant(catalog, (dynamic)gcf);
			sum = catalog.SumOf(catalog.MultiplesExtracted(products)
				.Select(e =>
				{
					var m = e.Multiple ?? one;
					if (m != one && tryGetReducedFactor(m.Value, out var f))
						return catalog.ProductOf(f, e.Entry);

					return e.Entry;
				}));

			return true;
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
