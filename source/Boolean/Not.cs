/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Evaluation.Core;
using System;
using System.Linq;
using System.Text;

namespace Open.Evaluation.Boolean;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1716:Identifiers should not match keywords")]
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

	// OperatorBase only emits the symbol BETWEEN children, so a single-child operator
	// would render with no symbol at all -- Not(x) came out as "({0})", indistinguishable
	// from a merely-parenthesized child, and Not(a & b) as "(({0} & {1}))". Since the string
	// form doubles as an identity/hash for consumers, that conflated distinct expressions.
	// Prefix the symbol instead: Not(x) renders as "(!{0})".
	protected override void ToStringInternal_OnAppendNextChild(StringBuilder result, int index, object child)
	{
		result.Append(SymbolString);
		result.Append(child);
	}
}

public static partial class BooleanExtensions
{
	public static IEvaluate<bool> Not(
		this ICatalog<IEvaluate<bool>> catalog,
		IEvaluate<bool> param)
		=> Boolean.Not.Create(catalog, param);
}
