/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Evaluation.Core;
using System;
using System.Linq;

namespace Open.Evaluation.Boolean
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "Intentional.")]
	public class Not : OperatorBase<bool>,
		IReproducable<IEvaluate<bool>, IEvaluate<bool>>
	{
		public const char SYMBOL = '!';
		public const string SYMBOL_STRING = "!";

		internal Not(IEvaluate<bool> contents)
			: base(SYMBOL, SYMBOL_STRING,
				  Enumerable.Repeat(contents ?? throw new ArgumentNullException(nameof(contents)), 1))
		{ }

		internal static IEvaluate<bool> Create(
			ICatalog<IEvaluate<bool>> catalog,
			IEvaluate<bool> param)
			=> catalog.Register(new Not(param));

		public IEvaluate<bool> NewUsing(
			ICatalog<IEvaluate<bool>> catalog,
			IEvaluate<bool> param)
			=> Create(catalog, param);

		protected override bool EvaluateInternal(object context)
			=> !ChildResults(context).Cast<bool>().Single();

	}

	public static partial class BooleanExtensions
	{
		public static IEvaluate<bool> Not(
			this ICatalog<IEvaluate<bool>> catalog,
			IEvaluate<bool> param)
			=> Boolean.Not.Create(catalog, param);
	}

}
