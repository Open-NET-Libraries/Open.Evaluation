using Open.Evaluation.Boolean;

namespace Open.Evaluation.Tests.Boolean;

[TestClass]
public class ComparisonReductions
{
	static (EvaluationCatalog<double> doubles, EvaluationCatalog<bool> bools) Setup()
		=> (new EvaluationCatalog<double>(), new EvaluationCatalog<bool>());

	[TestMethod]
	public void GreaterThan_FoldsConstants()
	{
		var (doubles, bools) = Setup();

		bools.GetReduced(bools.GreaterThan((doubles.GetConstant(5d), doubles.GetConstant(3d))))
			.Description.Value.Should().Be("True");

		bools.GetReduced(bools.GreaterThan((doubles.GetConstant(3d), doubles.GetConstant(5d))))
			.Description.Value.Should().Be("False");
	}

	[TestMethod]
	public void LessThan_FoldsConstants()
	{
		var (doubles, bools) = Setup();

		bools.GetReduced(bools.LessThan((doubles.GetConstant(3d), doubles.GetConstant(5d))))
			.Description.Value.Should().Be("True");

		bools.GetReduced(bools.LessThan((doubles.GetConstant(5d), doubles.GetConstant(3d))))
			.Description.Value.Should().Be("False");
	}

	[TestMethod]
	public void GreaterThan_IdenticalOperands_ReducesToFalse()
	{
		var (doubles, bools) = Setup();
		var a = doubles.GetParameter(0);

		bools.GetReduced(bools.GreaterThan((a, a))).Description.Value.Should().Be("False");
	}

	[TestMethod]
	public void LessThan_IdenticalOperands_ReducesToFalse()
	{
		var (doubles, bools) = Setup();
		var a = doubles.GetParameter(0);

		bools.GetReduced(bools.LessThan((a, a))).Description.Value.Should().Be("False");
	}

	[TestMethod]
	public void LessThan_CanonicalizesToGreaterThanWithSwappedOperands()
	{
		var (doubles, bools) = Setup();
		var a = doubles.GetParameter(0);
		var b = doubles.GetParameter(1);

		var reduced = bools.GetReduced(bools.LessThan((a, b)));

		reduced.Should().BeOfType<GreaterThan<double>>();
		reduced.Description.Value.Should().Be("({1} > {0})");
	}

	[TestMethod]
	public void GreaterThan_IsAReductionFixpoint()
	{
		var (doubles, bools) = Setup();
		var a = doubles.GetParameter(0);
		var b = doubles.GetParameter(1);
		var greaterThan = bools.GreaterThan((b, a));

		bools.TryGetReduced(greaterThan, out var reduction).Should().BeFalse();
		ReferenceEquals(reduction, greaterThan).Should().BeTrue();
	}
}
