using Microsoft.VisualStudio.TestTools.UnitTesting;
using Open.Evaluation.Core;

namespace Open.Evaluation.Tests
{
	public static class Validation
    {
		public static void ValidateValue(this IEvaluate<double> e, double value)
		{
			Assert.AreEqual(value, e.Evaluate(null));
		}
	}
}
