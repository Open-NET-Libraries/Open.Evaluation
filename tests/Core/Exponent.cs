using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Open.Evaluation.Tests
{
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
			public OneCollapse() : base(FORMAT, null, RED) { }

			protected override double Expected => 1;
		}

		[TestClass]
		public class Division : ParseTestBase
		{
			const string FORMAT = "(2 * (({0} + {1})^-1))";
			public Division() : base(FORMAT) { }

			protected override double Expected => 2 / (PV[0] + PV[1]);
		}

		[TestClass]
		public class SquareRoot : ParseTestBase
		{
			const string FORMAT = "(2 * (({0} + {1})^0.5))";
			public SquareRoot() : base(FORMAT) { }

			protected override double Expected => 2 * Math.Sqrt(PV[0] + PV[1]);
		}

	}
}
