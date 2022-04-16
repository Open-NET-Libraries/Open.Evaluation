/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

using System;
using System.Collections.Generic;
using System.Linq;

namespace Open.Evaluation.Core;

public sealed class Constant : Constant<double>
{
	Constant(double value) : base(value)
	{ }

	internal new static Constant Create(ICatalog<IEvaluate<double>> catalog, double value)
		=> catalog.Register(ToStringRepresentation(in value), _ => new Constant(value));

	public override IEvaluate<double> NewUsing(ICatalog<IEvaluate<double>> catalog, double value)
		=> catalog.Register(ToStringRepresentation(in value), _ => new Constant(value));
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
	{
		if (c2 is null) throw new ArgumentNullException(nameof(c2));
		return GetConstant(catalog, c1 + c2.Value + rest.Sum(s => s.Value));
	}

	public static Constant SumOfConstants(
		this ICatalog<IEvaluate<double>> catalog,
		in IConstant<double> c1, IConstant<double> c2, params IConstant<double>[] rest)
	{
		if (c1 is null) throw new ArgumentNullException(nameof(c1));
		return SumOfConstants(catalog, c1.Value, c2, rest);
	}

	public static Constant ProductOfConstants(
		this ICatalog<IEvaluate<double>> catalog,
		IEnumerable<IConstant<double>> constants)
		=> ProductOfConstants(catalog, 1, constants);

	public static Constant ProductOfConstants(
		this ICatalog<IEvaluate<double>> catalog,
		in IConstant<double> c1, in IConstant<double> c2, params IConstant<double>[] rest)
	{
		if (c1 is null) throw new ArgumentNullException(nameof(c1));
		if (c2 is null) throw new ArgumentNullException(nameof(c2));

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
