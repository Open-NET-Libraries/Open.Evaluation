using Microsoft.VisualStudio.TestTools.UnitTesting;
using Open.Evaluation.Arithmetic;
using Open.Evaluation.Catalogs;
using System;

namespace Open.Evaluation.Tests
{
	public static class Product
	{
		[TestClass]
		public class Default : ParseTestBase
		{
			const string FORMAT = "(({0} + {1} + 2) * ({0} + {1}) * ({2} + {3}))";
			public Default() : base(FORMAT) { }

			protected override double Expected
			{
				get
				{
					var x1 = PV[0] + PV[1];
					var x2 = PV[2] + PV[3];
					var x3 = x1 + 2;
					return x1 * x2 * x3;
				}
			}
		}

		[TestClass]
		public class ConstantCollapse : ParseTestBase
		{
			const string FORMAT = "(({0} * {1}) + (7 * 5 * 2 * {0}) + ({2} * {3}))";
			const string REP = "(({0} * {1}) + ({2} * {3}) + (70 * {0}))";
			public ConstantCollapse() : base(FORMAT, REP) { }

			protected override double Expected
			{
				get
				{
					var x1 = PV[0] * PV[1];
					var x2 = 7 * 5 * 2 * PV[0];
					var x3 = PV[2] * PV[3];
					return x1 + x2 + x3;
				}
			}
		}

		[TestClass]
		public class ProductCollapse : ParseTestBase
		{
			const string FORMAT = "(({0} + {1}) * ({0} + {1}) * {2} * {2} * {3} * 2 * 1)";
			const string REP = "(2 * ({0} + {1}) * ({0} + {1}) * {2} * {2} * {3})";
			const string RED = "(2 * (({0} + {1})²) * ({2}²) * {3})";
			public ProductCollapse() : base(FORMAT, REP, RED) { }

			protected override double Expected
				=> Math.Pow(PV[0] + PV[1], 2)
				  * Math.Pow(PV[2], 2)
				  * PV[3]
				  * 2;
		}

		[TestClass]
		public class ZeroCollapse : ParseTestBase
		{
			const string FORMAT = "(({0} + {1}) * 0 * {2} * {2} * {3} * 2 * 1)";
			const string REP = "(0 * ({0} + {1}) * {2} * {2} * {3})";
			const string RED = "0";
			public ZeroCollapse() : base(FORMAT, REP, RED) { }

			protected override double Expected => 0;
		}

		[TestClass]
		public class ProductOfSums
		{
			[TestMethod]
			public void Product()
			{
				using var catalog = new EvaluationCatalog<double>();

				var a = (Sum<double>)catalog.Parse("({0} + {1})");
				var b = (Sum<double>)catalog.Parse("({2} + {3})");

				var p = catalog.ProductOfSums(a, b);
				var v = p.Evaluate(new double[] { 1, 2, 3, 4 });
				var s = p.ToStringRepresentation();
				Assert.AreEqual("(({0} * {2}) + ({0} * {3}) + ({1} * {2}) + ({1} * {3}))", s);
				Assert.AreEqual(21, v);
			}
		}
	}

}
