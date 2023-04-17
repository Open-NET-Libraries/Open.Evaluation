/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

namespace Open.Evaluation.Core;

public sealed class Constant : Constant<double>
{
	Constant(double value) : base(value)
	{ }

	internal new static Constant Create(ICatalog<IEvaluate<double>> catalog, double value)
		=> catalog.Register(ToStringRepresentation(in value), _ => new Constant(value));

	public override IEvaluate<double> NewUsing(ICatalog<IEvaluate<double>> catalog, double param)
		=> catalog.Register(ToStringRepresentation(in param), _ => new Constant(param));
}

public static partial class ConstantExtensions
{
	public static Constant GetConstant(
		this ICatalog<IEvaluate<double>> catalog,
		double value)
		=> Constant.Create(catalog, value);

	public static Constant SumOfConstants(
		this ICatalog<IEvaluate<double>> catalog,
		IEnumerable<IConstant<double>> constants)
		=> GetConstant(catalog, constants.Sum(s => s.Value));

	public static Constant SumOfConstants(
		this ICatalog<IEvaluate<double>> catalog,
		double c1, in IConstant<double> c2, params IConstant<double>[] rest)
		=> c2 is null ? throw new ArgumentNullException(nameof(c2))
		: GetConstant(catalog, c1 + c2.Value + rest.Sum(s => s.Value));

	public static Constant SumOfConstants(
		this ICatalog<IEvaluate<double>> catalog,
		in IConstant<double> c1, IConstant<double> c2, params IConstant<double>[] rest)
		=> c1 is null ? throw new ArgumentNullException(nameof(c1))
		: SumOfConstants(catalog, c1.Value, c2, rest);

	public static Constant ProductOfConstants(
		this ICatalog<IEvaluate<double>> catalog,
		IEnumerable<IConstant<double>> constants)
		=> ProductOfConstants(catalog, 1, constants);

	public static Constant ProductOfConstants(
		this ICatalog<IEvaluate<double>> catalog,
		in IConstant<double> c1, in IConstant<double> c2, params IConstant<double>[] rest)
	{
		c1.ThrowIfNull();
		c2.ThrowIfNull();
		Contract.EndContractBlock();

		return ProductOfConstants(catalog, c1.Value, rest);
	}

	public static Constant ProductOfConstants(
		this ICatalog<IEvaluate<double>> catalog,
		double c1, IEnumerable<IConstant<double>> others)
		=> GetConstant(catalog,
			others.Aggregate(c1, (current, c) => current * c.Value));

	public static Constant ProductOfConstants(
		this ICatalog<IEvaluate<double>> catalog,
		double c1, in IConstant<double> c2, params IConstant<double>[] rest)
		=> ProductOfConstants(catalog, c1, rest.Prepend(c2));
}
