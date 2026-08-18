using Open.Evaluation.Arithmetic;
using Open.Hierarchy;
using System.Numerics;

namespace Open.Evaluation.Tests.Arithmetic;

// Reduction is the validity detector: anything that constant-folds to an everywhere-undefined
// value reduces to the catalog's single Undefined expression, and containment poisons the whole
// tree -- so an invalid reduction condemns its (unreduced) source. Reduction never throws.
[TestClass]
public class UndefinedTests
{
	static Exponent<double> Sqrt(EvaluationCatalog<double> c, IEvaluate<double> x) => c.GetExponent(x, 0.5d);

	[TestMethod]
	public void ZeroToNegativePower_ReducesToUndefined_NotThrow()
	{
		using var catalog = new EvaluationCatalog<double>();
		var zeroInverse = catalog.GetExponent(catalog.GetConstant(0d), catalog.GetConstant(-1d));

		IEvaluate<double>? reduced = null;
		Action act = () => reduced = catalog.GetReduced(zeroInverse);

		act.Should().NotThrow();
		reduced.Should().BeSameAs(catalog.GetUndefined());
		catalog.IsValid(zeroInverse).Should().BeFalse();
	}

	[TestMethod]
	public void DivisionByConstantZero_IsUndefined()
	{
		using var catalog = new EvaluationCatalog<double>();
		var x = catalog.GetParameter(0);
		// x / 0 is x · 0⁻¹ in this algebra.
		var xOverZero = catalog.ProductOf(x, catalog.GetExponent(catalog.GetConstant(0d), catalog.GetConstant(-1d)));

		catalog.GetReduced(xOverZero).Should().BeSameAs(catalog.GetUndefined());
		catalog.IsValid(xOverZero).Should().BeFalse();
	}

	[TestMethod]
	public void ZeroAppearingOnlyAfterReduction_CondemnsTheSource()
	{
		using var catalog = new EvaluationCatalog<double>();
		var a = catalog.GetParameter(0);
		// (a - a)⁻¹: the base is not syntactically zero; it reduces to zero.
		var aMinusA = catalog.SumOf(a, catalog.ProductOf(-1d, a));
		var source = catalog.GetExponent(aMinusA, catalog.GetConstant(-1d));

		catalog.GetReduced(aMinusA).Should().BeSameAs(catalog.GetConstant(0d), "the base must genuinely reduce to zero for this test to mean anything");
		catalog.GetReduced(source).Should().BeSameAs(catalog.GetUndefined());
		catalog.IsValid(source).Should().BeFalse("an invalid reduction condemns its unreduced source");
	}

	[TestMethod]
	public void ZeroTimesUndefined_IsUndefined_NotZero()
	{
		using var catalog = new EvaluationCatalog<double>();
		var zeroInverse = catalog.GetExponent(catalog.GetConstant(0d), catalog.GetConstant(-1d));
		var product = catalog.ProductOf(catalog.GetConstant(0d), zeroInverse);

		catalog.GetReduced(product).Should().BeSameAs(catalog.GetUndefined(), "poison must take precedence over the zero-factor collapse");
	}

	[TestMethod]
	public void ContainmentPoisons_ThroughSumProductAndExponentNesting()
	{
		using var catalog = new EvaluationCatalog<double>();
		var x = catalog.GetParameter(0);
		var y = catalog.GetParameter(1);
		var zeroInverse = catalog.GetExponent(catalog.GetConstant(0d), catalog.GetConstant(-1d));

		// √(x² + y·(3 + 0⁻¹)) -- the undefined leaf is three levels down.
		var tree = Sqrt(catalog, catalog.SumOf(
			catalog.GetExponent(x, 2d),
			catalog.ProductOf(y, catalog.SumOf(catalog.GetConstant(3d), zeroInverse))));

		catalog.GetReduced(tree).Should().BeSameAs(catalog.GetUndefined());
		catalog.IsValid(tree).Should().BeFalse();

		// Undefined as a POWER poisons too, and an Undefined base survives the power==1 shortcut.
		catalog.GetReduced(catalog.GetExponent(x, catalog.GetUndefined())).Should().BeSameAs(catalog.GetUndefined());
		catalog.GetReduced(catalog.GetExponent(catalog.GetUndefined(), catalog.GetConstant(1d))).Should().BeSameAs(catalog.GetUndefined());
	}

	[TestMethod]
	public void NegativeBaseToNonIntegerPower_IsUndefined_ForTypesWithAndWithoutNaN()
	{
		// √(-4) has no real value: undefined everywhere, decided symbolically -- so it is
		// Undefined rather than a NaN constant (double) or a throw (decimal cannot hold NaN).
		using (var catalog = new EvaluationCatalog<double>())
		{
			var sqrtNeg = catalog.GetExponent(catalog.GetConstant(-4d), catalog.GetConstant(0.5d));
			catalog.GetReduced(sqrtNeg).Should().BeSameAs(catalog.GetUndefined());
			catalog.IsValid(sqrtNeg).Should().BeFalse();
			// ...while an integer power of a negative base is perfectly defined.
			catalog.GetReduced(catalog.GetExponent(catalog.GetConstant(-4d), catalog.GetConstant(-1d)))
				.Should().BeSameAs(catalog.GetConstant(-0.25d));
		}

		using (var catalog = new EvaluationCatalog<decimal>())
		{
			var sqrtNeg = catalog.GetExponent(catalog.GetConstant(-4m), catalog.GetConstant(0.5m));
			IEvaluate<decimal>? reduced = null;
			Action act = () => reduced = catalog.GetReduced(sqrtNeg);
			act.Should().NotThrow();
			reduced.Should().BeSameAs(catalog.GetUndefined());
		}
	}

	[TestMethod]
	public void NonZeroBaseToZeroPower_IsOne_UnderEveryPolicy()
	{
		// Reachable only under non-default policies; the fold used to return zero.
		using var catalog = new EvaluationCatalog<double>();
		var five = catalog.GetConstant(5d);
		var zero = catalog.GetConstant(0d);
		var underUndefinedPolicy = catalog.Register(new UndefinedZeroExponent(catalog, five, zero));
		catalog.GetReduced(underUndefinedPolicy).Should().BeSameAs(catalog.GetConstant(1d));
	}

	[TestMethod]
	public void PointwiseUndefined_StaysValid()
	{
		using var catalog = new EvaluationCatalog<double>();
		var x = catalog.GetParameter(0);
		// 1/x and √x are undefined only at some inputs: poles, not invalid expressions.
		catalog.IsValid(catalog.GetExponent(x, catalog.GetConstant(-1d))).Should().BeTrue();
		catalog.IsValid(Sqrt(catalog, x)).Should().BeTrue();
	}

	[TestMethod]
	public void PowerOfZero_UndefinedPolicy()
	{
		// The default policy (One) is unchanged...
		using (var catalog = new EvaluationCatalog<double>())
		{
			var zero = catalog.GetConstant(0d);
			catalog.GetReduced(catalog.GetExponent(zero, zero)).Should().BeSameAs(catalog.GetConstant(1d));
		}

		// ...and the new policy reports Undefined. Separate catalog on purpose: the policy is a
		// per-instance construction choice while interning is by rendering, so whichever "(0⁰)"
		// instance a catalog registers first is the one it keeps.
		using (var catalog = new EvaluationCatalog<double>())
		{
			var zero = catalog.GetConstant(0d);
			var undefinedPolicy = catalog.Register(new UndefinedZeroExponent(catalog, zero, zero));
			catalog.GetReduced(undefinedPolicy).Should().BeSameAs(catalog.GetUndefined());
		}
	}

	private sealed class UndefinedZeroExponent(ICatalog<IEvaluate<double>> catalog, IEvaluate<double> b, IEvaluate<double> p)
		: Exponent<double>(catalog, b, p, Exponent.PowerOfZeroReduction.Undefined);

	[TestMethod]
	public void SingleInternedInstance_RendersToken_ParsesRoundTrip()
	{
		using var catalog = new EvaluationCatalog<double>();
		var u = catalog.GetUndefined();

		ReferenceEquals(u, catalog.GetUndefined()).Should().BeTrue();
		u.Description.Value.Should().Be(Undefined<double>.Token);
		u.IsUndefined().Should().BeTrue();
		catalog.GetConstant(1d).IsUndefined().Should().BeFalse();

		// Non-generic type check, like IParameter / IConstant, so callers that only hold an
		// IEvaluate can recognize it without knowing T.
		IEvaluate untyped = u;
		(untyped is IUndefined).Should().BeTrue();
		(untyped is IConstant).Should().BeFalse("Undefined must never be mistaken for a constant");

		catalog.Parse("Undefined").Should().BeSameAs(u);
		catalog.Parse("{Undefined}").Should().BeSameAs(u, "an already-braced token parses identically");
		var parsedSum = catalog.Parse("(Undefined + {0})");
		catalog.GetReduced(parsedSum).Should().BeSameAs(u, "the token composes with the operator grammar and poisons on reduction");
		catalog.GetReduced(catalog.Parse("(2 * {Undefined})")).Should().BeSameAs(u);
	}

	[TestMethod]
	public void EveryNumericType_HasAnUndefined()
	{
		// Every signed numeric type: unsigned types cannot express a negative power at all
		// (-T.One wraps), so 0⁻¹ is not constructible for them in the first place.
		Check<double>(); Check<float>(); Check<Half>(); Check<System.Runtime.InteropServices.NFloat>(); Check<decimal>();
		Check<int>(); Check<long>(); Check<sbyte>(); Check<BigInteger>(); Check<Int128>();

		static void Check<T>() where T : notnull, INumber<T>
		{
			using var catalog = new EvaluationCatalog<T>();
			var zeroInverse = catalog.GetExponent(catalog.GetConstant(T.Zero), catalog.GetConstant(-T.One));
			catalog.GetReduced(zeroInverse).Should().BeSameAs(catalog.GetUndefined(), typeof(T).Name);
			catalog.IsValid(zeroInverse).Should().BeFalse(typeof(T).Name);
		}
	}

	// Normal operation never evaluates an Undefined expression: reduction, mutation and variation
	// over trees that contain it must all complete without touching Evaluate. In DEBUG builds
	// Undefined.EvaluateInternal fails fast, so merely PASSING in the Debug configuration proves it.
	[TestMethod]
	public void MutationAndVariation_OverTreesContainingUndefined_NeverEvaluateIt()
	{
		using var catalog = new EvaluationCatalog<double>();
		var x = catalog.GetParameter(0);
		var y = catalog.GetParameter(1);
		var tree = Sqrt(catalog, catalog.SumOf(catalog.GetExponent(x, 2d), catalog.ProductOf(y, catalog.GetUndefined())));

		var node = catalog.Factory.Map(tree);
		var leaves = node.GetDescendants().Cast<Node<IEvaluate<double>>>().Where(n => n.Value is IParameter or Undefined<double>).ToArray();
		foreach (var leaf in leaves)
		{
			_ = catalog.Mutation.Square(leaf);
			_ = catalog.Mutation.MutateSign(leaf, 1);
			_ = catalog.Variation.ApplyRandomFunction(leaf);
		}

		_ = catalog.Variation.PromoteChildren(node.Children[0]);
		_ = catalog.GetReduced(tree);

		node.Recycle();
	}

#if !DEBUG
	// The value contract is only observable in RELEASE: DEBUG builds fail fast on evaluation by
	// design (the canary that keeps it from ever being observed in normal operation).
	[TestMethod]
	public void Evaluate_ReturnsNaNWhereSupported_ZeroOtherwise_NeverThrows()
	{
		Check<double>(expectNaN: true); Check<float>(expectNaN: true); Check<Half>(expectNaN: true);
		Check<decimal>(expectNaN: false); Check<int>(expectNaN: false); Check<BigInteger>(expectNaN: false);

		static void Check<T>(bool expectNaN) where T : notnull, INumber<T>
		{
			using var catalog = new EvaluationCatalog<T>();
			using var lease = Context.Rent();
			var context = lease.Item.Init(catalog, ReadOnlySpan<T>.Empty);
			T result = default!;
			Action act = () => result = catalog.GetUndefined().Evaluate(context).Result;
			act.Should().NotThrow(typeof(T).Name);
			if (expectNaN) T.IsNaN(result).Should().BeTrue(typeof(T).Name);
			else T.IsZero(result).Should().BeTrue(typeof(T).Name);
		}
	}
#endif
}
