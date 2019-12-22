/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Evaluation.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Open.Evaluation.Boolean
{
	public class Or : OperatorBase<bool>,
		IReproducable<IEnumerable<IEvaluate<bool>>, IEvaluate<bool>>
	{
		public const char SYMBOL = '|';
		public const string SEPARATOR = " | ";

		public Or(IEnumerable<IEvaluate<bool>> children)
			: base(SYMBOL, SEPARATOR, children, true)
		{ }

		protected override bool EvaluateInternal(object context)
		{
			if (Children.Length == 0)
				throw new InvalidOperationException("Cannot resolve boolean of empty set.");

			return ChildResults(context).Cast<bool>().Any();
		}

		internal static Or Create(
			ICatalog<IEvaluate<bool>> catalog,
			IEnumerable<IEvaluate<bool>> param)
		{
			if (catalog is null) throw new ArgumentNullException(nameof(catalog));
			if (param is null) throw new ArgumentNullException(nameof(param));
			Contract.EndContractBlock();

			return catalog.Register(new Or(param));
		}

		public IEvaluate<bool> NewUsing(
			ICatalog<IEvaluate<bool>> catalog,
			IEnumerable<IEvaluate<bool>> param)
			=> Create(catalog, param);
	}

	public static class OrExtensions
	{
		public static IEvaluate<bool> Or(
			this ICatalog<IEvaluate<bool>> catalog,
			IEnumerable<IEvaluate<bool>> children)
			=> Boolean.Or.Create(catalog, children);
	}
}
