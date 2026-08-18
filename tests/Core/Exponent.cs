using Open.Evaluation.Arithmetic;

namespace Open.Evaluation.Tests.Core;

public static class Exponent
{
	[TestClass]
	public class Default : ParseTestBase
	{
		const string FORMAT = "(({0} + {1})^({2} + {3}))";
		public Default() : base(FORMAT) { }

		protected override double Expected
		{
			get
			{
				var x1 = PV[0] + PV[1];
				var x2 = PV[2] + PV[3];
				return Math.Pow(x1, x2);
			}
		}
	}

	[TestClass]
	public class OneCollapse : ParseTestBase
	{
		const string FORMAT = "(({0} + {1})^0)";
		const string RED = "1";
		public OneCollapse() : base(FORMAT, "(({0} + {1})⁰)", RED) { }

		protected override double Expected => 1;
	}

	[TestClass]
	public class Division : ParseTestBase
	{
		const string FORMAT = "(2 * (({0} + {1})^-1))";
		public Division() : base(FORMAT, "(2 / ({0} + {1}))") { }

		protected override double Expected => 2 / (PV[0] + PV[1]);
	}

	[TestClass]
	public class DivisionOfConstants : ParseTestBase
	{
		const string FORMAT = "(9 * (3^-1))";
		public DivisionOfConstants() : base(FORMAT, "(9 / 3)", "3") { }

		protected override double Expected => 3;
	}

	[TestClass]
	public class DivisionOfConstantsDecimalResult : ParseTestBase
	{
		const string FORMAT = "(2^-1)";
		public DivisionOfConstantsDecimalResult() : base(FORMAT, "(1/2)", "0.5") { }

		protected override double Expected => 0.5;
	}

	[TestClass]
	public class DivisionOfMultiples : ParseTestBase
	{
		const string FORMAT = "(-9 * {0} * (-3^-1))";
		public DivisionOfMultiples() : base(FORMAT, "(-9 * {0} / -3)", "(3 * {0})") { }

		protected override double Expected => 3 * PV[0];
	}

	[TestClass]
	public class SquareRoot : ParseTestBase
	{
		const string FORMAT = "(2 * (({0} + {1})^0.5))";
		public SquareRoot() : base(FORMAT, "(2 * √({0} + {1}))") { }

		protected override double Expected => 2 * Math.Sqrt(PV[0] + PV[1]);
	}

	[TestClass]
	public class ExponentOfConstants : ParseTestBase
	{
		const string FORMAT = "((({0})^3)^2)";
		public ExponentOfConstants() : base(FORMAT, "(({0}³)²)", "({0}⁶)") { }

		protected override double Expected => Math.Pow(PV[0], 6);
	}

	// Constructing x^1 via the public catalog path used to throw
	// InvalidCastException because the unreduced rendering collided with the bare base's
	// own catalog key ("x^1" rendered as just "x"). These tests exercise that path directly
	// (catalog.GetExponent), rather than through the string parser, to pin the fix down at
	// its source: Exponent<T>.Describe and Catalog.Register.
	[TestClass]
	public class PowerOfOneIdentity
	{
		[TestMethod]
		public void ConstructingPowerOfOne_DoesNotThrow()
		{
			using var catalog = new EvaluationCatalog<double>();
			var x = catalog.GetParameter(0);
			var one = catalog.GetConstant(1d);

			Exponent<double>? exponent = null;
			Action act = () => exponent = catalog.GetExponent(x, one);

			act.Should().NotThrow();
			exponent.Should().NotBeNull();
		}

		[TestMethod]
		public void RendersDistinctlyFromTheBareBase()
		{
			using var catalog = new EvaluationCatalog<double>();
			var x = catalog.GetParameter(0);
			var one = catalog.GetConstant(1d);
			var exponent = catalog.GetExponent(x, one);

			exponent.Description.Value.Should().Be("({0}^1)");
			exponent.Description.Value.Should().NotBe(x.Description.Value);
		}

		[TestMethod]
		public void ReductionReturnsTheSameCatalogParameterInstance()
		{
			using var catalog = new EvaluationCatalog<double>();
			var x = catalog.GetParameter(0);
			var one = catalog.GetConstant(1d);
			var exponent = catalog.GetExponent(x, one);

			var reduced = catalog.GetReduced(exponent);

			ReferenceEquals(reduced, x).Should().BeTrue("reduction of x^1 should hand back the catalog's own x instance, not a copy");
		}

		[TestMethod]
		public void EvaluatingUnreducedEqualsEvaluatingBase()
		{
			using var catalog = new EvaluationCatalog<double>();
			var x = catalog.GetParameter(0);
			var one = catalog.GetConstant(1d);
			var exponent = catalog.GetExponent(x, one);

			foreach (double v in new[] { 2d, -3.5, 0d, 100d })
			{
				using var lease = Context.Rent();
				var context = lease.Item.Init(catalog, (ReadOnlySpan<double>)[v]);

				exponent.Evaluate(context).Result
					.Should().Be(x.Evaluate(context).Result);
			}
		}
	}

	// Checking the analogous exponent==0 case for the same collision class.
	// Unlike x^1, x^0 never collided (constant `1` renders as "1", not "(x⁰)"), so no
	// rendering change was needed there -- this test pins down that it stays that way.
	// (tests/Core/Exponent.cs's OneCollapse class already covers this indirectly via the
	// string parser; this covers the same ground directly through the public catalog path.)
	[TestClass]
	public class PowerOfZeroNoCollision
	{
		[TestMethod]
		public void ConstructingPowerOfZero_DoesNotThrow_AndRendersDistinctlyFromConstantOne()
		{
			using var catalog = new EvaluationCatalog<double>();
			var x = catalog.GetParameter(0);
			var zero = catalog.GetConstant(0d);

			Exponent<double>? exponent = null;
			Action act = () => exponent = catalog.GetExponent(x, zero);

			act.Should().NotThrow();
			exponent!.Description.Value.Should().Be("({0}⁰)");
			exponent.Description.Value.Should().NotBe(catalog.GetConstant(1d).Description.Value);
		}
	}

	// IsSquareRoot's fallback check used to go through
	// Catalog.Register("(1/2)", ...), but the factory's computation -- GetExponent(2, -1)
	// .GetReduction() -- reduces to a Constant registered under "0.5", not "(1/2)". Register's
	// id/hash consistency check then threw ArgumentException on the very first call against a
	// fresh catalog (before that could even surface, TryGetItem's own always-firing assert could
	// FailFast in DEBUG on the way in). Fixed by computing the reduction directly instead of
	// through the mismatched Register call; the "0.5" fast path is unaffected.
	[TestClass]
	public class IsSquareRootTests
	{
		[TestMethod]
		public void NonSquareRootExponent_OnFreshCatalog_ReturnsFalseWithoutFailFast()
		{
			using var catalog = new EvaluationCatalog<double>();
			var p0 = catalog.GetParameter(0);
			var square = catalog.GetExponent(p0, catalog.GetConstant(2d));

			bool result = false;
			Action act = () => result = square.IsSquareRoot();

			act.Should().NotThrow();
			result.Should().BeFalse();
		}

		[TestMethod]
		public void GenuineHalfPower_OnFreshCatalog_ReturnsTrue()
		{
			using var catalog = new EvaluationCatalog<double>();
			var p0 = catalog.GetParameter(0);

			// Built the same way the real pipeline would organically arrive at 0.5 (e.g. via
			// GetFloatFunction(SquareRoot, x) -> GetExponent(x, ValueFloat<T>.Half)), without
			// calling GetConstant(0.5d) directly first -- so the "0.5" fast path inside
			// IsSquareRoot hasn't already been pre-populated by this test itself.
			var half = catalog.GetExponent(catalog.GetConstant(2d), catalog.GetConstant(-1d)).GetReduction();
			var sqrtOfX = catalog.GetExponent(p0, half);

			bool result = false;
			Action act = () => result = sqrtOfX.IsSquareRoot();

			act.Should().NotThrow();
			result.Should().BeTrue();
		}

		[TestMethod]
		public void SymbolicHalfPower_Unreduced_ReturnsTrue()
		{
			using var catalog = new EvaluationCatalog<double>();
			var p0 = catalog.GetParameter(0);

			// The power is the UNREDUCED symbolic (1/2) node itself -- previously a confirmed
			// gap (matched neither the "0.5" constant nor the interned symbolic by reference).
			// Interning makes the symbolic power and IsSquareRoot's registered (1/2) the SAME
			// node, so the direct comparison matches by reference.
			var symbolicHalf = catalog.GetExponent(catalog.GetConstant(2d), catalog.GetConstant(-1d));
			var sqrtOfX = catalog.GetExponent(p0, symbolicHalf);

			((Exponent<double>)sqrtOfX).IsSquareRoot().Should().BeTrue();
		}

		[TestMethod]
		public void IntegerCatalog_SymbolicHalfPower_ReturnsFalse()
		{
			using var catalog = new EvaluationCatalog<int>();
			var p0 = catalog.GetParameter(0);

			// THE case the IsFloatingPoint gate actually decides. Construction of the symbolic
			// (1/2) node is unrestricted, so it exists even for integer T -- and for integer T
			// the reduction machinery refuses the half division and keeps it SYMBOLIC. Without
			// the gate, x^(2^-1) would match the interned (1/2) by reference and answer "true".
			// (An earlier version of this test used x^0 believing the hazard was truncation --
			// mutation testing proved that case is independently protected by the reduction
			// guard and never reaches the comparison at all.)
			var symbolicHalf = catalog.GetExponent(catalog.GetConstant(2), catalog.GetConstant(-1));
			var sqrtShaped = catalog.GetExponent(p0, symbolicHalf);

			((Exponent<int>)sqrtShaped).IsSquareRoot().Should().BeFalse();
		}

		[TestMethod]
		public void DegeneratePowerSubtree_AnswersFalse_NeverThrows()
		{
			using var catalog = new EvaluationCatalog<double>();
			var p0 = catalog.GetParameter(0);

			// x^(0^-1): a legal-to-construct tree whose power subtree is degenerate --
			// REDUCING it throws ("0 to a negative power is undefined"). IsSquareRoot is a
			// predicate on the mutation path (MutateSign's parentIsSquareRoot), so it must
			// answer false here, not throw. Pins the deliberate never-reduce-Power choice.
			var degenerate = catalog.GetExponent(catalog.GetConstant(0d), catalog.GetConstant(-1d));
			var outer = catalog.GetExponent(p0, degenerate);

			bool result = true;
			Action act = () => result = ((Exponent<double>)outer).IsSquareRoot();

			act.Should().NotThrow();
			result.Should().BeFalse();
		}

		[TestMethod]
		public void FloatCatalog_FreshCatalog_BothForms_ReturnTrue()
		{
			// Non-double floating T: the Register("(1/2)") key must match the factory
			// product's rendering EXACTLY or the first call throws. Pins that Constant<float>
			// renders invariantly ("2", "-1") like double.
			using var catalog = new EvaluationCatalog<float>();
			var p0 = catalog.GetParameter(0);

			var symbolicHalf = catalog.GetExponent(catalog.GetConstant(2f), catalog.GetConstant(-1f));
			((Exponent<float>)catalog.GetExponent(p0, symbolicHalf)).IsSquareRoot().Should().BeTrue();

			var reducedHalf = catalog.GetReduced(symbolicHalf);
			((Exponent<float>)catalog.GetExponent(p0, reducedHalf)).IsSquareRoot().Should().BeTrue();
		}

		[TestMethod]
		public void DecimalCatalog_FreshCatalog_BothForms_ReturnTrue()
		{
			// Same rendering-contract pin for decimal (an IFloatingPoint that is not IEEE).
			using var catalog = new EvaluationCatalog<decimal>();
			var p0 = catalog.GetParameter(0);

			var symbolicHalf = catalog.GetExponent(catalog.GetConstant(2m), catalog.GetConstant(-1m));
			((Exponent<decimal>)catalog.GetExponent(p0, symbolicHalf)).IsSquareRoot().Should().BeTrue();

			var reducedHalf = catalog.GetReduced(symbolicHalf);
			((Exponent<decimal>)catalog.GetExponent(p0, reducedHalf)).IsSquareRoot().Should().BeTrue();
		}
	}
}
