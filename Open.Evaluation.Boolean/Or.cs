using Open.Collections;
using Open.Evaluation.Core;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using Throw;

namespace Open.Evaluation.Boolean;

public sealed class Or : OperatorBase<bool>,
	IReproducable<IEnumerable<IEvaluate<bool>>, IEvaluate<bool>>
{
	private Or(ICatalog<IEvaluate<bool>> catalog, IEnumerable<IEvaluate<bool>> children)
		: base(catalog, Symbols.Or, children, true) { }

	protected override EvaluationResult<bool> EvaluateInternal(Context context)
	{
		Children.Length.Throw("Cannot resolve boolean of empty set.").IfEquals(0);

		var results = ChildResults(context).Memoize();
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

		return catalog.Register(new Or(catalog, param));
	}

	[SuppressMessage("Performance", "CA1822:Mark members as static")]
	public Or NewUsing(
		ICatalog<IEvaluate<bool>> catalog,
		IEnumerable<IEvaluate<bool>> param)
		=> Create(catalog, param);

	public Or NewUsing(
		IEnumerable<IEvaluate<bool>> param)
		=> Create(Catalog, param);

	IEvaluate<bool> IReproducable<IEnumerable<IEvaluate<bool>>, IEvaluate<bool>>.NewUsing(ICatalog<IEvaluate<bool>> catalog, IEnumerable<IEvaluate<bool>> param)
		=> NewUsing(catalog, param);

	IEvaluate<bool> IReproducable<IEnumerable<IEvaluate<bool>>, IEvaluate<bool>>.NewUsing(IEnumerable<IEvaluate<bool>> param)
		=> NewUsing(param);
}

public static class OrExtensions
{
	public static Or Or(
		this ICatalog<IEvaluate<bool>> catalog,
		IEnumerable<IEvaluate<bool>> children)
		=> Boolean.Or.Create(catalog, children);
}
