using Open.Evaluation.Boolean;

namespace Open.Evaluation.Tests.Boolean;

[TestClass]
public class And
{
	static (EvaluationCatalog<bool> Catalog, IEvaluate<bool> P0, IEvaluate<bool> P1) Setup()
	{
		var catalog = new EvaluationCatalog<bool>();
		return (catalog, catalog.GetParameter(0), catalog.GetParameter(1));
	}

	[DataTestMethod]
	[DataRow(true, true, true)]
	[DataRow(true, false, false)]
	[DataRow(false, true, false)]
	[DataRow(false, false, false)]
	public void TruthTable(bool a, bool b, bool expected)
	{
		var (catalog, p0, p1) = Setup();
		var and = catalog.And([p0, p1]);

		using var lease = Context.Rent();
		var context = lease.Item.Init(catalog, (ReadOnlySpan<bool>)[a, b]);

		and.Evaluate(context).Result.Should().Be(expected);
	}

	[TestMethod]
	public void SingleChild_EqualsChildValue()
	{
		var catalog = new EvaluationCatalog<bool>();
		var p0 = catalog.GetParameter(0);
		var and = catalog.And([p0]);

		using var lease = Context.Rent();
		var context = lease.Item.Init(catalog, (ReadOnlySpan<bool>)[true]);
		and.Evaluate(context).Result.Should().BeTrue();
	}

	[TestMethod]
	public void EmptyChildren_Throws()
	{
		var catalog = new EvaluationCatalog<bool>();
		Action act = () => catalog.And([]);
		act.Should().Throw<ArgumentException>();
	}

	[TestMethod]
	public void NullCatalog_Throws()
	{
		ICatalog<IEvaluate<bool>> catalog = null!;
		Action act = () => catalog.And([]);
		act.Should().Throw<Exception>();
	}

	[TestMethod]
	public void NullChildren_Throws()
	{
		var catalog = new EvaluationCatalog<bool>();
		Action act = () => catalog.And(null!);
		act.Should().Throw<Exception>();
	}

	[TestMethod]
	public void Description_UsesPaddedAmpersand()
	{
		var (catalog, p0, p1) = Setup();
		var and = catalog.And([p0, p1]);
		and.Description.Value.Should().Be("({0} & {1})");
	}

	[TestMethod]
	public void ThreeChildren_AllTrue_IsTrue()
	{
		var catalog = new EvaluationCatalog<bool>();
		IEvaluate<bool>[] ps = [catalog.GetParameter(0), catalog.GetParameter(1), catalog.GetParameter(2)];
		var and = catalog.And(ps);

		using var lease = Context.Rent();
		var context = lease.Item.Init(catalog, (ReadOnlySpan<bool>)[true, true, true]);
		and.Evaluate(context).Result.Should().BeTrue();
	}

	[TestMethod]
	public void ThreeChildren_OneFalse_IsFalse()
	{
		var catalog = new EvaluationCatalog<bool>();
		IEvaluate<bool>[] ps = [catalog.GetParameter(0), catalog.GetParameter(1), catalog.GetParameter(2)];
		var and = catalog.And(ps);

		using var lease = Context.Rent();
		var context = lease.Item.Init(catalog, (ReadOnlySpan<bool>)[true, false, true]);
		and.Evaluate(context).Result.Should().BeFalse();
	}

	[TestMethod]
	public void Interning_StructurallyIdentical_ReturnsSameInstance()
	{
		var (catalog, p0, p1) = Setup();
		var a = catalog.And([p0, p1]);
		var b = catalog.And([catalog.GetParameter(0), catalog.GetParameter(1)]);

		ReferenceEquals(a, b).Should().BeTrue();
	}

	[TestMethod]
	public void ReorderedChildren_ProduceSameInternedInstance()
	{
		// And reorders children (parameters sorted by Id), so construction order shouldn't matter.
		var (catalog, p0, p1) = Setup();
		var forward = catalog.And([p0, p1]);
		var reversed = catalog.And([p1, p0]);

		ReferenceEquals(forward, reversed).Should().BeTrue();
	}

	[TestMethod]
	public void ShortCircuitEvaluation_StillEvaluatesAllChildren()
	{
		// And.EvaluateInternal uses .All(), which is lazy/short-circuiting over the
		// enumerable, but ChildResults evaluates each child as it's enumerated -
		// verify a false-first case still resolves to false without throwing.
		var catalog = new EvaluationCatalog<bool>();
		var p0 = catalog.GetParameter(0);
		var p1 = catalog.GetParameter(1);
		var and = catalog.And([p0, p1]);

		using var lease = Context.Rent();
		var context = lease.Item.Init(catalog, (ReadOnlySpan<bool>)[false, true]);
		and.Evaluate(context).Result.Should().BeFalse();
	}
}
