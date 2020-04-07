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
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "Intentional.")]
	public class And : OperatorBase<bool>,
		IReproducable<IEnumerable<IEvaluate<bool>>, IEvaluate<bool>>
	{
		public const char SYMBOL = '&';
		public const string SEPARATOR = " & ";

		public And(IEnumerable<IEvaluate<bool>> children)
			: base(SYMBOL, SEPARATOR, children, true)
		{ }

		protected override bool EvaluateInternal(object context)
		{
			if (Children.Length == 0)
				throw new InvalidOperationException("Cannot resolve boolean of empty set.");

			return ChildResults(context).All(result => (bool)result);
		}

		internal static And Create(
			ICatalog<IEvaluate<bool>> catalog,
			IEnumerable<IEvaluate<bool>> param)
		{
			if (catalog is null) throw new ArgumentNullException(nameof(catalog));
			if (param is null) throw new ArgumentNullException(nameof(param));
			Contract.EndContractBlock();

			return catalog.Register(new And(param));
		}

		public IEvaluate<bool> NewUsing(
			ICatalog<IEvaluate<bool>> catalog,
			IEnumerable<IEvaluate<bool>> param)
			=> Create(catalog, param);

	}

	public static class AndExtensions
	{
		public static IEvaluate<bool> And(
			this ICatalog<IEvaluate<bool>> catalog,
			IEnumerable<IEvaluate<bool>> children)
			=> Boolean.And.Create(catalog, children);
	}
}
