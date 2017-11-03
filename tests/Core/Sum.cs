using Microsoft.VisualStudio.TestTools.UnitTesting;
using Open.Evaluation.Arithmetic;
using Open.Evaluation.Core;

namespace Open.Evaluation.Tests
{
	[TestClass]
	public class SumTests
	{
		[TestMethod]
		public void Sum()
		{
			using (var catalog = new EvaluateDoubleCatalog())
			{
				var e = catalog.ProductOf(
					catalog.GetParameter(0),
					catalog.GetParameter(1));

				var f = catalog.ProductOf(
					catalog.GetParameter(2),
					catalog.GetParameter(3));

				var s = catalog
					.SumOf(e, f);

				var p = new double[] { 2, 3, 4, 5 };
				var expected = p[0] * p[1] + p[2] * p[3];

				Assert.AreEqual(expected, s.Evaluate(p));
			}
		}
	}
}
