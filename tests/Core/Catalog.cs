namespace Open.Evaluation.Tests.Core;

// Issue #6(c): Catalog.Register interns purely by ToString()/Describe(), with no type
// disambiguation. On an interning HIT (the key already maps to a registered instance),
// the old code blindly cast that instance to the requested TItem, throwing an unhelpful
// InvalidCastException if the types didn't match (as happened for Exponent(x,1) colliding
// with the bare Parameter x -- see tests/Core/Exponent.cs's PowerOfOneIdentity). These
// tests exercise the guardrail directly, using a minimal fake IEvaluate<double> whose
// rendering is engineered to collide with an already-registered node of a different type.
[TestClass]
public class Catalog
{
	// A deliberately colliding IEvaluate<double>: its Describe() (and therefore its catalog
	// key) is supplied by the test, independent of its runtime type, so it can be made to
	// collide with any already-registered node's key on demand.
	private sealed class FakeCollidingEvaluation(ICatalog<IEvaluate<double>> catalog, string collidingKey)
		: EvaluationBase<double>(catalog)
	{
		protected override string Describe() => collidingKey;

		protected override EvaluationResult<double> EvaluateInternal(Context context)
			=> new(0d, Description);
	}

	[TestMethod]
	public void Register_TypeMismatchOnInterningHit_ThrowsDescriptiveExceptionNotInvalidCast()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0); // registers key "{0}" as a Parameter<double>.
		string collidingKey = p0.Description.Value;

		var fake = new FakeCollidingEvaluation(catalog, collidingKey);

		Action act = () => catalog.Register(fake);

		// InvalidCastException does not derive from InvalidOperationException, so this
		// assertion alone already excludes the old blind-cast failure mode; asserting the
		// new, descriptive exception type is the whole point of the guardrail.
		act.Should().Throw<InvalidOperationException>()
			.WithMessage("*Catalog identity collision*");
	}

	[TestMethod]
	public void Register_SameKeySameType_ReturnsTheExistingInstance()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		var p0Again = catalog.GetParameter(0);

		ReferenceEquals(p0, p0Again).Should().BeTrue("interning should return the same instance for the same key and matching type");
	}
}
