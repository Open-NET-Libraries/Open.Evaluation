using Open.Evaluation.Boolean;

namespace Open.Evaluation.Tests.Boolean;

[TestClass]
public class ConditionalRenderingCollision
{
	// Interning is keyed by rendering, so two different trees must never render the same.
	// A bare ternary next to a prefix Not did exactly that.
	[TestMethod]
	public void NotOfConditional_AndConditionalOfNot_AreDistinctInTheCatalog()
	{
		using var catalog = new EvaluationCatalog<bool>();
		var p0 = catalog.GetParameter(0);
		var p1 = catalog.GetParameter(1);
		var p2 = catalog.GetParameter(2);

		var notOfConditional = catalog.Not(catalog.Conditional((p0, p2, p1)));
		var conditionalOfNot = catalog.Conditional((catalog.Not(p0), p2, p1));

		notOfConditional.Description.Value.Should().Be("!({0} ? {2} : {1})");
		conditionalOfNot.Description.Value.Should().Be("(!{0} ? {2} : {1})");
		ReferenceEquals(notOfConditional, conditionalOfNot).Should().BeFalse();
	}
}
