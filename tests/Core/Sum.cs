using Microsoft.VisualStudio.TestTools.UnitTesting;
using Open.Evaluation.Arithmetic;
using Open.Evaluation.Core;
using System.Linq;

namespace Open.Evaluation.Tests
{
	[TestClass]
	public class SumTests
	{
		const string FORMAT = "(({0} * {1}) + ({2} * {3}))";
		readonly double[] PV = new double[] { 2, 3, 4, 5 };

		readonly IEvaluate<double> Evaluation;

		public SumTests()
		{
			var catalog = new EvaluateDoubleCatalog();
			var e = catalog.ProductOf(
				catalog.GetParameter(0),
				catalog.GetParameter(1));

			var f = catalog.ProductOf(
				catalog.GetParameter(2),
				catalog.GetParameter(3));

			Evaluation = catalog
				.SumOf(e, f);
		}


		[TestMethod]
		public void Sum_Evaluate()
		{
			Assert.AreEqual(
				(PV[0] * PV[1]) + (PV[2] * PV[3]),
				Evaluation.Evaluate(PV));
		}

		[TestMethod]
		public void Sum_ToString()
		{
			Assert.AreEqual(
				string.Format(FORMAT, PV.Cast<object>().ToArray()),
				Evaluation.ToString(PV));
		}

		[TestMethod]
		public void Sum_ToStringRepresentation()
		{
			Assert.AreEqual(
				FORMAT,
				Evaluation.ToStringRepresentation());
		}

	}
}
