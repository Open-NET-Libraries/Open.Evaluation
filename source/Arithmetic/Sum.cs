/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Disposable;
using Open.Evaluation.Core;
using Open.Numeric.Primes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Open.Evaluation.Arithmetic
{
	public class Sum<TResult> :
		OperatorBase<TResult>,
		IReproducable<IEnumerable<IEvaluate<TResult>>, IEvaluate<TResult>>
		where TResult : struct, IComparable
	{
		protected Sum(IEnumerable<IEvaluate<TResult>> children)
			: base(Sum.SYMBOL, Sum.SEPARATOR, children, true)
		{ }

		static bool IsProductWithSingleConstant(
			IEvaluate<TResult> a,
			[NotNullWhen(true)] out IConstant<TResult> value)
		{
			if (a is Product<TResult> aP)
			{
				int count = 0;
				IConstant<TResult>? v = default;
				foreach (var c in aP.Children.OfType<IConstant<TResult>>())
				{
					v = c;
					if (++count != 1) break;
				}
				if (count == 1)
				{
					value = v!;
					return true;
				}
			}
			value = default!;
			return false;
		}

		public override int Compare(IEvaluate<TResult> a, IEvaluate<TResult> b)
		{
			var aFound = IsProductWithSingleConstant(a, out var aConstant);
			var bFound = IsProductWithSingleConstant(b, out var bConstant);
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
		static readonly Regex HasNegativeMultiple
			= new(@"^\(-(\d+)(\s*[\*\/]\s*)(.+)\)$|^-(\d+)$", RegexOptions.Compiled);

		protected override void ToStringInternal_OnAppendNextChild(StringBuilder result, int index, object child)
		{
			Debug.Assert(result != null);
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
			Debug.Assert(catalog != null);
			var zero = GetConstant(catalog, (TResult)(dynamic)0);

			// Phase 1: Flatten sums of sums.
			var children = catalog.Flatten<Sum<TResult>>(Children.Select(a =>
			{
				// Check for products that can be flattened as well.
				if (a is not Product<TResult> aP || aP.Children.Length != 2) return a;

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
			if (catalog is null) throw new ArgumentNullException(nameof(catalog));
			if (param is null) throw new ArgumentNullException(nameof(param));
			Contract.EndContractBlock();

			// ReSharper disable once SuspiciousTypeConversion.Global
			if (catalog is ICatalog<IEvaluate<double>> dCat && param is IEnumerable<IEvaluate<double>> p)
				return (dynamic)Sum.Create(dCat, p);

			return catalog.Register(new Sum<TResult>(param));
		}

		public virtual IEvaluate<TResult> NewUsing(
			ICatalog<IEvaluate<TResult>> catalog,
			IEnumerable<IEvaluate<TResult>> param)
		{
			if (param is null) throw new ArgumentNullException(nameof(param));
			var p = param as IEvaluate<TResult>[] ?? param.ToArray();
			return p.Length == 1 ? p[0] : Create(catalog, p);
		}

		public bool TryExtractGreatestFactor(
			ICatalog<IEvaluate<TResult>> catalog,
			[NotNull] out IEvaluate<TResult> sum,
			[NotNull] out IConstant<TResult> greatestFactor)
		{
			if (catalog is null) throw new ArgumentNullException(nameof(catalog));
			Contract.EndContractBlock();

			var one = GetConstant(catalog, (TResult)(dynamic)1);
			greatestFactor = one;
			sum = this;
			// Phase 5: Try and group by GCF:
			using var products = ListPool<Product<TResult>>.Shared.Rent();
			foreach (var c in Children)
			{
				// All of them must be products for GCF to work.
				if (c is Product<TResult> p)
					products.Item.Add(p);
				else
					return false;
			}

			// Try and get all the constants, and if a product does not have one, then done.
			using var constantRH = ListPool<TResult>.Shared.Rent();
			var constants = constantRH.Item;
			foreach (var p in products.Item)
			{
				using var c = p.Children.OfType<IConstant<TResult>>().GetEnumerator();
				if(c.MoveNext()) // At least 1. Ok.
				{
					var e = c.Current;
					if(!c.MoveNext()) // More than 1? Abort.
					{
						constants.Add(e.Value);
						continue;
					}
				}				

				return false;
			}

			// Convert all the constants to factors, and if any are invalid for factoring, then done.
			using var factors = ListPool<ulong>.Shared.Rent();
			foreach (var v in constants)
			{
				var d = Math.Abs(Convert.ToDecimal(v, CultureInfo.InvariantCulture));
				if (d <= decimal.One || decimal.Floor(d) != d) return false;
				factors.Item.Add(Convert.ToUInt64(d));
			}
			var gcf = Prime.GreatestFactor(factors.Item);
			Debug.Assert(factors.Item.All(f => f >= gcf));
			factors.Dispose();
			if (gcf <= 1) return false;

			TResult gcfT = (dynamic)gcf;

			greatestFactor = GetConstant(catalog, gcfT);
			sum = catalog.SumOf(catalog.MultiplesExtracted(products.Item)
				.Select(e =>
				{
					var m = e.Multiple ?? one;
					if (m != one && tryGetReducedFactor(m.Value, out var f))
						return catalog.ProductOf(f, e.Entry);

					return e.Entry;
				}));

			return true;

			bool tryGetReducedFactor(TResult value, out TResult f)
			{
				var r = (dynamic)value / gcf;
				f = r;
				return r != 1;
			}
		}

	}

	public static partial class SumExtensions
	{
		public static IEvaluate<TResult> SumOf<TResult>(
			this ICatalog<IEvaluate<TResult>> catalog,
			IEnumerable<IEvaluate<TResult>> children)
			where TResult : struct, IComparable
		{
			if (catalog is null) throw new ArgumentNullException(nameof(catalog));
			if (children is null) throw new ArgumentNullException(nameof(children));
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

					ListPool<IConstant<TResult>>.Shared.Give(constants);

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
			=> SumOf(catalog, (IEnumerable<IEvaluate<TResult>>)children);
	}

}
