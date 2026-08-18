using Open.Evaluation.Arithmetic;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Open.Evaluation.Tests.Arithmetic;

// Non-integer exponents are computed in double for EVERY numeric T (via saturating conversions
// on both sides): value-identical to the previous per-type switch for double/float, and Half /
// NFloat -- which used to throw "no supported calculation" even for √4 -- now work. No boxing.
[TestClass]
public class FractionalPow
{
	static T Eval<T>(EvaluationCatalog<T> catalog, IEvaluate<T> e) where T : notnull, INumber<T>
	{
		using var lease = Context.Rent();
		return e.Evaluate(lease.Item.Init(catalog, ReadOnlySpan<T>.Empty)).Result;
	}

	[TestMethod]
	public void SquareRoot_EvaluatesForEveryFloatingType()
	{
		Check<double>(9d, 0.5d, 3d);
		Check<float>(9f, 0.5f, 3f);
		Check<Half>((Half)9, (Half)0.5, (Half)3);
		Check<NFloat>((NFloat)9, (NFloat)0.5, (NFloat)3);
		Check<decimal>(9m, 0.5m, 3m);

		static void Check<T>(T b, T p, T expected) where T : notnull, INumber<T>
		{
			using var catalog = new EvaluationCatalog<T>();
			var e = catalog.GetExponent(catalog.GetConstant(b), catalog.GetConstant(p));
			T result = default!;
			Action act = () => result = Eval(catalog, e);
			act.Should().NotThrow(typeof(T).Name);
			result.Should().Be(expected, typeof(T).Name);
		}
	}

	[TestMethod]
	public void Double_IsBitIdenticalToMathPow()
	{
		using var catalog = new EvaluationCatalog<double>();
		foreach ((double b, double p) in new[] { (2d, 0.5d), (10d, 1.5d), (0.25d, -0.5d), (7d, 2.25d), (1e6, 0.1d) })
		{
			var e = catalog.GetExponent(catalog.GetConstant(b), catalog.GetConstant(p));
			BitConverter.DoubleToInt64Bits(Eval(catalog, e)).Should().Be(BitConverter.DoubleToInt64Bits(Math.Pow(b, p)), $"{b}^{p}");
		}
	}

	[TestMethod]
	public void Decimal_SaturatesInsteadOfThrowing()
	{
		using var catalog = new EvaluationCatalog<decimal>();
		// A result with no decimal representation (NaN) becomes zero, matching Undefined's rule;
		// it used to throw OverflowException from the double->decimal cast.
		var sqrtNeg = catalog.GetExponent(catalog.GetConstant(-4m), catalog.GetConstant(0.5m));
		decimal result = 1m;
		Action act = () => result = Eval(catalog, sqrtNeg);
		act.Should().NotThrow();
		result.Should().Be(0m);
		// (Reduction of the same expression is Undefined -- decided symbolically before any fold.)
		catalog.IsValid(sqrtNeg).Should().BeFalse();
	}
}
