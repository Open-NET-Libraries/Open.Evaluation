/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Evaluation.Core;
using System.Diagnostics.Contracts;
using Throw;

namespace Open.Evaluation.Boolean;

public class Or : OperatorBase<bool>,
	IReproducable<IEnumerable<IEvaluate<bool>>, IEvaluate<bool>>
{
	public Or(IEnumerable<IEvaluate<bool>> children)
		: base(BooleanSymbols.Or, children, true)
	{ }

	protected override bool EvaluateInternal(object context)
		=> Children.Length == 0
			? throw new NotSupportedException("Cannot resolve boolean of empty set.")
			: ChildResults(context).Cast<bool>().Any();

	internal static Or Create(
		ICatalog<IEvaluate<bool>> catalog,
		IEnumerable<IEvaluate<bool>> param)
	{
		catalog.ThrowIfNull();
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
