/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Disposable;
using Open.Evaluation.Core;
using Open.Numeric.Primes;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;
using Throw;

namespace Open.Evaluation.Arithmetic;

public partial class Sum<TResult> :
	ArithmeticOperatorBase<TResult>
	where TResult : notnull, INumber<TResult>
{
	protected Sum(IEnumerable<IEvaluate<TResult>> children)
		: base(Symbols.Sum, children, true) { }

	protected override EvaluationResult<TResult> EvaluateInternal(Context context)
	{
		Children.Length.Throw("Cannot resolve sum of empty set.").IfEquals(0);

		var descs = new List<Lazy<string>>();
		var result = TResult.AdditiveIdentity;
		foreach (var r in ChildResults(context))
		{
			result += r.Result;
			descs.Add(r.Description);
		}

		return new(result, Describe(descs));
	}

	static bool IsProductWithSingleConstant(
		IEvaluate<TResult> a,
		[NotNullWhen(true)] out IConstant<TResult> value)
	{
		if (a is Product<TResult> aP)
		{
			var count = 0;
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

	public override int Compare(IEvaluate<TResult>? x, IEvaluate<TResult>? y)
	{
		if (x is null) return y is null ? 0 : -1;
		if (y is null) return +1;

		var aFound = IsProductWithSingleConstant(x, out var aConstant);
		var bFound = IsProductWithSingleConstant(y, out var bConstant);
		if (aFound && bFound)
		{
			var result = base.Compare(aConstant, bConstant);
			if (result != 0) return result;
		}
		else if (aFound)
		{
			if (TResult.Zero > aConstant.Value)
				return +1;
		}
		else if (bFound)
		{
			if (TResult.Zero > bConstant.Value)
				return -1;
		}

		return base.Compare(x, y);
	}

	[GeneratedRegex("^\\(-(\\d+)(\\s*[*/]\\s*)(.+)\\)$|^-(\\d+)$", RegexOptions.Compiled)]
	private static partial Regex HasNegativeMultiplePattern();
	private static readonly Regex HasNegativeMultiple = HasNegativeMultiplePattern();

	protected override void ToStringInternal_OnAppendNextChild(StringBuilder result, int index, object child)
	{
		Debug.Assert(result is not null);
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

	protected override IEvaluate<TResult> Reduction(
		ICatalog<IEvaluate<TResult>> catalog)
	{
		catalog.ThrowIfNull().OnlyInDebug();
		var zero = catalog.GetConstant(TResult.Zero);

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
		})).Where(c => c != zero).ToList(); // ** children's reduction is done here.

		// Phase 2: Can we collapse?
		switch (children.Count)
		{
			case 0:
				return catalog.GetConstant(TResult.Zero);
			case 1:
				return children[0];
		}

		if (typeof(TResult) == typeof(float) && children.Any(c => c is IConstant<float> d && float.IsNaN(d.Value)))
			return catalog.GetConstant(Constant<TResult>.FloatNaN.Value);

		if (typeof(TResult) == typeof(double) && children.Any(c => c is IConstant<double> d && double.IsNaN(d.Value)))
			return catalog.GetConstant(Constant<TResult>.DoubleNaN.Value);

		var one = catalog.GetConstant(TResult.One);

		// Phase 3: Look for groupings by "multiples".
		var withMultiples = catalog.MultiplesExtracted(children, true).ToArray();

		// Phase 4: Replace multipliable products with single merged version.
		return catalog.SumOf(
			withMultiples
				.GroupBy(g => g.Hash)
				.OrderBy(g => g.Key) // Ensure consistency.
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
		catalog.ThrowIfNull();
		param.ThrowIfNull();
		Contract.EndContractBlock();

		// ReSharper disable once SuspiciousTypeConversion.Global
		return catalog is ICatalog<IEvaluate<double>> dCat && param is IEnumerable<IEvaluate<double>> p
			? (Sum<TResult>)(dynamic)Sum.Create(dCat, p)
			: catalog.Register(new Sum<TResult>(param));
	}

	internal virtual IEvaluate<TResult> NewUsing(
		ICatalog<IEvaluate<TResult>> catalog,
		IReadOnlyList<IEvaluate<TResult>> param)
		=> param.Count == 1 ? param[0] : Create(catalog, param);

	public override IEvaluate<TResult> NewUsing(
		ICatalog<IEvaluate<TResult>> catalog,
		IEnumerable<IEvaluate<TResult>> param)
		=> param is IReadOnlyList<IEvaluate<TResult>> p
		? NewUsing(catalog, p)
		: ConditionalTransform(param, p => Create(catalog, p));

	public bool TryExtractGreatestFactor(
		ICatalog<IEvaluate<TResult>> catalog,
		[NotNullWhen(true)] out IEvaluate<TResult> sum,
		[NotNullWhen(true)] out IConstant<TResult> greatestFactor)
	{
		catalog.ThrowIfNull();
		Contract.EndContractBlock();

		var one = catalog.GetConstant(TResult.One);
		greatestFactor = one;
		sum = this;
		// Phase 5: Try and group by GCF:
		using var products = ListPool<Product<TResult>>.Rent();
		foreach (var c in Children)
		{
			// All of them must be products for GCF to work.
			if (c is Product<TResult> p)
				products.Item.Add(p);
			else
				return false;
		}

		// Try and get all the constants, and if a product does not have one, then done.
		using var constantRH = ListPool<TResult>.Rent();
		var constants = constantRH.Item;
		foreach (var p in products.Item)
		{
			using var c = p.Children.OfType<IConstant<TResult>>().GetEnumerator();
			if (c.MoveNext()) // At least 1. OK.
			{
				var e = c.Current;
				if (!c.MoveNext()) // More than 1? Abort.
				{
					constants.Add(e.Value);
					continue;
				}
			}

			return false;
		}

		// Convert all the constants to factors, and if any are invalid for factoring, then done.
		using var factors = ListPool<ulong>.Rent();
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

		greatestFactor = catalog.GetConstant(gcfT);
		sum = catalog.SumOf(catalog.MultiplesExtracted(products.Item)
			.Select(e =>
			{
				var m = e.Multiple ?? one;
				return m != one && tryGetReducedFactor(m.Value, out var f)
					? catalog.ProductOf(in f, e.Entry)
					: e.Entry;
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
