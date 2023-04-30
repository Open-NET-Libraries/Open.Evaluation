/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Evaluation.Core;
using Throw;

namespace Open.Evaluation.Boolean.Counting;

public abstract class CountingBase
	: OperatorBase<bool>
{
	protected CountingBase(
		string prefix,
		int count,
		IEnumerable<IEvaluate<bool>> children,
		int minimumChildren = 0)
		: base(Symbols.Counting, children, true, minimumChildren)
	{
		PrefixValue = prefix.ThrowIfNull();
		Count = count.Throw().IfLessThan(0);
	}

	public const char Glyph = ',';

	protected string PrefixValue { get; }

	// ReSharper disable once MemberCanBeProtected.Global
	public int Count
	{
		get;
	}

	protected override string ToStringInternal(object context)
		=> $"{PrefixValue}({Count}, {base.ToStringInternal(context)})";
}
