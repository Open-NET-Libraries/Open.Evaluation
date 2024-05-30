using Open.Evaluation.Core;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using Throw;

namespace Open.Evaluation.Boolean;

public sealed class And : OperatorBase<bool>,
	IReproducable<IEnumerable<IEvaluate<bool>>, IEvaluate<bool>>
{
	private And(ICatalog<IEvaluate<bool>> catalog, IEnumerable<IEvaluate<bool>> children)
		: base(catalog, Symbols.And, children, true) { }

	protected override EvaluationResult<bool> EvaluateInternal(Context context)
	{
		Children.Length.Throw("Cannot resolve boolean of empty set.").IfEquals(0);

		var results = ChildResults(context);
		return new(
			results.All(r => r.Result),
			Describe(results.Select(r => r.Description)));
	}

	internal static And Create(
		ICatalog<IEvaluate<bool>> catalog,
		IEnumerable<IEvaluate<bool>> param)
	{
		catalog.ThrowIfNull();
		param.ThrowIfNull();
		Contract.EndContractBlock();

		return catalog.Register(new And(catalog, param));
	}

	[SuppressMessage("Performance", "CA1822:Mark members as static")]
	public And NewUsing(
		ICatalog<IEvaluate<bool>> catalog,
		IEnumerable<IEvaluate<bool>> param)
		=> Create(catalog, param);

	public And NewUsing(
		IEnumerable<IEvaluate<bool>> param)
		=> Create(Catalog, param);

	IEvaluate<bool> IReproducable<IEnumerable<IEvaluate<bool>>, IEvaluate<bool>>.NewUsing(ICatalog<IEvaluate<bool>> catalog, IEnumerable<IEvaluate<bool>> param)
		=> NewUsing(catalog, param);

	IEvaluate<bool> IReproducable<IEnumerable<IEvaluate<bool>>, IEvaluate<bool>>.NewUsing(IEnumerable<IEvaluate<bool>> param)
		=> NewUsing(param);
}

public static partial class BooleanExtensions
{
	public static And And(
		this ICatalog<IEvaluate<bool>> catalog,
		IEnumerable<IEvaluate<bool>> children)
		=> Boolean.And.Create(catalog, children);
}
