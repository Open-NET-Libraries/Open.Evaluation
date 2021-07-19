/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
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
	public class Product<TResult> :
		OperatorBase<TResult>,
		IReproducable<IEnumerable<IEvaluate<TResult>>, IEvaluate<TResult>>
		where TResult : struct, IComparable
	{
		protected Product(IEnumerable<IEvaluate<TResult>> children)
			: base(Product.SYMBOL, Product.SEPARATOR, children, true, 2)
		{ }

		protected Product(IEvaluate<TResult> first, params IEvaluate<TResult>[] rest)
			: this(Enumerable.Repeat(first, 1).Concat(rest))
		{ }

		protected override int ConstantPriority => -1;

		protected override TResult EvaluateInternal(object context)
		{
			if (Children.Length == 0)
				throw new InvalidOperationException("Cannot resolve product of empty set.");

			return ChildResults(context)
				.Cast<TResult>()
				.Aggregate<TResult, dynamic>(1, (current, r) => current * r);
		}

		protected override IEvaluate<TResult> Reduction(
			ICatalog<IEvaluate<TResult>> catalog)
		{
			Debug.Assert(catalog != null);
			var one = catalog.GetConstant((TResult)(dynamic)1);

			// Phase 1: Flatten products of products.
			var children = catalog
				.Flatten<Product<TResult>>(Children)
				.Where(c => c != one)
				.ToList(); // ** chidren's reduction is done here.

			// Phase 3: Try to extract common multiples...
			var len = children.Count;
			for (var i = 0; i < len; i++)
			{
				var child = children[i];
				if (child is not Sum<TResult> sum ||
					!sum.TryExtractGreatestFactor(catalog, out var newSum, out var gcf))
					continue;

				children[i] = newSum; // Replace with extraction...
				children.Add(gcf); // Append the constant..
			}

			// Phase 3: Can we collapse?
			switch (children.Count)
			{
				case 0:
					if (Children.Length == 0)
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
				? (Base: cat.GetReduced(e.Base), e.Power)
				: (c, one)).ToArray();

			if (exponents.Any(c => c.Base == zero))
				return zero;

			children = exponents
				.Where(e => e.Power != zero)
				.GroupBy(e => e.Base.ToStringRepresentation())
				.Select(g =>
				{
					var @base = g.First().Base;
					var sumPower = cat.SumOf(g.Select(t => t.Power)!);
					var power = cat.GetReduced(sumPower);
					if (power == zero) return one;
					return (IEvaluate<TResult>)(power == one
						? @base
						: GetExponent(catalog, @base, power));

				}).ToList();

			var multiple = children.OfType<IConstant<TResult>>().FirstOrDefault();
			// ReSharper disable once InvertIf
			if (multiple != null)
			{
				var multipleValue = Convert.ToDouble(multiple.Value, CultureInfo.InvariantCulture);
				// ReSharper disable once InvertIf
				if (multipleValue != 1 && Math.Floor(multipleValue) == multipleValue)
				{
					var oneNeg = catalog.GetConstant((TResult)(dynamic)(-1));
					var muValue = (long)multipleValue;
					var originalMultiple = muValue;
					var multipleIndex = children.IndexOf(multiple);
					var count = children.Count;
					for (var i = 0; i < count; i++)
					{
						// Let's start with simple division of constants.
						if (children[i] is not Exponent<TResult> ex
							|| ex.Power != oneNeg
							|| ex.Base is not IConstant<TResult> expC)
							continue;

						var divisor = Convert.ToDouble(expC.Value, CultureInfo.InvariantCulture);
						// ReSharper disable once CompareOfFloatsByEqualityOperator

						// We won't mess with factional divisors yet.
						if (Math.Floor(divisor) != divisor)
							continue;

						var d = (long)divisor;
						if (muValue % d == 0)
						{
							muValue /= d;
							children[i] = one;
						}
						else
						{
							var f = 1L;
							// We might have a potential divisor...
							foreach (var factor in Prime.Factors(d, true))
							{
								if (factor > muValue) break;
								if (muValue % factor != 0) continue;
								Debug.Assert(factor != 0);
								muValue /= factor;
								f *= factor;
							}

							if (f != 1L)
							{
								children[i] = catalog.GetExponent(
									catalog.GetConstant((TResult)(dynamic)(d / f)),
									catalog.GetConstant((TResult)(dynamic)(-1)));
							}
						}

						if (muValue == 1)
							break;
					}

					if (muValue != originalMultiple)
					{
						children[multipleIndex] = catalog.GetConstant((TResult)(dynamic)(muValue));
					}
				}
			}

			return children.Count == 1
					? children[0]
					: catalog.ProductOf(children);

		}

		// ReSharper disable once StaticMemberInGenericType
		static readonly Regex IsInverted = new(@"^\(1/(.+)\)$", RegexOptions.Compiled);

		protected override void ToStringInternal_OnAppendNextChild(StringBuilder result, int index, object child)
		{
			Debug.Assert(result != null);
			if (index != 0 && child is string c)
			{
				var m = IsInverted.Match(c);
				if (m.Success)
				{
					result.Append(" / ");
					result.Append(m.Groups[1].Value);
					return;
				}
			}

			base.ToStringInternal_OnAppendNextChild(result, index, child);
		}

		protected virtual Exponent<TResult> GetExponent(ICatalog<IEvaluate<TResult>> catalog,
			IEvaluate<TResult> baseValue,
			IEvaluate<TResult> power)
			=> Exponent<TResult>.Create(catalog, baseValue, power);

		public IEvaluate<TResult> ExtractMultiple(ICatalog<IEvaluate<TResult>> catalog, out IConstant<TResult>? multiple)
		{
			multiple = null;

			if (!Children.OfType<IConstant<TResult>>().Any()) return this;

			var children = Children.ToList(); // Make a copy to be worked on...
			var constants = children.ExtractType<IConstant<TResult>>();
			if (constants.Count == 0) return this;

			multiple = catalog.ProductOfConstants(constants);
			return NewUsing(catalog, children);
		}

		public IEvaluate<TResult> ReductionWithMutlipleExtracted(ICatalog<IEvaluate<TResult>> catalog, out IConstant<TResult>? multiple)
		{
			multiple = null;
			Debug.Assert(catalog != null);
			var reduced = catalog.GetReduced(this);
			return reduced is Product<TResult> product
				? product.ExtractMultiple(catalog, out multiple)
				: reduced;
		}

		static bool IsExponentWithConstantPower(
			IEvaluate<TResult> a,
			[NotNullWhen(true)] out IConstant<TResult> value)
		{
			if (a is Exponent<TResult> aP && aP.Power is IConstant<TResult> c)
			{
				value = c;
				return true;
			}

			value = default!;
			return false;
		}

		public override int Compare(IEvaluate<TResult> a, IEvaluate<TResult> b)
		{
			// Constants always get priority in products and are moved to the front.  They should collapse in reduction to a 'multiple'.
			if (a is IConstant<TResult> || b is IConstant<TResult>)
				return base.Compare(a, b);

			var aFound = IsExponentWithConstantPower(a, out var aConstant);
			var bFound = IsExponentWithConstantPower(b, out var bConstant);
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

		internal static Product<TResult> Create(
			ICatalog<IEvaluate<TResult>> catalog,
			IEnumerable<IEvaluate<TResult>> param)
		{
			if (catalog is null) throw new ArgumentNullException(nameof(catalog));
			if (param is null) throw new ArgumentNullException(nameof(param));
			Contract.EndContractBlock();

			// ReSharper disable once SuspiciousTypeConversion.Global
			if (catalog is ICatalog<IEvaluate<double>> dCat && param is IEnumerable<IEvaluate<double>> p)
				return (dynamic)Product.Create(dCat, p);

			return catalog.Register(new Product<TResult>(param));
		}

		public virtual IEvaluate<TResult> NewUsing(
			ICatalog<IEvaluate<TResult>> catalog,
			IReadOnlyList<IEvaluate<TResult>> param)
		{
			if (param is null) throw new ArgumentNullException(nameof(param));
			Contract.EndContractBlock();

			return param.Count == 1 ? param[0] : Create(catalog, param);
		}

		public virtual IEvaluate<TResult> NewUsing(
			ICatalog<IEvaluate<TResult>> catalog,
			IEnumerable<IEvaluate<TResult>> param)
			=> param is IReadOnlyList<IEvaluate<TResult>> p
			? NewUsing(catalog, p)
			: ConditionalTransform(param, p => Create(catalog, p));
	}

	public static partial class ProductExtensions
	{
		public static IEnumerable<(string Hash, IConstant<TResult>? Multiple, IEvaluate<TResult> Entry)> MultiplesExtracted<TResult>(
			this ICatalog<IEvaluate<TResult>> catalog,
			IEnumerable<IEvaluate<TResult>> source, bool reduce = false)
			where TResult : struct, IComparable
			=> source.Select(c =>
			{
				if (c is not Product<TResult> p)
					return (c.ToStringRepresentation(), default(IConstant<TResult>?), c);

				var reduced = reduce
					? p.ReductionWithMutlipleExtracted(catalog, out var multiple)
					: p.ExtractMultiple(catalog, out multiple);

				return (
					reduced.ToStringRepresentation(),
					multiple,
					reduced
				);
			});

		public static IEvaluate<TResult> ProductOf<TResult>(
			this ICatalog<IEvaluate<TResult>> catalog,
			IEnumerable<IEvaluate<TResult>> children)
			where TResult : struct, IComparable
		{
			if (catalog is null) throw new ArgumentNullException(nameof(catalog));
			if (children is IReadOnlyCollection<IEvaluate<TResult>> ch && ch.Count == 0)
				throw new InvalidOperationException("Cannot produce a product of an empty set.");

			using var childListRH = ListPool<IEvaluate<TResult>>.Shared.Rent();
			var childList = childListRH.Item;
			childList.AddRange(children);
			if (childList.Count == 0)
				throw new InvalidOperationException("Cannot produce a product of an empty set.");
			var constants = childList.ExtractType<IConstant<TResult>>();

			if (constants.Count > 0)
			{
				var c = constants.Count == 1
					? constants[0]
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

			switch (childList.Count)
			{
				case 0:
					//Debug.Fail("Extraction failure.", "Should not have occured.");
					throw new Exception("Extraction failure.");
				case 1:
					return childList[0];
				default:
					break;
			}

			return Product<TResult>.Create(catalog, childList);
		}

		public static IEvaluate<TResult> ProductOf<TResult>(
			this ICatalog<IEvaluate<TResult>> catalog,
			IEvaluate<TResult> multiple,
			IEnumerable<IEvaluate<TResult>> children)
			where TResult : struct, IComparable
			=> ProductOf(catalog, children.Append(multiple));

		public static IEvaluate<TResult> ProductOfSum<TResult>(
			this ICatalog<IEvaluate<TResult>> catalog,
			IEvaluate<TResult> multiple,
			Sum<TResult> sum)
			where TResult : struct, IComparable
			=> multiple is Sum<TResult> m
			? ProductOfSums(catalog, m, sum)
			: catalog.GetReduced(catalog.SumOf(sum.Children.Select(c => ProductOf(catalog, multiple, c))));

		public static IEvaluate<TResult> ProductOfSums<TResult>(
			this ICatalog<IEvaluate<TResult>> catalog,
			Sum<TResult> a,
			Sum<TResult> b)
			where TResult : struct, IComparable
			=> catalog.GetReduced(catalog.SumOf(a.Children.Select(c => ProductOfSum(catalog, c, b))));

		public static IEvaluate<TResult> ProductOfSums<TResult>(
			this ICatalog<IEvaluate<TResult>> catalog,
			IReadOnlyCollection<Sum<TResult>> sums)
			where TResult : struct, IComparable
		{
			if(sums.Count==0) return catalog.GetConstant((TResult)(dynamic)1);
			using var e = sums.GetEnumerator();
			if (!e.MoveNext()) throw new Exception("Collection empty with count>0.");
			IEvaluate<TResult> p = e.Current;
			while (e.MoveNext()) p = ProductOfSum(catalog, p, e.Current);
			return catalog.GetReduced(p);
		}

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
			=> ProductOf(catalog, catalog.GetConstant(multiple), (IEnumerable<IEvaluate<TResult>>)rest);

	}
}
