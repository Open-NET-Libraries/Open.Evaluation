using Open.Evaluation.Arithmetic;

namespace Open.Evaluation.Tests.Arithmetic;

/// <summary>
/// Covers the deterministic node-multiple helpers in
/// Open.Evaluation.Arithmetic.CatalogExtensions (Multiply.cs) used by the mutation/variation
/// machinery: MultiplyNode, GetMultiple, and AdjustNodeMultiple.
/// </summary>
[TestClass]
public class Multiply
{
	[TestMethod]
	public void MultiplyNode_WithIdentityMultiple_ReturnsUnchanged()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		var node = catalog.Factory.Map(p0);
		try
		{
			var result = catalog.MultiplyNode(node, 1d);
			ReferenceEquals(result, p0).Should().BeTrue();
		}
		finally
		{
			node.Recycle();
		}
	}

	[TestMethod]
	public void MultiplyNode_WithZeroMultiple_ReturnsZeroConstant()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		var node = catalog.Factory.Map(p0);
		try
		{
			var result = catalog.MultiplyNode(node, 0d);
			result.Should().BeOfType<Constant<double>>();
			((Constant<double>)result).Value.Should().Be(0d);
		}
		finally
		{
			node.Recycle();
		}
	}

	[TestMethod]
	public void MultiplyNode_OnNonProduct_WrapsInProductWithMultiple()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		var node = catalog.Factory.Map(p0);
		try
		{
			var result = catalog.MultiplyNode(node, 3d);
			result.Description.Value.Should().Be("(3 * {0})");
		}
		finally
		{
			node.Recycle();
		}
	}

	[TestMethod]
	public void MultiplyNode_OnProductWithExistingConstant_MultipliesThatConstant()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		var product = catalog.ProductOf(catalog.GetConstant(2d), p0);
		product.Description.Value.Should().Be("(2 * {0})");

		var node = catalog.Factory.Map(product);
		try
		{
			var result = catalog.MultiplyNode(node, 3d);
			result.Description.Value.Should().Be("(6 * {0})");
		}
		finally
		{
			node.Recycle();
		}
	}

	[TestMethod]
	public void MultiplyNode_OnProductWithoutConstant_AddsOneAsNewConstantChild()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		var p1 = catalog.GetParameter(1);
		var product = catalog.ProductOf(p0, p1);
		product.Description.Value.Should().Be("({0} * {1})");

		var node = catalog.Factory.Map(product);
		try
		{
			var result = catalog.MultiplyNode(node, 5d);
			// Product sorts constants to the front (ConstantPriority = -1).
			result.Description.Value.Should().Be("(5 * {0} * {1})");
		}
		finally
		{
			node.Recycle();
		}
	}

	[TestMethod]
	public void GetMultiple_OnProductWithConstant_ReturnsThatConstant()
	{
		using var catalog = new EvaluationCatalog<double>();
		var product = catalog.ProductOf(catalog.GetConstant(4d), catalog.GetParameter(0));

		var multiple = catalog.GetMultiple(product);
		multiple.Value.Should().Be(4d);
	}

	[TestMethod]
	public void GetMultiple_OnNonParentNode_ReturnsMultiplicativeIdentity()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);

		var multiple = catalog.GetMultiple(p0);
		multiple.Value.Should().Be(1d);
	}

	[TestMethod]
	public void GetMultiple_OnProductWithoutConstant_ReturnsMultiplicativeIdentity()
	{
		using var catalog = new EvaluationCatalog<double>();
		var product = catalog.ProductOf(catalog.GetParameter(0), catalog.GetParameter(1));

		var multiple = catalog.GetMultiple(product);
		multiple.Value.Should().Be(1d);
	}

	[TestMethod]
	public void AdjustNodeMultiple_WithZeroDelta_ReturnsUnchanged()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		var node = catalog.Factory.Map(p0);
		try
		{
			var result = catalog.AdjustNodeMultiple(node, 0d);
			ReferenceEquals(result, p0).Should().BeTrue();
		}
		finally
		{
			node.Recycle();
		}
	}

	[TestMethod]
	public void AdjustNodeMultiple_OnProductWithExistingMultiple_AddsDeltaToConstant()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		var product = catalog.ProductOf(catalog.GetConstant(4d), p0);

		var node = catalog.Factory.Map(product);
		try
		{
			var result = catalog.AdjustNodeMultiple(node, 3d);
			result.Description.Value.Should().Be("(7 * {0})");
		}
		finally
		{
			node.Recycle();
		}
	}

	[TestMethod]
	public void AdjustNodeMultiple_OnNonProduct_BehavesLikeMultiplyByDeltaPlusOne()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		var node = catalog.Factory.Map(p0);
		try
		{
			// delta=2 on a bare parameter (not a Product) => MultiplyNode(delta + 1 = 3).
			var result = catalog.AdjustNodeMultiple(node, 2d);
			result.Description.Value.Should().Be("(3 * {0})");
		}
		finally
		{
			node.Recycle();
		}
	}
}
