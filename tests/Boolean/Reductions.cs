using Open.Evaluation.Arithmetic;
using Open.Evaluation.Boolean;

namespace Open.Evaluation.Tests.Boolean;

[TestClass]
public class Reductions
{
	static (EvaluationCatalog<bool> c, IEvaluate<bool> a, IEvaluate<bool> b, IEvaluate<bool> x) Setup()
	{
		var c = new EvaluationCatalog<bool>();
		return (c, c.GetParameter(0), c.GetParameter(1), c.GetParameter(2));
	}

	static string R(EvaluationCatalog<bool> c, IEvaluate<bool> e) => c.GetReduced(e).Description.Value;

	[TestMethod]
	public void Not_DoubleNegation_And_Constants()
	{
		var (c, a, _, _) = Setup();
		R(c, c.Not(c.Not(a))).Should().Be("{0}");
		R(c, c.Not(c.GetConstant(true))).Should().Be("False");
		R(c, c.Not(c.Not(c.Not(a)))).Should().Be("!{0}");
	}

	[TestMethod]
	public void And_Identities()
	{
		var (c, a, b, x) = Setup();
		R(c, c.And([a, a])).Should().Be("{0}");                                   // idempotence
		R(c, c.And([a, c.Not(a)])).Should().Be("False");                          // contradiction
		R(c, c.And([a, c.GetConstant(false), b])).Should().Be("False");
		R(c, c.And([a, c.GetConstant(true), b])).Should().Be("({0} & {1})");      // identity dropped
		R(c, c.And([a, c.And([b, x])])).Should().Be("({0} & {1} & {2})");         // flatten
		R(c, c.And([a, c.Or([a, b])])).Should().Be("{0}");                        // absorption
		R(c, c.And([a, b])).Should().Be("({0} & {1})");                           // already canonical
	}

	[TestMethod]
	public void Or_Identities()
	{
		var (c, a, b, x) = Setup();
		R(c, c.Or([a, a])).Should().Be("{0}");
		R(c, c.Or([a, c.Not(a)])).Should().Be("True");                            // tautology
		R(c, c.Or([a, c.GetConstant(true), b])).Should().Be("True");
		R(c, c.Or([a, c.GetConstant(false), b])).Should().Be("({0} | {1})");
		R(c, c.Or([a, c.Or([b, x])])).Should().Be("({0} | {1} | {2})");
		R(c, c.Or([a, c.And([a, b])])).Should().Be("{0}");                        // absorption
	}

	[TestMethod]
	public void Conditional_Identities()
	{
		var (c, a, b, x) = Setup();
		R(c, c.Conditional((a, b, b))).Should().Be("{1}");                        // c ? x : x
		R(c, c.Conditional((c.GetConstant(true), b, x))).Should().Be("{1}");
		R(c, c.Conditional((c.GetConstant(false), b, x))).Should().Be("{2}");
		R(c, c.Conditional((c.Not(a), b, x))).Should().Be("({0} ? {2} : {1})");   // !c ? a : b → c ? b : a
		R(c, c.Conditional((a, a, b))).Should().Be("({0} | {1})");                // c ? c : b
		R(c, c.Conditional((a, b, a))).Should().Be("({0} & {1})");                // c ? a : c
		R(c, c.Conditional((a, c.GetConstant(true), b))).Should().Be("({0} | {1})");
		R(c, c.Conditional((a, c.GetConstant(false), b))).Should().Be("(!{0} & {1})");
		R(c, c.Conditional((a, b, c.GetConstant(true)))).Should().Be("(!{0} | {1})");
		R(c, c.Conditional((a, b, c.GetConstant(false)))).Should().Be("({0} & {1})");
		R(c, c.Conditional((a, b, x))).Should().Be("({0} ? {1} : {2})");          // a real mux stays
	}

	[TestMethod]
	public void NumericConditional_ReducesBranchesAndCondition_KeepsShape()
	{
		using var bools = new EvaluationCatalog<bool>();
		using var doubles = new EvaluationCatalog<double>();
		var cond = bools.Not(bools.Not(bools.GetParameter(0)));
		var v = doubles.GetParameter(1);
		var conditional = doubles.Conditional((cond, doubles.SumOf(v, doubles.GetConstant(0d)), doubles.GetParameter(2)));
		doubles.GetReduced(conditional).Description.Value.Should().Be("({0} ? {1} : {2})");
	}

	// The property that makes reductions trustworthy as a validity/identity detector: for random
	// trees over three variables, the reduction must agree with the source on all 8 input rows.
	[TestMethod]
	public void RandomTrees_ReductionIsTruthTableEquivalent()
	{
		var random = new Random(20260818);
		using var c = new EvaluationCatalog<bool>();
		int reducedCount = 0;
		for (int i = 0; i < 400; i++)
		{
			IEvaluate<bool> tree = RandomTree(c, random, depth: 4);
			IEvaluate<bool> reduced = c.GetReduced(tree);
			if (reduced != tree) reducedCount++;
			for (int row = 0; row < 8; row++)
			{
				bool[] bits = [(row & 1) == 1, (row & 2) == 2, (row & 4) == 4];
				Eval(c, tree, bits).Should().Be(Eval(c, reduced, bits), $"{tree.Description.Value} vs {reduced.Description.Value} at row {row}");
			}
		}

		reducedCount.Should().BeGreaterThan(100, "the sweep must actually exercise reductions");
	}

	static bool Eval(EvaluationCatalog<bool> c, IEvaluate<bool> e, bool[] bits)
	{
		using var lease = Context.Rent();
		var context = lease.Item.Init(c, (ReadOnlySpan<bool>)bits);
		return e.Evaluate(context).Result;
	}

	static IEvaluate<bool> RandomTree(EvaluationCatalog<bool> c, Random r, int depth)
		=> depth == 0 || r.Next(4) == 0
			? r.Next(6) == 0 ? c.GetConstant(r.Next(2) == 0) : c.GetParameter((ushort)r.Next(3))
			: r.Next(4) switch
			{
				0 => c.Not(RandomTree(c, r, depth - 1)),
				1 => c.And([RandomTree(c, r, depth - 1), RandomTree(c, r, depth - 1)]),
				2 => c.Or([RandomTree(c, r, depth - 1), RandomTree(c, r, depth - 1)]),
				_ => c.Conditional((RandomTree(c, r, depth - 1), RandomTree(c, r, depth - 1), RandomTree(c, r, depth - 1))),
			};
}
