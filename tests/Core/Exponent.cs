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

	// Issue #6: constructing x^1 via the public catalog path used to throw
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

	// Issue #6(b): checking the analogous exponent==0 case for the same collision class.
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
}
