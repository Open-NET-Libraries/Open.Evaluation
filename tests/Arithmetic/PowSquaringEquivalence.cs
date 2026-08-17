using System.Numerics;
using System.Reflection;
using Open.Evaluation.Arithmetic;

namespace Open.Evaluation.Tests.Arithmetic;

/// <summary>
/// Equivalence tests for the exponentiation-by-squaring fast path added to the internal
/// <c>Exponent.Pow&lt;T&gt;</c> extension method for integer T (T : IBinaryInteger&lt;T&gt;).
/// The squaring path is only reachable/meaningful for a positive, integer-valued exponent
/// (0 and negative exponents, and base 0/1, are handled by guards that run before the loop
/// and are untouched by this change) -- see Open.Evaluation.Arithmetic/Exponent.cs.
///
/// <c>Pow&lt;T&gt;</c> is invoked directly via reflection (it is `internal`, and the test
/// assembly has no InternalsVisibleTo) rather than through the public
/// catalog/Exponent&lt;T&gt;-node evaluation path. This is deliberate, not a workaround: it
/// targets exactly the changed method and sidesteps the unrelated catalog machinery
/// entirely. (An earlier version of this comment also cited a since-fixed catalog/interning
/// defect -- exponent == 1 used to collide with the base's own key because
/// <c>Exponent&lt;T&gt;.Describe</c> rendered "x^1" as just "x", so <c>Catalog.Register</c>
/// handed back the wrong type on that interning hit. See
/// tests/Core/Exponent.cs's PowerOfOneIdentity and tests/Core/Catalog.cs for the fix and its
/// coverage; that collision is no longer a reason to avoid the public path here.) A separate
/// small end-to-end test below confirms the public evaluation path still works for ordinary
/// cases.
///
/// Every grid case is checked against TWO independent oracles:
///  1. A reference sequential-multiplication loop, reproduced in-test exactly as it existed
///     before this change (unchecked, so wraparound behaves the same way), including the
///     same base==0/1 and exponent==0 guards in the same order.
///  2. An exact BigInteger power (same guarded shape), explicitly reduced modulo 2^width and
///     reinterpreted as the target type's two's-complement bit pattern. This oracle is
///     independent of both loop implementations, so it empirically proves the "unchecked
///     integer multiplication is exact ring arithmetic mod 2^width" claim that justifies the
///     optimization -- rather than merely proving two implementations we wrote agree.
/// </summary>
[TestClass]
public class PowSquaringEquivalence
{
    static readonly int[] Exponents = [0, 1, 2, 3, 5, 8, 13, 31, 64, 100];

    static readonly MethodInfo PowMethodDefinition =
        typeof(Exponent).GetMethod("Pow", BindingFlags.NonPublic | BindingFlags.Static)
        ?? throw new InvalidOperationException("Exponent.Pow<T>(this T, T) extension method not found via reflection; test needs updating.");

    // Invokes the ACTUAL production Exponent.Pow<T>(this T baseValue, T exponent) -- the
    // method modified by this change -- bypassing the unrelated catalog defect described
    // above.
    static T InvokeProductionPow<T>(T baseValue, T exponent)
        where T : notnull, INumber<T>
    {
        var closed = PowMethodDefinition.MakeGenericMethod(typeof(T));
        return (T)closed.Invoke(null, [baseValue, exponent])!;
    }

    // Mirrors EXACTLY the guards + sequential-multiplication loop that existed in
    // Exponent.Pow<T> before the squaring fast path was added (the negative-exponent
    // division loop and the floating-point dispatch are omitted here since this helper is
    // only ever called with a non-negative integer exponent in this test).
    static T ReferenceSequentialPow<T>(T baseValue, T exponent)
        where T : notnull, INumber<T>
    {
        unchecked
        {
            if (baseValue == T.Zero || baseValue == T.One)
                return baseValue;

            if (exponent == T.Zero)
                return T.MultiplicativeIdentity;

            T result = baseValue;
            for (T i = T.One; i < exponent; i++)
                result *= baseValue;

            return result;
        }
    }

    static BigInteger PositiveMod(BigInteger value, BigInteger modulus)
    {
        BigInteger m = value % modulus;
        return m < 0 ? m + modulus : m;
    }

    // Same guard shape as ReferenceSequentialPow/the production guards (base==0/1 checked
    // BEFORE exponent==0 -- so 0^0 deliberately evaluates to 0 here, matching the library's
    // existing, pre-this-change behavior), but the positive-exponent branch is computed via
    // exact BigInteger.Pow instead of either loop implementation -- the independent oracle.
    static BigInteger GuardedExactBigInteger(BigInteger baseValue, int exponent)
        => baseValue.IsZero || baseValue == BigInteger.One
            ? baseValue
            : exponent == 0 ? BigInteger.One : BigInteger.Pow(baseValue, exponent);

    static int WrapToInt32(BigInteger baseValue, int exponent)
        => unchecked((int)(uint)PositiveMod(GuardedExactBigInteger(baseValue, exponent), BigInteger.One << 32));

    static long WrapToInt64(BigInteger baseValue, int exponent)
        => unchecked((long)(ulong)PositiveMod(GuardedExactBigInteger(baseValue, exponent), BigInteger.One << 64));

    static byte WrapToByte(BigInteger baseValue, int exponent)
        => (byte)PositiveMod(GuardedExactBigInteger(baseValue, exponent), BigInteger.One << 8);

    [TestMethod]
    public void Int32_MatchesReferenceLoopAndBigIntegerOracle()
    {
        int[] bases = [-3, -2, -1, 0, 1, 2, 3, 200_000, -200_000, int.MaxValue - 1, int.MinValue + 1];

        foreach (int b in bases)
        {
            foreach (int e in Exponents)
            {
                int reference = ReferenceSequentialPow(b, e);
                int oracle = WrapToInt32(b, e);
                int actual = InvokeProductionPow(b, e);

                actual.Should().Be(reference, $"squaring result must match sequential-loop reference for base={b}, exponent={e}");
                actual.Should().Be(oracle, $"squaring result must match BigInteger-mod-2^32 oracle for base={b}, exponent={e}");
            }
        }
    }

    [TestMethod]
    public void Int64_MatchesReferenceLoopAndBigIntegerOracle()
    {
        long[] bases = [-3, -2, -1, 0, 1, 2, 3, 3_000_000_000L, -3_000_000_000L, long.MaxValue - 1, long.MinValue + 1];

        foreach (long b in bases)
        {
            foreach (int e in Exponents)
            {
                long exponent = e;
                long reference = ReferenceSequentialPow(b, exponent);
                long oracle = WrapToInt64(b, e);
                long actual = InvokeProductionPow(b, exponent);

                actual.Should().Be(reference, $"squaring result must match sequential-loop reference for base={b}, exponent={e}");
                actual.Should().Be(oracle, $"squaring result must match BigInteger-mod-2^64 oracle for base={b}, exponent={e}");
            }
        }
    }

    [TestMethod]
    public void Byte_MatchesReferenceLoopAndBigIntegerOracle_UnsignedWraparound()
    {
        // Unsigned, narrow width: wraps (mod 256) constantly across this exponent grid --
        // the densest empirical proof of the mod-2^width ring claim in this test class.
        byte[] bases = [0, 1, 2, 3, 100, 200, 250, 255];

        foreach (byte b in bases)
        {
            foreach (int e in Exponents)
            {
                byte exponent = (byte)e;
                byte reference = ReferenceSequentialPow(b, exponent);
                byte oracle = WrapToByte(b, e);
                byte actual = InvokeProductionPow(b, exponent);

                actual.Should().Be(reference, $"squaring result must match sequential-loop reference for base={b}, exponent={e}");
                actual.Should().Be(oracle, $"squaring result must match BigInteger-mod-2^8 oracle for base={b}, exponent={e}");
            }
        }
    }

    [TestMethod]
    public void BigInteger_MatchesReferenceLoop_ArbitraryPrecisionNoWraparound()
    {
        // BigInteger implements IBinaryInteger<BigInteger> too, so it also takes the
        // squaring path -- but has no fixed width, so this exercises the "exact,
        // arbitrary-precision" half of the ring argument rather than the "mod 2^n" half.
        BigInteger[] bases = [-3, -2, -1, 0, 1, 2, 3, 123456789, -123456789];

        foreach (BigInteger b in bases)
        {
            foreach (int e in Exponents)
            {
                BigInteger exponent = e;
                BigInteger reference = ReferenceSequentialPow(b, exponent);
                BigInteger exact = GuardedExactBigInteger(b, e);
                BigInteger actual = InvokeProductionPow(b, exponent);

                actual.Should().Be(reference, $"squaring result must match sequential-loop reference for base={b}, exponent={e}");
                actual.Should().Be(exact, $"squaring result must match exact BigInteger.Pow for base={b}, exponent={e}");
            }
        }
    }

    [TestMethod]
    public void Int32_WraparoundActuallyOccursInThisGrid_SanityCheckOnTheTestItself()
    {
        // Guards against a vacuous proof: confirms that at least one grid case in
        // Int32_MatchesReferenceLoopAndBigIntegerOracle genuinely overflows Int32 range,
        // so the wraparound agreement above is not accidental.
        bool anyOverflow = false;
        foreach (int b in new[] { 200_000, -200_000, int.MaxValue - 1 })
        {
            foreach (int e in Exponents)
            {
                if (e < 2) continue;
                BigInteger exact = BigInteger.Pow(b, e);
                if (exact > int.MaxValue || exact < int.MinValue)
                {
                    anyOverflow = true;
                    break;
                }
            }
        }

        anyOverflow.Should().BeTrue("the test grid must actually exercise Int32 overflow/wraparound, not just in-range values");
    }

    [TestMethod]
    public void Int32_NegativeExponent_UnaffectedByChange_StillIntegerTruncationBehavior()
    {
        // The squaring optimization deliberately does NOT touch the negative-exponent
        // division loop (see Exponent.cs comments / final report: division is not a ring
        // operation, so the reorder-exactness argument doesn't trivially extend). This is
        // a light confirmation that documented pre-existing behavior (see
        // tests/Arithmetic/GenericNumeric.cs) is unchanged for a couple of extra cases,
        // through the actual Pow<T> method.
        //
        // base=-1 with an EVEN-magnitude negative exponent (e.g. (-1)^-4) used to trip a
        // separate, Debug-only defect in that same loop: Debug.Assert(result != T.One,
        // "Type must be capable of division.") fired incorrectly, because dividing 1 by -1
        // an even number of times legitimately lands back on 1 (the mathematically correct
        // answer), which the assert wrongly treated as proof the type can't divide. Fixed
        // (issue #7) by checking the assert's canary condition only after the FIRST
        // division in the loop -- the point where an incapable-division type would
        // actually reveal itself, since base is never 0 or 1 there (both short-circuit
        // earlier, above). Now exercised here (previously excluded with this same note).
        InvokeProductionPow(2, -1).Should().Be(0);
        InvokeProductionPow(2, -3).Should().Be(0);
        InvokeProductionPow(-1, -3).Should().Be(-1);
        InvokeProductionPow(-1, -2).Should().Be(1);
        InvokeProductionPow(-1, -4).Should().Be(1);
    }

    [TestMethod]
    public void EndToEnd_PublicEvaluationPath_OrdinaryNonCollidingCases()
    {
        // Confirms the real public path (EvaluationCatalog -> Exponent<T> node ->
        // Context.Evaluate) still produces the correct, squaring-accelerated result for
        // typical cases.
        using var catalog = new EvaluationCatalog<int>();

        var exp = catalog.GetExponent(catalog.GetConstant(3), catalog.GetConstant(5));
        using (var lease = Context.Rent())
            exp.Evaluate(lease.Item).Result.Should().Be(243); // 3^5

        var exp2 = catalog.GetExponent(catalog.GetConstant(2), catalog.GetConstant(31));
        using (var lease = Context.Rent())
            exp2.Evaluate(lease.Item).Result.Should().Be(1 << 31); // int.MinValue, via wraparound

        var expLarge = catalog.GetExponent(catalog.GetConstant(200_000), catalog.GetConstant(64));
        int expectedLarge = InvokeProductionPow(200_000, 64);
        using (var lease = Context.Rent())
            expLarge.Evaluate(lease.Item).Result.Should().Be(expectedLarge);
    }
}
