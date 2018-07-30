/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Evaluation.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Open.Evaluation.Arithmetic
{
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

		protected override Exponent<double> GetExponent(ICatalog<IEvaluate<double>> catalog,
			IEvaluate<double> @base,
			IEvaluate<double> power)
			=> Exponent.Create(catalog, @base, power);

		internal new static Product Create(
			ICatalog<IEvaluate<double>> catalog,
			IEnumerable<IEvaluate<double>> param)
		{
			Debug.Assert(catalog != null);
			Debug.Assert(param != null);
			return catalog.Register(new Product(param));
		}

		public override IEvaluate<double> NewUsing(
			ICatalog<IEvaluate<double>> catalog,
			IEnumerable<IEvaluate<double>> param)
		{
			Debug.Assert(param != null);
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
			var childList = children.ToList();
			if (childList.Count == 0)
				throw new InvalidOperationException("Cannot produce a product of an empty set.");

			var constants = childList.ExtractType<IConstant<double>>();
			if (constants.Count > 0)
			{
				var c = constants.Count == 1
					? constants.Single() :
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
					//Debug.Fail("Extraction failure.", "Should not have occured.");
					throw new Exception("Extraction failure.");
				case 1:
					return childList[0];
			}

			return Product.Create(catalog, childList);
		}

		public static IEvaluate<double> ProductOf(
			this ICatalog<IEvaluate<double>> catalog,
			IEvaluate<double> multiple,
			IEnumerable<IEvaluate<double>> children)
			=> ProductOf(catalog, children.Concat(multiple));

		public static IEvaluate<double> ProductOf(
			this ICatalog<IEvaluate<double>> catalog,
			IEvaluate<double> multiple,
			params IEvaluate<double>[] rest)
			=> ProductOf(catalog, rest.Concat(multiple));

		public static IEvaluate<double> ProductOf(
			this ICatalog<IEvaluate<double>> catalog,
			in double multiple,
			IEnumerable<IEvaluate<double>> children)
			=> ProductOf(catalog, catalog.GetConstant(multiple), children);

		public static IEvaluate<double> ProductOf(
			this ICatalog<IEvaluate<double>> catalog,
			in double multiple,
			params IEvaluate<double>[] rest)
			=> ProductOf(catalog, catalog.GetConstant(multiple), rest);
	}
}
