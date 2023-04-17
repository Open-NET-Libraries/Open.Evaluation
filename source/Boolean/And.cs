/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Evaluation.Core;
using System.Diagnostics.Contracts;
using Throw;

namespace Open.Evaluation.Boolean;

public class And : OperatorBase<bool>,
	IReproducable<IEnumerable<IEvaluate<bool>>, IEvaluate<bool>>
{
	public And(IEnumerable<IEvaluate<bool>> children)
		: base(Symbols.And, children, true)
	{ }

	protected override EvaluationResult<bool> EvaluateInternal(object context)
	{
		if (Children.Length == 0)
			throw new NotSupportedException("Cannot resolve boolean of empty set.");

		var r = ChildResults(context).All(result => (bool)result);
	}

	internal static And Create(
		ICatalog<IEvaluate<bool>> catalog,
		IEnumerable<IEvaluate<bool>> param)
	{
		catalog.ThrowIfNull();
		param.ThrowIfNull();
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
