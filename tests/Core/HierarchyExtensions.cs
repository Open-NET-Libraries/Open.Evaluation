using Open.Evaluation.Arithmetic;
using Open.Evaluation.Hierarchy;

namespace Open.Evaluation.Tests.Core;

[TestClass]
public class HierarchyExtensionsTests
{
	[TestMethod]
	public void CountDistinctParameters_CountsUniqueIds_IgnoringDuplicateReferences()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		var expr = catalog.SumOf(p0, catalog.GetParameter(1), p0, catalog.GetConstant(5d));

		var node = catalog.Factory.Map(expr);
		try
		{
			node.CountDistinctParameters().Should().Be(2);
		}
		finally
		{
			node.Recycle();
		}
	}

	[TestMethod]
	public void CountDistinctDescendantValuesOfType_WithSelector_CountsDistinctProjectedValues()
	{
		using var catalog = new EvaluationCatalog<double>();

		// SumOf folds multiple *sibling* constants together at construction time, so to keep
		// 2 and 3 as genuinely distinct descendant constant nodes, nest one inside a Product -
		// only top-level sibling constants get merged, not ones buried in a child operator.
		var expr = catalog.SumOf(
			catalog.GetConstant(2d),
			catalog.ProductOf(catalog.GetConstant(3d), catalog.GetParameter(0)));

		var node = catalog.Factory.Map(expr);
		try
		{
			node.CountDistinctDescendantValuesOfType<IEvaluate<double>, IConstant<double>, double>(c => c.Value)
				.Should().Be(2);
		}
		finally
		{
			node.Recycle();
		}
	}

	[TestMethod]
	public void CountDistinctDescendantValuesOfType_WithoutSelector_CountsDistinctInstances()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		var expr = catalog.SumOf(p0, catalog.GetParameter(1), p0);

		var node = catalog.Factory.Map(expr);
		try
		{
			// p0 appears twice by reference, but as an interned/distinct *value* it should
			// only count once alongside the one occurrence of parameter 1.
			node.CountDistinctDescendantValuesOfType<IEvaluate<double>, IParameter<double>>().Should().Be(2);
		}
		finally
		{
			node.Recycle();
		}
	}

	[TestMethod]
	public void CountDistinctParameters_NoParameters_IsZero()
	{
		using var catalog = new EvaluationCatalog<double>();
		var expr = catalog.SumOf(catalog.GetConstant(2d), catalog.GetConstant(3d));

		var node = catalog.Factory.Map(expr);
		try
		{
			node.CountDistinctParameters().Should().Be(0);
		}
		finally
		{
			node.Recycle();
		}
	}
}
