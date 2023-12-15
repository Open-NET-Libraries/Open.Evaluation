/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Evaluation.Core;

namespace Open.Evaluation.Boolean;

public class Not : OperatorBase<bool>,
	IReproducable<IEvaluate<bool>, IEvaluate<bool>>
{
	public const char Glyph = '!';

	internal Not(IEvaluate<bool> contents)
		: base(Symbols.Not,
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

	protected override EvaluationResult<bool> EvaluateInternal(Context context)
	{
		var r = ChildResults(context).Single();
		return new(!r.Result, v => $"!{v}");
	}
}

public static partial class BooleanExtensions
{
	public static IEvaluate<bool> Not(
		this ICatalog<IEvaluate<bool>> catalog,
		IEvaluate<bool> param)
		=> Boolean.Not.Create(catalog, param);
}
