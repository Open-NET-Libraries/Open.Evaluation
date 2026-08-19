using Open.Evaluation.Arithmetic;
using Open.Evaluation.Boolean;

namespace Open.Evaluation.Tests.Core;

[TestClass]
public class ContextEvaluatedCount
{
	[TestMethod]
	public void CountsDistinctNodesActuallyEvaluated_ShortCircuitAware()
	{
		using var c = new EvaluationCatalog<bool>();
		var a = c.GetParameter(0); var x = c.GetParameter(1); var y = c.GetParameter(2);
		var mux = c.Conditional((a, x, y));                         // a ? x : y
		var sop = c.Or([c.And([a, x]), c.And([c.Not(a), y])]);      // (a & x) | (!a & y)

		// a = true: the conditional touches a, x and itself; the SOP touches a, x, the first And,
		// then (Or does not short-circuit on a true? it does) -- count the difference honestly.
		int muxCount = Count(c, mux, [true, true, false]);
		int sopCount = Count(c, sop, [true, true, false]);
		muxCount.Should().Be(3 + 1, "condition, taken branch, the conditional itself, plus the pre-bound unused parameter y");
		sopCount.Should().BeGreaterThan(muxCount, "the sum-of-products form does more work for the same answer");
	}

	static int Count(EvaluationCatalog<bool> c, IEvaluate<bool> e, bool[] values)
	{
		using var lease = Context.Rent();
		var context = lease.Item;
		for (ushort i = 0; i < values.Length; i++) context.AddParam(c, i, values[i]);
		_ = e.Evaluate(context).Result;
		return context.EvaluatedCount;
	}
}
