/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Disposable;
using Open.Evaluation.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Open.Evaluation.Arithmetic
{
	public class Sum : Sum<double>
	{
		public const char SYMBOL = '+';
		public const string SEPARATOR = " + ";

		internal Sum(IEnumerable<IEvaluate<double>> children)
			: base(children)
		{ }

		protected override double EvaluateInternal(object context)
		{
			if (Children.Length == 0)
				throw new InvalidOperationException("Cannot resolve sum of empty set.");

			return ChildResults(context).Cast<double>().Sum();
		}

		protected virtual Constant<double> GetConstant(ICatalog<IEvaluate<double>> catalog, double value)
			=> Constant.Create(catalog, value);

		internal new static Sum Create(
			ICatalog<IEvaluate<double>> catalog,
			IEnumerable<IEvaluate<double>> param)
			=> catalog.Register(new Sum(param));

		internal static Sum<TResult> Create<TResult>(
			ICatalog<IEvaluate<TResult>> catalog,
			IEnumerable<IEvaluate<TResult>> param)
			where TResult : struct, IComparable
			=> Sum<TResult>.Create(catalog, param);

		public override IEvaluate<double> NewUsing(
			ICatalog<IEvaluate<double>> catalog,
			IEnumerable<IEvaluate<double>> param)
		{
			if (catalog is null) throw new ArgumentNullException(nameof(catalog));
			if (param is null) throw new ArgumentNullException(nameof(param));
			Contract.EndContractBlock();

			var p = param as IEvaluate<double>[] ?? param.ToArray();
			return p.Length == 1 ? p[0] : Create(catalog, p);
		}
	}

	public static partial class SumExtensions
	{

		static IEvaluate<double> SumOfCollection<TResult>(
			ICatalog<IEvaluate<double>> catalog,
			List<IEvaluate<double>> childList)
			where TResult : struct, IComparable
		{
			var constants = childList.ExtractType<IConstant<double>>();

			if (constants.Count == 0)
				return Sum.Create(catalog, childList);

			var c = constants.Count == 1
				? constants[0]
				: catalog.SumOfConstants(constants);

			ListPool<IConstant<double>>.Shared.Give(constants);

			if (childList.Count == 0)
				return c;

			childList.Add(c);

			return Sum.Create(catalog, childList);
		}

		public static IEvaluate<double> SumOf(
			this ICatalog<IEvaluate<double>> catalog,
			IReadOnlyList<IEvaluate<double>> children)
		{
			if (catalog is null) throw new ArgumentNullException(nameof(catalog));
			if (children is null) throw new ArgumentNullException(nameof(children));
			Contract.EndContractBlock();

			switch (children.Count)
			{
				case 0:
					return catalog.GetConstant(0);

				case 1:
					return children[0];

				default:
					{
						using var childListRH = ListPool<IEvaluate<double>>.Rent();
						var childList = childListRH.Item;
						childList.AddRange(children);
						return SumOfCollection(catalog, childList);
					}
			}
		}

		public static IEvaluate<double> SumOf(
			this ICatalog<IEvaluate<double>> catalog,
			IEnumerable<IEvaluate<double>> children)
		{
			if (children is IReadOnlyList<IEvaluate<double>> ch) return SumOf(catalog, ch);

			if (catalog is null) throw new ArgumentNullException(nameof(catalog));
			if (children is null) throw new ArgumentNullException(nameof(children));
			Contract.EndContractBlock();

			using var e = children.GetEnumerator();
			if (!e.MoveNext()) return ConstantExtensions.GetConstant<double>(catalog, (dynamic)0);
			var v0 = e.Current;
			if (!e.MoveNext()) return v0;

			using var childListRH = ListPool<IEvaluate<double>>.Rent();
			var childList = childListRH.Item;
			childList.Add(v0);
			do { childList.Add(e.Current); }
			while (e.MoveNext());
			return SumOfCollection(catalog, childList);
		}

		public static IEvaluate<double> SumOf(
			this ICatalog<IEvaluate<double>> catalog,
			IEvaluate<double> child1,
			IEvaluate<double> child2,
			params IEvaluate<double>[] moreChildren)
			=> SumOf(catalog, moreChildren.Prepend(child2).Prepend(child1));

		public static IEvaluate<double> SumOf(
			this ICatalog<IEvaluate<double>> catalog,
			in double multiple,
			IEvaluate<double> child,
			params IEvaluate<double>[] moreChildren)
			=> SumOf(catalog, moreChildren.Prepend(child).Prepend(catalog.GetConstant(multiple)));

	}

}
