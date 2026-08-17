using Open.Evaluation.Boolean;

namespace Open.Evaluation.Tests.Boolean;

[TestClass]
public class Or
{
	static (EvaluationCatalog<bool> Catalog, IEvaluate<bool> P0, IEvaluate<bool> P1) Setup()
	{
		var catalog = new EvaluationCatalog<bool>();
		return (catalog, catalog.GetParameter(0), catalog.GetParameter(1));
	}

	[DataTestMethod]
	[DataRow(true, true, true)]
	[DataRow(true, false, true)]
	[DataRow(false, true, true)]
	[DataRow(false, false, false)]
	public void TruthTable(bool a, bool b, bool expected)
	{
		var (catalog, p0, p1) = Setup();
		var or = catalog.Or([p0, p1]);

		using var lease = Context.Rent();
		var context = lease.Item.Init(catalog, (ReadOnlySpan<bool>)[a, b]);

		or.Evaluate(context).Result.Should().Be(expected);
	}

	/// <summary>
	/// Regression test for a historical defect in the 1.x line: <c>Or</c> used a
	/// predicate-less <c>.Any()</c> (which only checks "are there any elements?"
	/// and is therefore always true for a non-empty child set), making Or
	/// constant-true regardless of actual child values. This explicitly verifies
	/// that on this (2.x) line, Or correctly returns false when every child is false.
	/// </summary>
	[TestMethod]
	public void AllChildrenFalse_IsFalse_NotConstantTrue()
	{
		var catalog = new EvaluationCatalog<bool>();
		IEvaluate<bool>[] ps = [catalog.GetParameter(0), catalog.GetParameter(1), catalog.GetParameter(2)];
		var or = catalog.Or(ps);

		using var lease = Context.Rent();
		var context = lease.Item.Init(catalog, (ReadOnlySpan<bool>)[false, false, false]);

		or.Evaluate(context).Result.Should().BeFalse(
			"Or must not be constant-true; a real predicate must be applied to child results.");
	}

	[TestMethod]
	public void SingleChild_EqualsChildValue()
	{
		var catalog = new EvaluationCatalog<bool>();
		var p0 = catalog.GetParameter(0);
		var or = catalog.Or([p0]);

		using var lease = Context.Rent();
		var context = lease.Item.Init(catalog, (ReadOnlySpan<bool>)[false]);
		or.Evaluate(context).Result.Should().BeFalse();
	}

	[TestMethod]
	public void EmptyChildren_Throws()
	{
		var catalog = new EvaluationCatalog<bool>();
		Action act = () => catalog.Or([]);
		act.Should().Throw<ArgumentException>();
	}

	[TestMethod]
	public void Description_UsesPaddedPipe()
	{
		var (catalog, p0, p1) = Setup();
		var or = catalog.Or([p0, p1]);
		or.Description.Value.Should().Be("({0} | {1})");
	}

	[TestMethod]
	public void ThreeChildren_OneTrue_IsTrue()
	{
		var catalog = new EvaluationCatalog<bool>();
		IEvaluate<bool>[] ps = [catalog.GetParameter(0), catalog.GetParameter(1), catalog.GetParameter(2)];
		var or = catalog.Or(ps);

		using var lease = Context.Rent();
		var context = lease.Item.Init(catalog, (ReadOnlySpan<bool>)[false, true, false]);
		or.Evaluate(context).Result.Should().BeTrue();
	}

	[TestMethod]
	public void Interning_StructurallyIdentical_ReturnsSameInstance()
	{
		var (catalog, p0, p1) = Setup();
		var a = catalog.Or([p0, p1]);
		var b = catalog.Or([catalog.GetParameter(0), catalog.GetParameter(1)]);

		ReferenceEquals(a, b).Should().BeTrue();
	}
}
