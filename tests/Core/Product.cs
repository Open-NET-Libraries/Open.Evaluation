using Microsoft.VisualStudio.TestTools.UnitTesting;
using Open.Evaluation.Core;
using System.Linq;

namespace Open.Evaluation.Tests
{
	[TestClass]
	public class ProductTests
	{
		const string FORMAT = "(({0} + {1} + 2) * ({0} + {1}) * ({2} + {3}))";
		readonly double[] PV = new double[] { 2, 3, 4, 5 };

		readonly IEvaluate<double> Evaluation;

		public ProductTests()
		{
			var catalog = new EvaluateDoubleCatalog();
			Evaluation = catalog.Parse(FORMAT);
		}

		[TestMethod]
		public void Product_Evaluate()
		{
			var x1 = PV[0] + PV[1];
			var x2 = PV[2] + PV[3];
			var x3 = x1 + 2;
			Assert.AreEqual(
				x1 * x2 * x3,
				Evaluation.Evaluate(PV));
		}

		[TestMethod]
		public void Product_ToString()
		{
			Assert.AreEqual(
				string.Format(FORMAT, PV.Cast<object>().ToArray()),
				Evaluation.ToString(PV));
		}

		[TestMethod]
		public void Product_ToStringRepresentation()
		{
			Assert.AreEqual(
				FORMAT,
				Evaluation.ToStringRepresentation());
		}
		
	}
}
