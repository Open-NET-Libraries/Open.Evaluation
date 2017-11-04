using Microsoft.VisualStudio.TestTools.UnitTesting;
using Open.Evaluation.Core;
using System;
using System.Linq;

namespace Open.Evaluation.Tests
{
	[TestClass]
	public class ExponentTests
	{
		const string FORMAT = "(({0} + {1})^({2} + {3}))";
		readonly double[] PV = new double[] { 2, 3, 4, 5 };

		readonly IEvaluate<double> Evaluation;

		public ExponentTests()
		{
			var catalog = new EvaluateDoubleCatalog();
			Evaluation = catalog.Parse(FORMAT);
		}

		[TestMethod]
		public void Exponent_Evaluate()
		{
			var x1 = PV[0] + PV[1];
			var x2 = PV[2] + PV[3];
			Assert.AreEqual(
				Math.Pow(x1, x2),
				Evaluation.Evaluate(PV));
		}

		[TestMethod]
		public void Exponent_ToString()
		{
			Assert.AreEqual(
				string.Format(FORMAT, PV.Cast<object>().ToArray()),
				Evaluation.ToString(PV));
		}

		[TestMethod]
		public void Exponent_ToStringRepresentation()
		{
			Assert.AreEqual(
				FORMAT,
				Evaluation.ToStringRepresentation());
		}

	}
}
