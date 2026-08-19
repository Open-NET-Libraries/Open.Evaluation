using Open.Evaluation.Core;
using System.Diagnostics.CodeAnalysis;

namespace Open.Evaluation.Boolean;

public sealed partial class Not : OperatorBase<bool>,
	IReproducable<IEvaluate<bool>, IEvaluate<bool>>
{
	internal Not(ICatalog<IEvaluate<bool>> catalog, IEvaluate<bool> contents)
		: base(catalog, Symbols.Not,
			  Enumerable.Repeat(contents ?? throw new ArgumentNullException(nameof(contents)), 1))
	{ }
	protected override EvaluationResult<bool> EvaluateInternal(Context context)
	{
		var r = ChildResults(context).Single();
		return new(!r.Result, Describe([r.Description]));
	}

	// NOTE: without this override, the inherited default OperatorBase.Describe(children) simply
	// wraps a single child in parens (it only injects the operator's Symbol.Text *between*
	// multiple children), so Not's static/unparameterized description - and therefore its
	// ToString()/catalog interning key - silently lost its "!" and rendered as e.g. "({0})"
	// instead of "!{0}". This affected both the static Description and (since EvaluateInternal
	// now reuses this) the evaluated Description, which previously showed the negated *result*
	// value (e.g. "!True" when the actual result was true) rather than the child's own
	// resolved text.
	protected override Lazy<string> Describe(IEnumerable<Lazy<string>> children)
		=> new(() => $"{Symbol.Text}{children.Single().Value}");

	internal static Not Create(
		ICatalog<IEvaluate<bool>> catalog,
		IEvaluate<bool> param)
		=> catalog.Register(new Not(catalog, param));

#pragma warning disable IDE0079 // Remove unnecessary suppression
	[SuppressMessage("Performance", "CA1822:Mark members as static")]
#pragma warning restore IDE0079 // Remove unnecessary suppression
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
