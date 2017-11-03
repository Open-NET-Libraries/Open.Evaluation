using Microsoft.VisualStudio.TestTools.UnitTesting;
using Open.Evaluation.Arithmetic;
using Open.Evaluation.Core;
using System;

namespace Open.Evaluation.Tests
{
	[TestClass]
	public class ExponentTests
	{
		[TestMethod]
		public void Exponent()
		{
			using (var catalog = new EvaluateDoubleCatalog())
			{
				var e = catalog.SumOf(
					catalog.GetParameter(0),
					catalog.GetParameter(1));

				var f = catalog.SumOf(
					catalog.GetParameter(2),
					catalog.GetParameter(3));

				var s = catalog
					.GetExponent(e, f);

				var p = new double[] { 2, 3, 4, 5 };
				var expected = Math.Pow( p[0] + p[1], p[2] + p[3] );

				Assert.AreEqual(expected, s.Evaluate(p));
			}
		}
	}
}
