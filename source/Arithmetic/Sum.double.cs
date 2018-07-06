/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Evaluation.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Open.Evaluation.Arithmetic
{
	public class Sum : Sum<double>
	{
		public const char SYMBOL = '+';
		public const string SEPARATOR = " + ";

		internal Sum(in IEnumerable<IEvaluate<double>> children = null)
			: base(in children)
		{ }

		protected override double EvaluateInternal(in object context)
		{
			if (ChildrenInternal.Count == 0)
				throw new InvalidOperationException("Cannot resolve sum of empty set.");

			return ChildResults(context).Cast<double>().Sum();
		}

		internal new static Sum Create(
			in ICatalog<IEvaluate<double>> catalog,
			in IEnumerable<IEvaluate<double>> param)
		{
			return catalog.Register(new Sum(in param));
		}

		public override IEvaluate NewUsing(
			in ICatalog<IEvaluate> catalog,
			in IEnumerable<IEvaluate<double>> param)
		{
			return catalog.Register(new Sum(in param));
		}
	}

	public static partial class SumExtensions
	{
		public static IEvaluate<double> SumOf(
			this ICatalog<IEvaluate<double>> catalog,
			in IEnumerable<IEvaluate<double>> children)
		{
			var childList = children.ToList();
			var constants = childList.ExtractType<IConstant<double>>();
			if (constants.Count > 0)
			{
				var c = constants.Count == 1 ? constants.Single() : catalog.SumOfConstants(constants);
				if (childList.Count == 0)
					return c;

				childList.Add(c);
			}
			else if (childList.Count == 0)
			{
				return catalog.GetConstant(0);
			}
			else if (childList.Count == 1)
			{
				return childList.Single();
			}

			return Sum.Create(catalog, childList);
		}

		public static IEvaluate<double> SumOf(
			this ICatalog<IEvaluate<double>> catalog,
			params IEvaluate<double>[] children)
		{
			return SumOf(catalog, (IEnumerable<IEvaluate<double>>)children);
		}


	}

}
