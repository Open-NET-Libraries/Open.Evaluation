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

	}
}
