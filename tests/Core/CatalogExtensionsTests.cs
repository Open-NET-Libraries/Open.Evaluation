using Open.Evaluation.Arithmetic;

namespace Open.Evaluation.Tests.Core;

[TestClass]
public class CatalogExtensionsTests
{
	[TestMethod]
	public void AssertBelongs_AllFromSameCatalog_DoesNotThrow()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		var p1 = catalog.GetParameter(1);

		Action act = () => catalog.AssertBelongs(p0, p1);
		act.Should().NotThrow();
	}

	[TestMethod]
	public void AssertBelongs_EvaluationFromDifferentCatalog_Throws()
	{
		using var catalog = new EvaluationCatalog<double>();
		using var otherCatalog = new EvaluationCatalog<double>();
		var foreign = otherCatalog.GetParameter(0);

		Action act = () => catalog.AssertBelongs(foreign);
		act.Should().Throw<ArgumentException>();
	}

	[TestMethod]
	public void TryAddConstant_OnOperatorNode_AddsChildAndReturnsNewExpression()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		var p1 = catalog.GetParameter(1);
		var sum = catalog.SumOf(p0, p1);

		var node = catalog.Factory.Map(sum);
		try
		{
			var result = catalog.TryAddConstant(node, 5d);

			result.Should().NotBeNull();
			result!.Description.Value.Should().Be("({0} + {1} + 5)");

			using var lease = Context.Rent();
			var context = lease.Item.Init(catalog, (ReadOnlySpan<double>)[2d, 3d]);
			result.Evaluate(context).Result.Should().Be(10d);
		}
		finally
		{
			node.Recycle();
		}
	}

	[TestMethod]
	public void TryAddConstant_OnLeafParameterNode_ReturnsNull()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);

		var node = catalog.Factory.Map(p0);
		try
		{
			var result = catalog.TryAddConstant(node, 5d);
			result.Should().BeNull("a bare parameter has no children to add a constant to");
		}
		finally
		{
			node.Recycle();
		}
	}

	[TestMethod]
	public void RemoveNode_RemovesChildAndFixesHierarchy()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		var p1 = catalog.GetParameter(1);
		var p2 = catalog.GetParameter(2);
		var sum = catalog.SumOf(p0, p1, p2);

		var node = catalog.Factory.Map(sum);
		try
		{
			var middleChild = node.Children[1];
			var fixedRoot = catalog.RemoveNode(middleChild);
			try
			{
				fixedRoot.Value!.Description.Value.Should().Be("({0} + {2})");
			}
			finally
			{
				if (fixedRoot != node) fixedRoot.Recycle();
			}
		}
		finally
		{
			node.Recycle();
		}
	}

	[TestMethod]
	public void RemoveNode_WithoutParent_Throws()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		var node = catalog.Factory.Map(p0);
		try
		{
			Action act = () => catalog.RemoveNode(node);
			act.Should().Throw<ArgumentException>();
		}
		finally
		{
			node.Recycle();
		}
	}
}
