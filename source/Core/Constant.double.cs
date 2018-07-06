﻿/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using System.Collections.Generic;
using System.Linq;

namespace Open.Evaluation.Core
{
	public sealed class Constant : Constant<double>
	{
		Constant(double value) : base(value)
		{ }

		internal new static Constant Create(ICatalog<IEvaluate<double>> catalog, in double value)
		{
			var v = value;
			return catalog.Register(value.ToString(), k => new Constant(v));
		}

		public override IEvaluate NewUsing(ICatalog<IEvaluate> catalog, in double value)
		{
			var v = value;
			return catalog.Register(value.ToString(), k => new Constant(v));
		}

	}

	public static partial class ConstantExtensions
	{
		public static Constant GetConstant(
			this ICatalog<IEvaluate<double>> catalog,
			in double value)
		{
			return Constant.Create(catalog, value);
		}

		public static Constant SumOfConstants(
			this ICatalog<IEvaluate<double>> catalog,
			IEnumerable<IConstant<double>> constants)
		{
			return GetConstant(catalog, constants.Sum(s => s.Value));
		}

		public static Constant SumOfConstants(
			this ICatalog<IEvaluate<double>> catalog,
			in double c1, params IConstant<double>[] rest)
		{
			return GetConstant(catalog, c1 + rest.Sum(s => s.Value));
		}

		public static Constant SumOfConstants(
			this ICatalog<IEvaluate<double>> catalog,
			in IConstant<double> c1, params IConstant<double>[] rest)
		{
			return SumOfConstants(catalog, rest.Concat(c1));
		}

		public static Constant ProductOfConstants(
			this ICatalog<IEvaluate<double>> catalog,
			IEnumerable<IConstant<double>> constants)
		{
			return ProductOfConstants(catalog, 1, constants);
		}

		public static Constant ProductOfConstants(
			this ICatalog<IEvaluate<double>> catalog,
			in IConstant<double> c1, params IConstant<double>[] rest)
		{
			return ProductOfConstants(catalog, c1.Value, rest);
		}

		public static Constant ProductOfConstants(
			this ICatalog<IEvaluate<double>> catalog,
			in double c1, IEnumerable<IConstant<double>> others)
		{
			double result = c1;
			foreach (var c in others)
			{
				result *= c.Value;
			}
			return GetConstant(catalog, in result);
		}

		public static Constant ProductOfConstants(
			this ICatalog<IEvaluate<double>> catalog,
			in double c1, params IConstant<double>[] rest)
		{
			return ProductOfConstants(catalog, in c1, (IEnumerable<IConstant<double>>)rest);
		}

	}

}
