using Open.Evaluation.Arithmetic;

namespace Open.Evaluation.Tests.Arithmetic;

[TestClass]
public class Reduction
{
	[TestMethod]
	public void SumOf_EmptyCollection_ReturnsZeroConstant_WithoutConstructingASumNode()
	{
		using var catalog = new EvaluationCatalog<double>();
		var result = catalog.SumOf([]);

		result.Should().BeOfType<Constant<double>>();
		((Constant<double>)result).Value.Should().Be(0d);
	}

	[TestMethod]
	public void SumOf_SingleChild_ReturnsThatChildDirectly_NotWrappedInSum()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		var result = catalog.SumOf(p0);

		ReferenceEquals(result, p0).Should().BeTrue();
	}

	[TestMethod]
	public void ProductOf_EmptyCollection_Throws()
	{
		using var catalog = new EvaluationCatalog<double>();
		Action act = () => catalog.ProductOf([]);
		act.Should().Throw<NotSupportedException>();
	}

	[TestMethod]
	public void Sum_NaNConstant_PropagatesAsNaN_UnderReduction()
	{
		using var catalog = new EvaluationCatalog<double>();
		var sum = catalog.SumOf(catalog.GetParameter(0), catalog.GetConstant(double.NaN), catalog.GetConstant(5d));
		var reduced = catalog.GetReduced(sum);

		reduced.Should().BeOfType<Constant<double>>();
		double.IsNaN(((Constant<double>)reduced).Value).Should().BeTrue();
	}

	[TestMethod]
	public void Product_NaNConstant_PropagatesAsNaN_UnderEvaluation()
	{
		using var catalog = new EvaluationCatalog<double>();
		var product = catalog.ProductOf(catalog.GetParameter(0), catalog.GetConstant(double.NaN));

		using var lease = Context.Rent();
		var context = lease.Item.Init(catalog, (ReadOnlySpan<double>)[5d]);
		double.IsNaN(product.Evaluate(context).Result).Should().BeTrue();
	}

	[TestMethod]
	public void Product_ZeroConstant_ShortCircuitsEvaluationToZero()
	{
		// Product reorders constants to the front (ConstantPriority = -1), so the zero
		// constant ends up as the first evaluated child regardless of construction order.
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		var product = catalog.ProductOf(p0, catalog.GetConstant(0d));

		using var lease = Context.Rent();
		var context = lease.Item.Init(catalog, (ReadOnlySpan<double>)[5d]);
		product.Evaluate(context).Result.Should().Be(0d);
	}

	[TestMethod]
	public void GetReduced_IsIdempotent()
	{
		using var catalog = new EvaluationCatalog<double>();
		var sum = catalog.SumOf(catalog.GetParameter(0), catalog.GetConstant(2d), catalog.GetConstant(3d));

		var reducedOnce = catalog.GetReduced(sum);
		var reducedTwice = catalog.GetReduced(reducedOnce);

		ReferenceEquals(reducedOnce, reducedTwice).Should().BeTrue();
	}

	[TestMethod]
	public void GetReduced_CachesResultAcrossCalls_ForSameSource()
	{
		using var catalog = new EvaluationCatalog<double>();
		var sum = catalog.SumOf(catalog.GetParameter(0), catalog.GetConstant(2d), catalog.GetConstant(3d));

		var reducedA = catalog.GetReduced(sum);
		var reducedB = catalog.GetReduced(sum);

		ReferenceEquals(reducedA, reducedB).Should().BeTrue("Catalog caches reductions per source via a ConditionalWeakTable");
	}

	[TestMethod]
	public void TryGetReduced_ReturnsFalse_WhenAlreadyFullyReduced()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);

		// A bare parameter cannot be reduced further.
		catalog.TryGetReduced(p0, out var reduction).Should().BeFalse();
		ReferenceEquals(reduction, p0).Should().BeTrue();
	}

	[TestMethod]
	public void TryGetReduced_ReturnsTrue_WhenReductionChangesTheExpression()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);

		// Unlike the multi-constant case, SumOf's constant-folding fast path does not strip a
		// single lone zero constant at construction time (it only *merges* multiple constants
		// together) - so this genuinely requires GetReduction()'s "filter out zero" phase to
		// collapse down to just the parameter itself.
		var sum = catalog.SumOf(p0, catalog.GetConstant(0d));
		sum.Description.Value.Should().Be("({0} + 0)");

		catalog.TryGetReduced(sum, out var reduction).Should().BeTrue();
		ReferenceEquals(reduction, p0).Should().BeTrue();
	}

	[TestMethod]
	public void Exponent_ZeroPower_ReducesToOne_ByDefault()
	{
		using var catalog = new EvaluationCatalog<double>();
		var exp = catalog.GetExponent(catalog.GetParameter(0), catalog.GetConstant(0d));
		var reduced = catalog.GetReduced(exp);

		reduced.Should().BeOfType<Constant<double>>();
		((Constant<double>)reduced).Value.Should().Be(1d);
	}

	[TestMethod]
	public void Exponent_BaseOne_ReducesToOne()
	{
		using var catalog = new EvaluationCatalog<double>();
		var exp = catalog.GetExponent(catalog.GetConstant(1d), catalog.GetParameter(0));
		var reduced = catalog.GetReduced(exp);

		reduced.Should().BeOfType<Constant<double>>();
		((Constant<double>)reduced).Value.Should().Be(1d);
	}

	[TestMethod]
	public void Exponent_ZeroBase_NegativePower_Throws()
	{
		using var catalog = new EvaluationCatalog<double>();
		var exp = catalog.GetExponent(catalog.GetConstant(0d), catalog.GetConstant(-1d));

		Action act = () => catalog.GetReduced(exp);
		act.Should().Throw<InvalidOperationException>();
	}

	[TestMethod]
	public void Sum_FlattensNestedSums_UnderReduction()
	{
		using var catalog = new EvaluationCatalog<double>();
		var inner = catalog.SumOf(catalog.GetParameter(0), catalog.GetParameter(1));

		// SumOf's constant-folding fast path doesn't flatten nested (non-constant) Sum
		// children, so this still produces a raw Sum containing `inner` as a direct child -
		// exercising GetReduction()'s "flatten sums of sums" phase.
		var outer = catalog.SumOf(inner, catalog.GetParameter(2));
		outer.Should().BeOfType<Sum<double>>();
		((Sum<double>)outer).Children.Should().Contain(inner);

		var reduced = catalog.GetReduced(outer);
		reduced.Description.Value.Should().Be("({0} + {1} + {2})");
	}
}
