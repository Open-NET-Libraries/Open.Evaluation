using Open.Evaluation.Arithmetic;

namespace Open.Evaluation.Tests.Core;

/// <summary>
/// Proves the library's core differentiator: many roots evaluating the SAME parameter set through
/// ONE shared <see cref="Context"/> compute an interned shared subtree exactly ONCE for the whole
/// population, using a counting probe node (see <see cref="CountingProbe"/>) rather than inferring
/// it indirectly from timing or from re-checking values alone.
/// </summary>
[TestClass]
public class PopulationScopedContextTests
{
	[TestMethod]
	public void SharedSubtree_AcrossDifferentRoots_EvaluatedExactlyOncePerContext()
	{
		using var catalog = new EvaluationCatalog<double>();
		var probe = CountingProbe.Create(catalog, "PROBE-A", 7d);
		var p0 = catalog.GetParameter(0);
		var p1 = catalog.GetParameter(1);

		// Two structurally DIFFERENT roots that both reference the SAME interned probe instance.
		var root1 = catalog.SumOf(probe, p0);
		var root2 = catalog.ProductOf(probe, p1);

		using var context = new Context();
		context.AddParam(catalog, 0, 3d);
		context.AddParam(catalog, 1, 4d);

		root1.Evaluate(context).Result.Should().Be(10d); // 7 + 3
		probe.EvaluationCount.Should().Be(1,
			"the probe should have actually run exactly once so far, for root1");

		root2.Evaluate(context).Result.Should().Be(28d); // 7 * 4
		probe.EvaluationCount.Should().Be(1,
			"root2 shares the SAME interned probe instance as root1; evaluating it through the same " +
			"Context must reuse the memoized result rather than recomputing it");
	}

	[TestMethod]
	public void SharedSubtree_TryGetResult_IsSameCachedResultAcrossRoots()
	{
		using var catalog = new EvaluationCatalog<double>();
		var probe = CountingProbe.Create(catalog, "PROBE-B", 11d);
		var p0 = catalog.GetParameter(0);

		var root1 = catalog.SumOf(probe, p0);
		var root2 = catalog.SumOf(probe, catalog.GetConstant(100d));

		using var context = new Context();
		context.AddParam(catalog, 0, 1d);

		root1.Evaluate(context);
		context.TryGetResult<double>(probe, out var afterFirst).Should().BeTrue();

		root2.Evaluate(context);
		context.TryGetResult<double>(probe, out var afterSecond).Should().BeTrue();

		// Same Result AND same Lazy<string> Description reference (EvaluationResult<T> is a record
		// struct whose equality is member-wise) - proving it's the identical cached entry, not a
		// freshly recomputed one that merely happens to carry the same value.
		afterSecond.Should().Be(afterFirst);
		probe.EvaluationCount.Should().Be(1);
	}

	[TestMethod]
	public void SharedSubtree_AfterContextClear_Recomputes()
	{
		using var catalog = new EvaluationCatalog<double>();
		var probe = CountingProbe.Create(catalog, "PROBE-C", 2d);

		using var context = new Context();

		probe.Evaluate(context);
		probe.EvaluationCount.Should().Be(1);

		probe.Evaluate(context);
		probe.EvaluationCount.Should().Be(1, "still memoized before clearing");

		context.Clear();

		probe.Evaluate(context);
		probe.EvaluationCount.Should().Be(2, "clearing the context must force recomputation on next evaluation");
	}

	[TestMethod]
	public void SharedSubtree_AcrossDifferentContexts_EvaluatedOncePerContext()
	{
		// Population-scoping is per-Context, not global to the node: the SAME interned node
		// evaluated through two DIFFERENT contexts (e.g. two individuals' own Context) must run
		// its evaluation logic once per context - the memoization is scoped to the population
		// sharing ONE context, not to the node itself.
		using var catalog = new EvaluationCatalog<double>();
		var probe = CountingProbe.Create(catalog, "PROBE-D", 3d);

		using var contextA = new Context();
		using var contextB = new Context();

		probe.Evaluate(contextA);
		probe.Evaluate(contextA);
		probe.Evaluate(contextB);

		probe.EvaluationCount.Should().Be(2);
	}

	[TestMethod]
	public void SharedSubtree_ThreeRootsInLargerPopulation_StillEvaluatedOnce()
	{
		// A slightly larger "population" of roots (3 distinct expressions) all sharing one
		// interned probe, to reinforce that the sharing isn't an artifact of only having 2 roots.
		using var catalog = new EvaluationCatalog<double>();
		var probe = CountingProbe.Create(catalog, "PROBE-E", 5d);
		var p0 = catalog.GetParameter(0);
		var p1 = catalog.GetParameter(1);

		var root1 = catalog.SumOf(probe, p0);
		var root2 = catalog.ProductOf(probe, p1);
		var root3 = catalog.SumOf(catalog.ProductOf(probe, catalog.GetConstant(2d)), p0);

		using var context = new Context();
		context.AddParam(catalog, 0, 1d);
		context.AddParam(catalog, 1, 2d);

		root1.Evaluate(context).Result.Should().Be(6d);
		root2.Evaluate(context).Result.Should().Be(10d);
		root3.Evaluate(context).Result.Should().Be(11d);

		probe.EvaluationCount.Should().Be(1,
			"all three roots share the same interned probe; it must compute only once for the whole population");
	}
}
