/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Evaluation.Core;

namespace Open.Evaluation.Boolean.Counting;

public abstract class CountingBase : OperatorBase<bool>
{
	public static readonly Symbol SymbolInstance = new(',', ", ");

	protected CountingBase(string prefix, int count, IEnumerable<IEvaluate<bool>> children, int minimumChildren = 0)
		: base(SymbolInstance, children, true, minimumChildren)
	{
		if (count < 0)
			throw new ArgumentOutOfRangeException(nameof(count), count, "Count must be at least 0.");
		Prefix = prefix ?? throw new ArgumentNullException(nameof(prefix));
		Count = count;
	}

	protected string Prefix { get; }

	// ReSharper disable once MemberCanBeProtected.Global
	public int Count
	{
		get;
	}

	protected override string ToStringInternal(object context)
		=> $"{Prefix}({Count}, {base.ToStringInternal(context)})";
}
