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

		internal Product(IEnumerable<IEvaluate<double>> children = null)
			: base(children)
		{
		}

		internal Product(IEvaluate<double> first, params IEvaluate<double>[] rest)
			: this(Enumerable.Repeat(first, 1).Concat(rest))
		{ }

		public override IEvaluate NewUsing(
			in ICatalog<IEvaluate> catalog,
			in IEnumerable<IEvaluate<double>> param)
		{
			return catalog.Register(new Product(param));
		}

		internal new static Product Create(
			in ICatalog<IEvaluate<double>> catalog,
			in IEnumerable<IEvaluate<double>> param)
		{
			return catalog.Register(new Product(param));
		}
	}

	public static partial class ProductExtensions
	{
		public static IEvaluate<double> ProductOf(
			this ICatalog<IEvaluate<double>> catalog,
			in IEnumerable<IEvaluate<double>> children)
		{
			var childList = children.ToList();
			if (childList.Count == 0)
				throw new InvalidOperationException("Cannot produce a product of an empty set.");

			var constants = childList.ExtractType<IConstant<double>>();
			if (constants.Count > 0)
			{
				var c = constants.Count == 1 ? constants.Single() : catalog.ProductOfConstants(constants);
				if (childList.Count == 0)
					return c;

				childList.Add(c);
			}
			else if (childList.Count == 0)
			{
				Debug.Fail("Extraction failure.", "Should not have occured.");
				throw new Exception("Extraction failure.");
			}
			else if (childList.Count == 1)
			{
				return childList.Single();
			}

			return Product.Create(catalog, childList);
		}

		public static IEvaluate<double> ProductOf(
			this ICatalog<IEvaluate<double>> catalog,
			in IEvaluate<double> multiple,
			in IEnumerable<IEvaluate<double>> children)
		{
			return ProductOf(catalog, children.Concat(multiple));
		}

		public static IEvaluate<double> ProductOf(
			this ICatalog<IEvaluate<double>> catalog,
			in IEvaluate<double> multiple,
			params IEvaluate<double>[] rest)
		{
			return ProductOf(catalog, rest.Concat(multiple));
		}

		public static IEvaluate<double> ProductOf(
			this ICatalog<IEvaluate<double>> catalog,
			in double multiple,
			in IEnumerable<IEvaluate<double>> children)
		{
			return ProductOf(catalog, catalog.GetConstant(in multiple), in children);
		}

		public static IEvaluate<double> ProductOf(
			this ICatalog<IEvaluate<double>> catalog,
			in double multiple,
			params IEvaluate<double>[] rest)
		{
			return ProductOf(catalog, catalog.GetConstant(in multiple), rest);
		}
	}
}