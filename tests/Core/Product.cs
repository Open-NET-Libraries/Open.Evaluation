using Microsoft.VisualStudio.TestTools.UnitTesting;

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
	}
	
}