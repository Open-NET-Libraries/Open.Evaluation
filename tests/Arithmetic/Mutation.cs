using Open.Evaluation.Arithmetic;

namespace Open.Evaluation.Tests.Arithmetic;

/// <summary>
/// Covers EvaluationCatalog{T}.MutationCatalog.ChangeOperation (CatalogExtensions/Mutation.cs).
/// The type check used a single-arg <c>IOperator{T}</c>, but Sum{T}/Product{T}/
/// Exponent{T} (via ArithmeticOperatorBase{T}/OperatorBase{T}) implement only the two-arg
/// <c>IOperator{IEvaluate{T}, T}</c> directly. Implementing a base interface does not grant a
/// more-derived named interface that merely extends it, so the old check was unsatisfiable for
/// every concrete arithmetic operator and ChangeOperation always threw
/// "Does not contain an Operation." Fixed by checking against the two-arg interface directly.
/// </summary>
[TestClass]
public class Mutation
{
	[TestMethod]
	public void ChangeOperation_OnSumNode_DoesNotThrow()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		var p1 = catalog.GetParameter(1);
		var sum = catalog.SumOf(p0, p1);

		var node = catalog.Factory.Map(sum);
		try
		{
			IEvaluate<double>? result = null;
			Action act = () => result = catalog.Mutation.ChangeOperation(node);

			act.Should().NotThrow();
			result.Should().NotBeNull();
		}
		finally
		{
			node.Recycle();
		}
	}

	[TestMethod]
	public void ChangeOperation_OnProductNode_DoesNotThrow()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		var p1 = catalog.GetParameter(1);
		var product = catalog.ProductOf(p0, p1);

		var node = catalog.Factory.Map(product);
		try
		{
			IEvaluate<double>? result = null;
			Action act = () => result = catalog.Mutation.ChangeOperation(node);

			act.Should().NotThrow();
			result.Should().NotBeNull();
		}
		finally
		{
			node.Recycle();
		}
	}

	[TestMethod]
	public void ChangeOperation_OnExponentNode_DoesNotThrow()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		var exponent = catalog.GetExponent(p0, catalog.GetConstant(2d));

		var node = catalog.Factory.Map(exponent);
		try
		{
			IEvaluate<double>? result = null;
			Action act = () => result = catalog.Mutation.ChangeOperation(node);

			act.Should().NotThrow();
			result.Should().NotBeNull();
		}
		finally
		{
			node.Recycle();
		}
	}

	[TestMethod]
	public void ChangeOperation_OnSumNode_ProducesProduct()
	{
		// Registry.Operators = [Sum, Product]; excluding the node's own symbol ('+') from a
		// 2-element set deterministically leaves Product.
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		var p1 = catalog.GetParameter(1);
		var sum = catalog.SumOf(p0, p1);

		var node = catalog.Factory.Map(sum);
		try
		{
			var result = catalog.Mutation.ChangeOperation(node);
			result.Should().BeOfType<Product<double>>();
		}
		finally
		{
			node.Recycle();
		}
	}

	[TestMethod]
	public void ChangeOperation_OnProductNode_ProducesSum()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		var p1 = catalog.GetParameter(1);
		var product = catalog.ProductOf(p0, p1);

		var node = catalog.Factory.Map(product);
		try
		{
			var result = catalog.Mutation.ChangeOperation(node);
			result.Should().BeOfType<Sum<double>>();
		}
		finally
		{
			node.Recycle();
		}
	}
}
