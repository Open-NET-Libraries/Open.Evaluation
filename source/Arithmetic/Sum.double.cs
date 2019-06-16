/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Evaluation.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Open.Evaluation.Arithmetic
{
	public class Sum : Sum<double>
	{
		public const char SYMBOL = '+';
		public const string SEPARATOR = " + ";

		internal Sum(IEnumerable<IEvaluate<double>> children = null)
			: base(children)
		{ }

		protected override double EvaluateInternal(object context)
		{
			if (ChildrenInternal.Count == 0)
				throw new InvalidOperationException("Cannot resolve sum of empty set.");

			return ChildResults(context).Cast<double>().Sum();
		}

		protected virtual Constant<double> GetConstant(ICatalog<IEvaluate<double>> catalog, double value)
			=> Constant.Create(catalog, value);

		internal new static Sum Create(
			ICatalog<IEvaluate<double>> catalog,
			IEnumerable<IEvaluate<double>> param)
		{
			Debug.Assert(catalog != null);
			Debug.Assert(param != null);
			return catalog.Register(new Sum(param));
		}

		public override IEvaluate<double> NewUsing(
			ICatalog<IEvaluate<double>> catalog,
			IEnumerable<IEvaluate<double>> param)
		{
			var p = param as IEvaluate<double>[] ?? param.ToArray();
			return p.Length == 1 ? p[0] : Create(catalog, p);
		}
	}

	public static partial class SumExtensions
	{
		public static IEvaluate<double> SumOf(
			this ICatalog<IEvaluate<double>> catalog,
			IEnumerable<IEvaluate<double>> children)
		{
			if (catalog == null) throw new ArgumentNullException(nameof(catalog));
			if (children == null) throw new ArgumentNullException(nameof(children));
			Contract.EndContractBlock();

			var childList = children.ToList();
			switch (childList.Count)
			{
				case 0:
					return catalog.GetConstant(0);
				case 1:
					return childList.Single();
				default:
					var constants = childList.ExtractType<IConstant<double>>();

					if (constants.Count == 0)
						return Sum.Create(catalog, childList);

					var c = constants.Count == 1
						? constants[0]
						: catalog.SumOfConstants(constants);

					if (childList.Count == 0)
						return c;

					childList.Add(c);

					return Sum.Create(catalog, childList);
			}
		}

		public static IEvaluate<double> SumOf(
			this ICatalog<IEvaluate<double>> catalog,
			params IEvaluate<double>[] children)
			=> SumOf(catalog, (IEnumerable<IEvaluate<double>>)children);


	}

}
