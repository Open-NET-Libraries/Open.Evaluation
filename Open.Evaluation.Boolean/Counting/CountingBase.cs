using Open.Evaluation.Core;
using Throw;

namespace Open.Evaluation.Boolean.Counting;

public abstract class CountingBase(
	ICatalog<IEvaluate<bool>> catalog, string prefix, int count, IEnumerable<IEvaluate<bool>> children, int minimumChildren = 0)
	: OperatorBase<bool>(catalog, Symbols.Counting, children, true, minimumChildren)
{
	protected string PrefixValue { get; } = prefix.ThrowIfNull();

	public int Count { get; } = count.Throw().IfLessThan(0);

	protected override Lazy<string> Describe(IEnumerable<Lazy<string>> children)
		=> new(() => $"{PrefixValue}({Count} from {base.Describe(children).Value})");
}
