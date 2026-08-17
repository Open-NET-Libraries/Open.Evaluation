namespace Open.Evaluation.Tests.Core;

[TestClass]
public class CatalogTests
{
	[TestMethod]
	public void Shared_ReturnsSameInstance_PerClosedGenericType()
	{
		var a = Catalog<IEvaluate<double>>.Shared;
		var b = Catalog<IEvaluate<double>>.Shared;
		ReferenceEquals(a, b).Should().BeTrue();
	}

	[TestMethod]
	public void Register_RefOverload_ReturnsInternedInstance()
	{
		using var catalog = new EvaluationCatalog<double>();
		var probe = CountingProbe.Create(catalog, "REF-PROBE", 1d);

		var same = probe;
		catalog.Register(ref same);

		ReferenceEquals(same, probe).Should().BeTrue(
			"registering an already-interned instance via the ref overload must return the identical reference");
	}

	[TestMethod]
	public void Register_TwoArgFactoryOverload_RegistersAndInterns()
	{
		using var catalog = new EvaluationCatalog<double>();

		var first = CountingProbe.CreateViaTwoArgFactory(catalog, "TWO-ARG-PROBE", 9d);
		var second = CountingProbe.CreateViaTwoArgFactory(catalog, "TWO-ARG-PROBE", 9d);

		ReferenceEquals(first, second).Should().BeTrue();
	}

	[TestMethod]
	public void TryGetItem_ForRegisteredId_ReturnsTrueWithSameInstance()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);

		catalog.TryGetItem<Parameter<double>>(p0.Description.Value, out var found).Should().BeTrue();
		ReferenceEquals(found, p0).Should().BeTrue();
	}

	[TestMethod]
	[Ignore("QUESTION FOR AUTHOR: Catalog<T>.TryGetItem's `Debug.Assert(e is not null)` fires on the " +
		"legitimate 'not found' result (Registry.TryGetValue returning false), so in DEBUG builds " +
		"calling TryGetItem for an id that was never registered throws instead of returning false as " +
		"the Try-pattern promises. Observed directly: catalog.TryGetItem<Parameter<double>>(\"{9999}\", " +
		"out _) throws (MSTest's TestHostTraceListener converts the Debug.Fail into a " +
		"DebugAssertException with message 'e is not null'). In RELEASE builds the " +
		"[Conditional(\"DEBUG\")] assert is compiled out and it correctly returns false - so the API's " +
		"behavior differs by build configuration. Is the assert meant to guard something else (e.g. an " +
		"invariant that's supposed to be established earlier), or should TryGetItem's not-found path " +
		"not assert on `e` at all?")]
	public void TryGetItem_ForUnregisteredId_DebugAssertFiresInsteadOfReturningFalse()
	{
		using var catalog = new EvaluationCatalog<double>();
		catalog.TryGetItem<Parameter<double>>("{9999}", out _).Should().BeFalse();
	}

	[TestMethod]
	public void GetReduction_DefaultImplementation_ReturnsSelf_WhenNotOverridden()
	{
		using var catalog = new EvaluationCatalog<double>();
		var op = catalog.Register(new NonReducibleOperation(catalog, "PROBE-NR"));

		ReferenceEquals(op.GetReduction(), op).Should().BeTrue();

		op.TryGetReduced(out var reduction).Should().BeFalse();
		ReferenceEquals(reduction, op).Should().BeTrue();

		var reducedViaCatalog = catalog.GetReduced(op);
		ReferenceEquals(reducedViaCatalog, op).Should().BeTrue();
	}

	[TestMethod]
	public void IEvaluate_NonGeneric_Evaluate_DelegatesToTypedEvaluate()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		using var context = new Context();
		context.AddParam(catalog, 0, 42d);

		IEvaluate baseRef = p0;
		EvaluationResult<object> boxed = baseRef.Evaluate(context);

		boxed.Result.Should().Be(42d);
	}

	[TestMethod]
	public void IConstant_NonGeneric_Value_DelegatesToTypedValue()
	{
		using var catalog = new EvaluationCatalog<double>();
		var c = catalog.GetConstant(5d);

		// Deliberately typed as the non-generic IConstant to exercise its default interface member
		// (`object IConstant.Value => Value;`) rather than the concrete/typed Value property.
#pragma warning disable CA1859 // Intentionally using the interface type to reach the DIM under test.
		IConstant baseRef = c;
#pragma warning restore CA1859
		baseRef.Value.Should().Be(5d);
	}

	[TestMethod]
	public void Mutation_ReturnsSameSubmoduleInstance_LazilyInitializedOnce()
	{
		using var catalog = new EvaluationCatalog<double>();

		var a = catalog.Mutation;
		var b = catalog.Mutation;

		a.Should().NotBeNull();
		ReferenceEquals(a, b).Should().BeTrue();
		ReferenceEquals(a.Catalog, catalog).Should().BeTrue();
	}
}
