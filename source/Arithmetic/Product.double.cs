/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Disposable;
using Open.Evaluation.Core;

namespace Open.Evaluation.Arithmetic;

public class Product : Product<double>
{
	public const char SYMBOL = '*';
	public const string SEPARATOR = " * ";

	internal Product(IEnumerable<IEvaluate<double>> children)
		: base(children)
	{
	}

	internal Product(IEvaluate<double> first, params IEvaluate<double>[] rest)
		: this(Enumerable.Repeat(first, 1).Concat(rest))
	{ }

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1725:Parameter names should match base declaration")]
	protected override Exponent<double> GetExponent(ICatalog<IEvaluate<double>> catalog,
		IEvaluate<double> @base,
		IEvaluate<double> power)
		=> Exponent.Create(catalog, @base, power);

	internal new static Product Create(
		ICatalog<IEvaluate<double>> catalog,
		IEnumerable<IEvaluate<double>> param)
		=> catalog.Register(new Product(param));

	//internal static Product<TResult> Create<TResult>(
	//	ICatalog<IEvaluate<TResult>> catalog,
	//	IEnumerable<IEvaluate<TResult>> param)
	//	where TResult : notnull, IComparable<TResult>, IComparable
	//	=> Product<TResult>.Create(catalog, param);

	public override IEvaluate<double> NewUsing(
		ICatalog<IEvaluate<double>> catalog,
		IEnumerable<IEvaluate<double>> param)
	{
		catalog.ThrowIfNull();
		param.ThrowIfNull();
		Contract.EndContractBlock();

		var p = param as IEvaluate<double>[] ?? param.ToArray();
		return p.Length == 1 ? p[0] : Create(catalog, p);
	}
}

public static partial class ProductExtensions
{
	public static IEvaluate<double> ProductOf(
		this ICatalog<IEvaluate<double>> catalog,
		IEnumerable<IEvaluate<double>> children)
	{
		catalog.ThrowIfNull();
		if (children is IReadOnlyCollection<IEvaluate<double>> ch && ch.Count == 0)
			throw new NotSupportedException("Cannot produce a product of an empty set.");

		using var childListRH = ListPool<IEvaluate<double>>.Rent();
		var childList = childListRH.Item;
		childList.AddRange(children);
		if (childList.Count == 0)
			throw new NotSupportedException("Cannot produce a product of an empty set.");
		var constants = childList.ExtractType<IConstant<double>>();

		if (constants.Count > 0)
		{
			var c = constants.Count == 1
				? constants[0] :
				catalog.ProductOfConstants(constants);

			if (childList.Count == 0)
				return c;

			// No need to multiply by 1.
			if (c != catalog.GetConstant(1))
				childList.Add(c);
		}

		switch (childList.Count)
		{
			case 0:
				Debug.Fail("Extraction failure.", "Should not have occured.");
				throw new InvalidOperationException("Extraction failure.");
			case 1:
				return childList[0];
			default:
				break;
		}

		return Product.Create(catalog, childList);
	}

	public static IEvaluate<double> ProductOf(
		this ICatalog<IEvaluate<double>> catalog,
		IEvaluate<double> multiple,
		IEnumerable<IEvaluate<double>> children)
		=> ProductOf(catalog, children.Append(multiple));

	public static IEvaluate<double> ProductOf(
		this ICatalog<IEvaluate<double>> catalog,
		IEvaluate<double> multiple,
		IEvaluate<double> first,
		params IEvaluate<double>[] rest)
		=> ProductOf(catalog, rest.Prepend(first).Append(multiple));

	public static IEvaluate<double> ProductOf(
		this ICatalog<IEvaluate<double>> catalog,
		in double multiple,
		IEnumerable<IEvaluate<double>> children)
		=> ProductOf(catalog, catalog.GetConstant(multiple), children);

	public static IEvaluate<double> ProductOf(
		this ICatalog<IEvaluate<double>> catalog,
		in double multiple,
		IEvaluate<double> first,
		params IEvaluate<double>[] rest)
		=> ProductOf(catalog, catalog.GetConstant(multiple), rest.Prepend(first));
}
