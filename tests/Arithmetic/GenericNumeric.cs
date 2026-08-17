using Open.Evaluation.Arithmetic;

namespace Open.Evaluation.Tests.Arithmetic;

/// <summary>
/// Verifies the arithmetic node types work generically across numeric types beyond double,
/// since they're built against <see cref="System.Numerics.INumber{TSelf}"/>.
/// </summary>
[TestClass]
public class GenericNumeric
{
	[TestMethod]
	public void Sum_Int_EvaluatesCorrectly()
	{
		using var catalog = new EvaluationCatalog<int>();
		var sum = catalog.SumOf(catalog.GetParameter(0), catalog.GetParameter(1), catalog.GetConstant(10));

		using var lease = Context.Rent();
		var context = lease.Item.Init(catalog, (ReadOnlySpan<int>)[2, 3]);
		sum.Evaluate(context).Result.Should().Be(15);
	}

	[TestMethod]
	public void Product_Int_EvaluatesCorrectly()
	{
		using var catalog = new EvaluationCatalog<int>();
		var product = catalog.ProductOf(catalog.GetParameter(0), catalog.GetParameter(1), catalog.GetConstant(10));

		using var lease = Context.Rent();
		var context = lease.Item.Init(catalog, (ReadOnlySpan<int>)[2, 3]);
		product.Evaluate(context).Result.Should().Be(60);
	}

	[TestMethod]
	public void Product_Int_ZeroChild_ShortCircuitsToZero()
	{
		using var catalog = new EvaluationCatalog<int>();
		var product = catalog.ProductOf(catalog.GetParameter(0), catalog.GetConstant(0));

		using var lease = Context.Rent();
		var context = lease.Item.Init(catalog, (ReadOnlySpan<int>)[7]);
		product.Evaluate(context).Result.Should().Be(0);
	}

	[TestMethod]
	public void Sum_Int_ConstantsCollapseUnderReduction()
	{
		using var catalog = new EvaluationCatalog<int>();
		var sum = catalog.SumOf(catalog.GetParameter(0), catalog.GetConstant(2), catalog.GetConstant(3));
		var reduced = catalog.GetReduced(sum);

		reduced.Description.Value.Should().Be("({0} + 5)");

		using var lease = Context.Rent();
		var context = lease.Item.Init(catalog, (ReadOnlySpan<int>)[10]);
		reduced.Evaluate(context).Result.Should().Be(15);
	}

	[TestMethod]
	public void Exponent_Int_PositivePower_EvaluatesViaRepeatedMultiplication()
	{
		using var catalog = new EvaluationCatalog<int>();
		var exp = catalog.GetExponent(catalog.GetParameter(0), catalog.GetConstant(3));

		using var lease = Context.Rent();
		var context = lease.Item.Init(catalog, (ReadOnlySpan<int>)[2]);
		exp.Evaluate(context).Result.Should().Be(8);
	}

	/// <summary>
	/// Documents actual (not necessarily "buggy") generic-numeric behavior: for an integer
	/// type, a negative integer exponent is evaluated through repeated integer division, which
	/// truncates. 2^-1 evaluates to 1/2 truncated to 0, rather than a fractional result (int
	/// cannot represent 0.5).
	/// </summary>
	[TestMethod]
	public void Exponent_Int_NegativePower_EvaluatesWithIntegerTruncation()
	{
		using var catalog = new EvaluationCatalog<int>();
		var exp = catalog.GetExponent(catalog.GetConstant(2), catalog.GetConstant(-1));

		using var lease = Context.Rent();
		var context = lease.Item;
		exp.Evaluate(context).Result.Should().Be(0);
	}

	/// <summary>
	/// Documents actual generic-numeric behavior in <c>Exponent&lt;T&gt;.GetReduction()</c>:
	/// for a non-floating-point type, a negative constant power is left unreduced (the
	/// algebraic inverse can't be represented), even though the rendered *description* still
	/// uses the "1/x" notation purely as a string-formatting convention.
	/// </summary>
	[TestMethod]
	public void Exponent_Int_NegativePower_IsNotAlgebraicallyReduced()
	{
		using var catalog = new EvaluationCatalog<int>();
		var exp = catalog.GetExponent(catalog.GetConstant(2), catalog.GetConstant(-1));

		var reduced = catalog.GetReduced(exp);
		ReferenceEquals(reduced, exp).Should().BeTrue("integer types can't represent the algebraic inverse, so reduction is a no-op");

		// The description still uses the "1/x" formatting convention regardless of whether
		// algebraic reduction actually took place.
		exp.Description.Value.Should().Be("(1/2)");
	}

	[TestMethod]
	public void Exponent_Double_NegativePower_IsAlgebraicallyReduced()
	{
		using var catalog = new EvaluationCatalog<double>();
		var exp = catalog.GetExponent(catalog.GetConstant(2d), catalog.GetConstant(-1d));

		var reduced = catalog.GetReduced(exp);
		reduced.Should().BeOfType<Constant<double>>();
		((Constant<double>)reduced).Value.Should().Be(0.5);
	}

	[TestMethod]
	public void Interning_IsPerCatalogInstance_NotSharedAcrossCatalogs()
	{
		// Each catalog maintains its own registry; the same logical value in two different
		// catalogs of the same T must NOT be the same instance.
		var c1 = new EvaluationCatalog<int>();
		var c2 = new EvaluationCatalog<int>();

		var a = c1.GetConstant(5);
		var b = c2.GetConstant(5);

		ReferenceEquals(a, b).Should().BeFalse();
		a.Value.Should().Be(b.Value);
	}
}
