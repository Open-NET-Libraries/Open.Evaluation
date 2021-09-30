using Microsoft.VisualStudio.TestTools.UnitTesting;
using Open.Evaluation.Arithmetic;
using Open.Evaluation.Catalogs;
using Open.Evaluation.Core;

namespace Open.Evaluation.Tests
{
	public static class Sum
	{
		[TestClass]
		public class Default : ParseTestBase
		{
			const string FORMAT = "((2 * {0} * {1}) + ({0} * {1}) + ({2} * {3}))";
			const string RED = "((3 * {0} * {1}) + ({2} * {3}))";
			public Default() : base(FORMAT, null, RED) { }

			protected override double Expected
			{
				get
				{
					var x1 = PV[0] * PV[1];
					var x2 = PV[2] * PV[3];
					return 2 * x1 + x2 + x1;
				}
			}

		}

		[TestClass]
		public class ConstantCollapse : ParseTestBase
		{
			const string FORMAT = "(({0} + {1} + 13 + 17) * ({0} + {1}) * ({2} + {3}))";
			const string REP = "(({0} + {1} + 30) * ({0} + {1}) * ({2} + {3}))";
			public ConstantCollapse() : base(FORMAT, REP) { }

			protected override double Expected
			{
				get
				{
					var x1 = PV[0] + PV[1];
					var x2 = PV[2] + PV[3];
					var x3 = x1 + 30;
					return x1 * x2 * x3;
				}
			}
		}

		[TestClass]
		public class SumCollapse : ParseTestBase
		{
			const string FORMAT = "(({0} * {1}) + ({0} * {1}) + {2} + {2} + {3} + 2 + 1)";
			const string REP = "(({0} * {1}) + ({0} * {1}) + {2} + {2} + {3} + 3)";
			const string RED = "((2 * {0} * {1}) + (2 * {2}) + {3} + 3)";
			public SumCollapse() : base(FORMAT, REP, RED) { }

			protected override double Expected
			{
				get
				{
					var x1 = 2 * PV[0] * PV[1];
					var x2 = 2 * PV[2];
					var x3 = PV[3];
					return x1 + x2 + x3 + 3;
				}
			}
		}


		[TestClass]
		public class SumCollapse2
		{

			[TestMethod]
			[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "HAA0101:Array allocation for params parameter", Justification = "Reviewed.  Acceptable.")]
			public void TestMultipleCombine()
			{
				using var catalog = new EvaluationCatalog<double>();
				var a2 = catalog.GetExponent(catalog.GetParameter(0), 2);
				var g1 = catalog.ProductOf(catalog.GetConstant(9), a2);
				var b = catalog.GetParameter(1);
				var g2 = catalog.ProductOf(-1, a2);
				var sum = catalog.SumOf(g1, b, g2);
				var sumString = sum.ToStringRepresentation();
				Assert.AreEqual("((9 * ({0}²)) + {1} - (({0}²)))", sumString);
				Assert.AreEqual("((8 * ({0}²)) + {1})", catalog.GetReduced(sum).ToStringRepresentation());

			}
		}
	}
}
