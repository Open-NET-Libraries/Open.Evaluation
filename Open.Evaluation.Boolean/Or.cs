/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Evaluation.Core;
using System.Diagnostics.Contracts;
using Throw;

namespace Open.Evaluation.Boolean;

public class Or(IEnumerable<IEvaluate<bool>> children)
	: OperatorBase<bool>(Symbols.Or, children, true),
	IReproducable<IEnumerable<IEvaluate<bool>>, IEvaluate<bool>>
{
	public const char Glyph = '|';

	protected override EvaluationResult<bool> EvaluateInternal(Context context)
	{
		Children.Length.Throw("Cannot resolve boolean of empty set.").IfEquals(0);

		var results = ChildResults(context);
		return new(
			results.Any(r => r.Result),
			Describe(results.Select(r => r.Description)));
	}

	internal static Or Create(
		ICatalog<IEvaluate<bool>> catalog,
		IEnumerable<IEvaluate<bool>> param)
	{
		catalog.ThrowIfNull();
		param.ThrowIfNull();
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
