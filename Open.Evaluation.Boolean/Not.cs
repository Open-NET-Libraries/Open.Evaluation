using Open.Evaluation.Core;
using System.Diagnostics.CodeAnalysis;

namespace Open.Evaluation.Boolean;

public sealed class Not : OperatorBase<bool>,
	IReproducable<IEvaluate<bool>, IEvaluate<bool>>
{
	internal Not(ICatalog<IEvaluate<bool>> catalog, IEvaluate<bool> contents)
		: base(catalog, Symbols.Not,
			  Enumerable.Repeat(contents ?? throw new ArgumentNullException(nameof(contents)), 1))
	{ }
	protected override EvaluationResult<bool> EvaluateInternal(Context context)
	{
		var r = ChildResults(context).Single();
		return new(!r.Result, v => $"!{v}");
	}

	internal static Not Create(
		ICatalog<IEvaluate<bool>> catalog,
		IEvaluate<bool> param)
		=> catalog.Register(new Not(catalog, param));

	[SuppressMessage("Performance", "CA1822:Mark members as static")]
	public Not NewUsing(
		ICatalog<IEvaluate<bool>> catalog,
		IEvaluate<bool> param)
		=> Create(catalog, param);

	public Not NewUsing(
		IEvaluate<bool> param)
		=> Create(Catalog, param);

	IEvaluate<bool> IReproducable<IEvaluate<bool>, IEvaluate<bool>>.NewUsing(ICatalog<IEvaluate<bool>> catalog, IEvaluate<bool> param)
		=> NewUsing(catalog, param);

	IEvaluate<bool> IReproducable<IEvaluate<bool>, IEvaluate<bool>>.NewUsing(IEvaluate<bool> param)
		=> NewUsing(param);
}

public static partial class BooleanExtensions
{
	public static Not Not(
		this ICatalog<IEvaluate<bool>> catalog,
		IEvaluate<bool> param)
		=> Boolean.Not.Create(catalog, param);
}
