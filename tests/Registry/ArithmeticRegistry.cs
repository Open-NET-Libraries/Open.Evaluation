using Open.Evaluation.Arithmetic;

namespace Open.Evaluation.Tests.Registry;

[TestClass]
public class ArithmeticRegistry
{
	static (EvaluationCatalog<double> Catalog, IEvaluate<double>[] Ps) Setup()
	{
		var catalog = new EvaluationCatalog<double>();
		return (catalog, [catalog.GetParameter(0), catalog.GetParameter(1)]);
	}

	[TestMethod]
	public void GetOperator_Sum_ProducesSum()
	{
		var (catalog, ps) = Setup();
		var op = Open.Evaluation.Arithmetic.Registry.GetOperator(catalog, Glyphs.Sum, ps);
		op.Should().BeOfType<Sum<double>>();

		using var lease = Context.Rent();
		var context = lease.Item.Init(catalog, (ReadOnlySpan<double>)[2, 3]);
		op.Evaluate(context).Result.Should().Be(5);
	}

	[TestMethod]
	public void GetOperator_Product_ProducesProduct()
	{
		var (catalog, ps) = Setup();
		var op = Open.Evaluation.Arithmetic.Registry.GetOperator(catalog, Glyphs.Product, ps);
		op.Should().BeOfType<Product<double>>();

		using var lease = Context.Rent();
		var context = lease.Item.Init(catalog, (ReadOnlySpan<double>)[2, 3]);
		op.Evaluate(context).Result.Should().Be(6);
	}

	[TestMethod]
	public void GetOperator_InvalidGlyph_Throws()
	{
		var (catalog, ps) = Setup();
		Action act = () => Open.Evaluation.Arithmetic.Registry.GetOperator(catalog, '%', ps);
		act.Should().Throw<ArgumentException>();
	}

	[TestMethod]
	public void Operators_ContainsSumAndProduct_Only()
	{
		Open.Evaluation.Arithmetic.Registry.Operators.Should().BeEquivalentTo([Glyphs.Sum, Glyphs.Product]);
	}

	[TestMethod]
	public void Functions_ContainsSquareInvertSquareRoot()
	{
		Open.Evaluation.Arithmetic.Registry.Functions.Should()
			.BeEquivalentTo([Glyphs.Square, Glyphs.Invert, Glyphs.SquareRoot]);
	}

	[TestMethod]
	public void GetFunction_Square_ProducesExponentOfTwo()
	{
		var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		var fn = Open.Evaluation.Arithmetic.Registry.GetFunction(catalog, Glyphs.Square, p0);

		using var lease = Context.Rent();
		var context = lease.Item.Init(catalog, (ReadOnlySpan<double>)[5]);
		fn.Evaluate(context).Result.Should().Be(25);
	}

	[TestMethod]
	public void GetFunction_Invert_ProducesNegativeOneExponent()
	{
		var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		var fn = Open.Evaluation.Arithmetic.Registry.GetFunction(catalog, Glyphs.Invert, p0);

		using var lease = Context.Rent();
		var context = lease.Item.Init(catalog, (ReadOnlySpan<double>)[4]);
		fn.Evaluate(context).Result.Should().Be(0.25);
	}

	[TestMethod]
	public void GetFunction_SquareRoot_SingleChild_Throws()
	{
		// GetFunction (non-float overload) cannot produce a square root; only GetFloatFunction can.
		var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		Action act = () => Open.Evaluation.Arithmetic.Registry.GetFunction(catalog, Glyphs.SquareRoot, p0);
		act.Should().Throw<NotSupportedException>();
	}

	[TestMethod]
	public void GetFloatFunction_SquareRoot_ProducesHalfExponent()
	{
		var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		var fn = Open.Evaluation.Arithmetic.Registry.GetFloatFunction(catalog, Glyphs.SquareRoot, p0);

		using var lease = Context.Rent();
		var context = lease.Item.Init(catalog, (ReadOnlySpan<double>)[9]);
		fn.Evaluate(context).Result.Should().Be(3);
	}

	[TestMethod]
	public void GetFunction_TwoChildren_Exponent_ProducesExponent()
	{
		var (catalog, ps) = Setup();
		var fn = Open.Evaluation.Arithmetic.Registry.GetFunction(catalog, Glyphs.Exponent, ps);
		fn.Should().BeOfType<Exponent<double>>();

		using var lease = Context.Rent();
		var context = lease.Item.Init(catalog, (ReadOnlySpan<double>)[2, 3]);
		fn.Evaluate(context).Result.Should().Be(8);
	}

	[TestMethod]
	public void GetFunction_TwoChildren_NonExponentGlyph_Throws()
	{
		var (catalog, ps) = Setup();
		Action act = () => Open.Evaluation.Arithmetic.Registry.GetFunction(catalog, Glyphs.Square, ps);
		act.Should().Throw<ArgumentException>();
	}

	[TestMethod]
	public void GetFunction_ExponentWithWrongChildCount_Throws()
	{
		var catalog = new EvaluationCatalog<double>();
		IEvaluate<double>[] three = [catalog.GetParameter(0), catalog.GetParameter(1), catalog.GetParameter(2)];
		Action act = () => Open.Evaluation.Arithmetic.Registry.GetFunction(catalog, Glyphs.Exponent, three);
		act.Should().Throw<ArgumentException>();
	}

	[TestMethod]
	public void GetFunction_SingleChild_Exponent_Throws()
	{
		var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		Action act = () => Open.Evaluation.Arithmetic.Registry.GetFunction(catalog, Glyphs.Exponent, p0);
		act.Should().Throw<ArgumentException>();
	}

	[TestMethod]
	public void GetRandomOperator_AlwaysProducesRegisteredOperator()
	{
		var (catalog, ps) = Setup();
		for (var i = 0; i < 20; i++)
		{
			var op = Open.Evaluation.Arithmetic.Registry.GetRandomOperator(catalog, ps);
			op.Should().NotBeNull();
			(op is Sum<double> or Product<double>).Should().BeTrue();
		}
	}

	[TestMethod]
	public void GetRandomOperator_ExceptSum_AlwaysProducesProduct()
	{
		var (catalog, ps) = Setup();
		for (var i = 0; i < 20; i++)
		{
			var op = Open.Evaluation.Arithmetic.Registry.GetRandomOperator(catalog, ps, Glyphs.Sum);
			op.Should().BeOfType<Product<double>>();
		}
	}

	// Issue #16 (author-approved intent flip): GetRandomFunction previously dispatched every
	// draw through the non-float GetFunction, which always throws NotSupportedException for
	// SquareRoot regardless of T -- so SquareRoot could never actually be produced by
	// GetRandomFunction for ANY T, including float-capable ones like double. The fix computes
	// the half-exponent directly for float-capable T instead of delegating to the throwing
	// GetFunction, so this now asserts SUCCESS: excluding Square and Invert from a 3-glyph set
	// deterministically lands on SquareRoot, which for double now produces Exponent(x, 0.5)
	// rather than throwing.
	[TestMethod]
	public void GetRandomFunction_ExceptSquareAndInvert_AlwaysProducesSquareRoot()
	{
		var catalog = new EvaluationCatalog<double>();
		IEvaluate<double>[] one = [catalog.GetParameter(0)];
		for (var i = 0; i < 20; i++)
		{
			IEvaluate<double>? fn = null;
			Action act = () => fn = Open.Evaluation.Arithmetic.Registry.GetRandomFunction(catalog, one, Glyphs.Square, Glyphs.Invert);

			act.Should().NotThrow();
			var exponent = fn.Should().BeOfType<Exponent<double>>().Which;
			ReferenceEquals(exponent.Power, catalog.GetConstant(0.5d)).Should().BeTrue();
		}
	}

	// Companion coverage for the same fix: for a non-floating-point T, SquareRoot must never be
	// drawn at all (its power, T.One/(T.One+T.One), would truncate to zero under integer
	// division), so only Square (power 2) or Invert (power -1) should ever be produced.
	[TestMethod]
	public void GetRandomFunction_IntType_NeverDrawsSquareRoot_NoTruncationToZeroPower()
	{
		var catalog = new EvaluationCatalog<int>();
		IEvaluate<int>[] one = [catalog.GetParameter(0)];
		for (var i = 0; i < 50; i++)
		{
			var fn = Open.Evaluation.Arithmetic.Registry.GetRandomFunction(catalog, one);

			var power = fn.Should().BeOfType<Exponent<int>>().Which.Power;
			power.Should().BeOfType<Constant<int>>();
			var value = ((Constant<int>)power).Value;
			(value == 2 || value == -1).Should().BeTrue(
				"Square (power 2) or Invert (power -1) are the only int-safe function powers; " +
				"SquareRoot (which would truncate 1/2 to 0) must never be drawn.");
		}
	}

	[TestMethod]
	public void GetRandomFunction_SingleChildOverload_RespectsExceptList()
	{
		var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		for (var i = 0; i < 20; i++)
		{
			var fn = Open.Evaluation.Arithmetic.Registry.GetRandomFunction(catalog, p0, Glyphs.Invert, Glyphs.SquareRoot);
			fn.Should().BeOfType<Exponent<double>>();
			ReferenceEquals(((Exponent<double>)fn).Power, catalog.GetConstant(2d)).Should().BeTrue();
		}
	}

	// The two exhaustion contracts are ASYMMETRIC by design (nullable vs non-nullable
	// return): the children+except overload signals "nothing left to draw" with null,
	// while the single-child overload's non-nullable return has no way to signal absence
	// and throws descriptively instead. Both contracts pinned so neither drifts silently.
	[TestMethod]
	public void GetRandomFunction_MultiChildOverload_ExhaustedExclusions_ReturnsNull()
	{
		var catalog = new EvaluationCatalog<double>();
		IEvaluate<double>[] one = [catalog.GetParameter(0)];

		var fn = Open.Evaluation.Arithmetic.Registry.GetRandomFunction(
			catalog, one, Glyphs.Square, Glyphs.Invert, Glyphs.SquareRoot);

		fn.Should().BeNull("excluding every available function glyph leaves nothing to draw");
	}

	[TestMethod]
	public void GetRandomFunction_SingleChildOverload_ExhaustedExclusions_ThrowsDescriptively()
	{
		var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);

		Action act = () => Open.Evaluation.Arithmetic.Registry.GetRandomFunction(
			catalog, p0, Glyphs.Square, Glyphs.Invert, Glyphs.SquareRoot);

		act.Should().Throw<InvalidOperationException>()
			.WithMessage("*eliminates every available function*");
	}
}
