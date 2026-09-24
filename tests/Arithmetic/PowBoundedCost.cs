using System.Numerics;
using System.Reflection;
using Open.Evaluation.Arithmetic;

namespace Open.Evaluation.Tests.Arithmetic;

/// <summary>
/// Covers the <c>SequentialLoopCap</c> bail-out added to <c>Exponent.Pow&lt;T&gt;</c> for
/// T : IFloatingPoint&lt;T&gt;: above the cap, an integer-valued exponent switches from the
/// sequential multiplication/division loop to the existing Math.Pow path, bounding what used to
/// be an unbounded-cost loop (repeated Square/AdjustExponent mutations can compound an exponent
/// into the hundreds of trillions). See Open.Evaluation.Arithmetic/Exponent.cs,
/// <c>FloatingPointCap&lt;T&gt;</c> and <c>SequentialLoopCap</c>.
///
/// <c>Pow&lt;T&gt;</c> is invoked directly via reflection (it is `internal`, and the test
/// assembly has no InternalsVisibleTo) -- same technique as
/// tests/Arithmetic/PowSquaringEquivalence.cs, for the same reason: it targets exactly the
/// changed method and sidesteps the unrelated catalog/reduction machinery entirely, except for
/// the one reduction-path test below, which deliberately goes through the public catalog API
/// instead (that is the path a mutation-compounded exponent actually reaches in practice).
/// </summary>
[TestClass]
public class PowBoundedCost
{
	static readonly MethodInfo PowMethodDefinition =
		typeof(Exponent).GetMethod("Pow", BindingFlags.NonPublic | BindingFlags.Static)
		?? throw new InvalidOperationException("Exponent.Pow<T>(this T, T) extension method not found via reflection; test needs updating.");

	static T InvokeProductionPow<T>(T baseValue, T exponent)
		where T : notnull, INumber<T>
	{
		var closed = PowMethodDefinition.MakeGenericMethod(typeof(T));
		object result = closed.Invoke(null, [baseValue, exponent])
			?? throw new InvalidOperationException("Exponent.Pow<T> returned null via reflection invocation.");
		return (T)result;
	}

	// Mirrors the PRODUCTION positive-exponent loop exactly (Exponent.cs's final `else`
	// branch): starts at baseValue itself -- the first factor is already applied -- and
	// multiplies exponent-1 MORE times, left-associated in that exact order. Floating-point
	// multiplication is not associative under rounding, so a different grouping (computing
	// `exponent` multiplications instead of exponent-1, or repeated squaring) can disagree in
	// the last bit; this has to replicate the loop's exact shape to be a meaningful oracle.
	static double ReferenceSequentialMultiply(double baseValue, int exponent)
	{
		double result = baseValue;
		for (int i = 1; i < exponent; i++)
			result *= baseValue;
		return result;
	}

	// Mirrors the PRODUCTION negative-exponent loop exactly (Exponent.cs's first branch):
	// starts at ONE -- not baseValue, and not 1/baseValue -- and divides by baseValue
	// `magnitude` times, left-associated in that exact order. This is deliberately NOT the same
	// sequence of roundings as "compute the positive power, then take one reciprocal": it is
	// `magnitude` separate divisions, each dividing the previous (already-rounded) quotient by
	// baseValue again. That shape is asymmetric with the positive-exponent loop above (which
	// starts at baseValue and does exponent-1 multiplications) -- the asymmetry is deliberate in
	// production, not a test artifact, so the two reference helpers here deliberately do not
	// share an implementation.
	static double ReferenceSequentialDivide(double baseValue, int magnitude)
	{
		double result = 1d;
		for (int i = 1; i <= magnitude; i++)
			result /= baseValue;
		return result;
	}

	[TestMethod]
	public void Double_AtCap_PositiveExponent_MatchesSequentialLoopBitForBit()
	{
		// At the cap itself (64), production still takes the sequential-multiplication loop,
		// so the result must be bit-identical to it -- NOT merely close, and not necessarily
		// equal to Math.Pow (which already disagrees with the sequential loop by exponent 4).
		double actual = InvokeProductionPow(1.1, 64d);
		double reference = ReferenceSequentialMultiply(1.1, 64);
		BitConverter.DoubleToInt64Bits(actual).Should().Be(BitConverter.DoubleToInt64Bits(reference));
	}

	[TestMethod]
	public void Double_JustPastCap_PositiveExponent_MatchesMathPow()
	{
		// One past the cap (65), production switches to the Math.Pow bail-out.
		double actual = InvokeProductionPow(1.1, 65d);
		BitConverter.DoubleToInt64Bits(actual).Should().Be(BitConverter.DoubleToInt64Bits(Math.Pow(1.1, 65)));
	}

	[TestMethod]
	public void Double_AtCap_NegativeExponent_MatchesSequentialLoopBitForBit()
	{
		double actual = InvokeProductionPow(1.1, -64d);
		double reference = ReferenceSequentialDivide(1.1, 64);
		BitConverter.DoubleToInt64Bits(actual).Should().Be(BitConverter.DoubleToInt64Bits(reference));
	}

	[TestMethod]
	public void Double_JustPastCap_NegativeExponent_MatchesMathPow()
	{
		// Math.Pow is given the ORIGINAL negative exponent (-65), not the magnitude (65): it
		// takes its own reciprocal path internally, which production must reproduce by passing
		// the untouched `exponent` parameter, not `-magnitude` or any recomputed value.
		double actual = InvokeProductionPow(1.1, -65d);
		BitConverter.DoubleToInt64Bits(actual).Should().Be(BitConverter.DoubleToInt64Bits(Math.Pow(1.1, -65)));
	}

	// Runs the (possibly still-unbounded, pre-fix) Pow call on a background task with a 5s
	// timeout, so a hang shows up as a failed assertion instead of hanging the whole test run --
	// pre-fix this spins for about 44s at exponent 1e10 before the background task itself
	// completes (Task.Wait returning false does not cancel it).
	static void AssertFastAndMatchesMathPow(double baseValue, double exponent)
	{
		double actual = default;
		var spinner = Task.Run(() => actual = InvokeProductionPow(baseValue, exponent));
		bool completedInTime = spinner.Wait(TimeSpan.FromSeconds(5));
		completedInTime.Should().BeTrue(
			$"Pow({baseValue}, {exponent}) must return within 5s via the Math.Pow bail-out -- " +
			"pre-fix it ran the uncapped sequential loop instead (about 44s at this magnitude)");
		actual.Should().Be(Math.Pow(baseValue, exponent), $"{baseValue}^{exponent}");
	}

	[TestMethod]
	public void LargeExponent_PositiveBaseNearOne_ReturnsWithinTimeoutAndMatchesMathPow()
		=> AssertFastAndMatchesMathPow(1.0000000001, 1e10);

	[TestMethod]
	public void LargeExponent_IntegerValuedDoubleBase_ReturnsWithinTimeoutAndMatchesMathPow()
		=> AssertFastAndMatchesMathPow(2d, 1e10);

	[TestMethod]
	public void LargeExponent_FractionalBase_ReturnsWithinTimeoutAndMatchesMathPow()
		=> AssertFastAndMatchesMathPow(0.5d, 1e10);

	[TestMethod]
	public void LargeExponent_NegativeBase_ReturnsWithinTimeoutAndMatchesMathPow()
		=> AssertFastAndMatchesMathPow(-1d, 1e10);

	[TestMethod]
	public void LargeNegativeExponent_ReturnsWithinTimeoutAndMatchesMathPow()
		=> AssertFastAndMatchesMathPow(2d, -1e10);

	[TestMethod]
	public void Reduction_ConstantBaseToLargeConstantPower_ReducesWithinTimeoutAndMatchesMathPow()
	{
		// Exercises the OTHER caller of Pow<T> -- ReduceWherePowIsConstant, reached through the
		// public catalog API rather than reflection -- since that is the path an evolved
		// expression actually goes through (a constant base raised to a
		// mutation-compounded constant power, reduced during evaluation).
		using var catalog = new EvaluationCatalog<double>();
		var exp = catalog.GetExponent(catalog.GetConstant(2d), catalog.GetConstant(1e10));

		IEvaluate<double>? reduced = null;
		var spinner = Task.Run(() => reduced = catalog.GetReduced(exp));
		bool completedInTime = spinner.Wait(TimeSpan.FromSeconds(5));
		completedInTime.Should().BeTrue(
			"reducing a constant base to a huge constant power must not run the pre-fix uncapped sequential loop");

		reduced.Should().BeAssignableTo<IConstant<double>>().Which.Value.Should().Be(Math.Pow(2d, 1e10));
	}
}
