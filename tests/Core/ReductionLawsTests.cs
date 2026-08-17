using Open.Evaluation.Arithmetic;

namespace Open.Evaluation.Tests.Core;

/// <summary>
/// Pins the reduction laws that the rest of the library (and any downstream consumer) can rely on:
/// idempotency (already covered in <c>Arithmetic/Reduction.cs</c>) and the more fundamental
/// requirement that reduction never changes what an expression EVALUATES to, only its shape.
/// </summary>
[TestClass]
public class ReductionLawsTests
{
	[TestMethod]
	public void GetReduced_PreservesEvaluationResults_AcrossRandomInputs()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		var p1 = catalog.GetParameter(1);
		var p2 = catalog.GetParameter(2);

		// A moderately complex expression with real constant-folding and merge opportunities.
		var expr = catalog.SumOf(
			catalog.ProductOf(p0, catalog.GetConstant(2d)),
			catalog.ProductOf(p0, catalog.GetConstant(3d)),
			catalog.GetExponent(p1, catalog.GetConstant(2d)),
			p2,
			catalog.GetConstant(5d),
			catalog.GetConstant(7d));

		var reduced = catalog.GetReduced(expr);
		reduced.Description.Value.Should().NotBe(expr.Description.Value,
			"the expression must actually be simplified for this test to be meaningful");

		var random = new Random(12345);
		for (var i = 0; i < 100; i++)
		{
			double[] values =
			[
				(random.NextDouble() * 200) - 100,
				(random.NextDouble() * 200) - 100,
				(random.NextDouble() * 200) - 100,
			];

			var before = Context.Evaluate(expr, (ReadOnlySpan<double>)values).Result;
			var after = Context.Evaluate(reduced, (ReadOnlySpan<double>)values).Result;

			if (double.IsNaN(before))
			{
				double.IsNaN(after).Should().BeTrue();
				continue;
			}

			var relativeError = before == 0d
				? Math.Abs(after)
				: Math.Abs((after - before) / before);

			relativeError.Should().BeLessThanOrEqualTo(1e-12,
				$"reduction must preserve the evaluated result for inputs [{string.Join(",", values)}] (before={before}, after={after})");
		}
	}

	[TestMethod]
	public void GetReduced_PreservesEvaluationResults_ForExactlyRepresentableIntegers()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);

		var expr = catalog.SumOf(
			catalog.ProductOf(p0, catalog.GetConstant(4d)),
			catalog.ProductOf(p0, catalog.GetConstant(6d)),
			catalog.GetConstant(3d),
			catalog.GetConstant(2d));

		var reduced = catalog.GetReduced(expr);

		foreach (double x in (double[])[-10d, -1d, 0d, 1d, 2d, 10d, 100d])
		{
			var before = Context.Evaluate(expr, (ReadOnlySpan<double>)[x]).Result;
			var after = Context.Evaluate(reduced, (ReadOnlySpan<double>)[x]).Result;

			// Integer-valued inputs through +/* only stay exactly representable in double.
			after.Should().Be(before, $"exact equality expected for integral input {x}");
		}
	}
}
