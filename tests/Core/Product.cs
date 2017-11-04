using Microsoft.VisualStudio.TestTools.UnitTesting;
using Open.Evaluation.Arithmetic;
using Open.Evaluation.Core;
using System.Linq;

namespace Open.Evaluation.Tests
{
	[TestClass]
	public class ProductTests
	{
		const string FORMAT = "(({0} + {1}) * ({2} + {3}))";
		readonly double[] PV = new double[] { 2, 3, 4, 5 };

		readonly IEvaluate<double> Evaluation;

		public ProductTests()
		{
			var catalog = new EvaluateDoubleCatalog();
			var e = catalog.SumOf(
				catalog.GetParameter(0),
				catalog.GetParameter(1));

			var f = catalog.SumOf(
				catalog.GetParameter(2),
				catalog.GetParameter(3));

			Evaluation = catalog
				.ProductOf(e, f);
		}


		[TestMethod]
		public void Product_Evaluate()
		{
			Assert.AreEqual(
				(PV[0] + PV[1]) * (PV[2] + PV[3]),
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
