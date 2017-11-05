using Microsoft.VisualStudio.TestTools.UnitTesting;
using Open.Evaluation.Core;
using System;
using System.Linq;

namespace Open.Evaluation.Tests
{

	public abstract class ParseTestBase
	{
		protected readonly double[] PV = new double[] { 2, 3, 4, 5 };
		protected readonly EvaluateDoubleCatalog Catalog;
		protected readonly string Format;
		protected readonly string Representation;

		protected readonly IEvaluate<double> Evaluation;

		protected ParseTestBase(string format, string representation = null)
		{
			Format = format ?? throw new ArgumentNullException("format");
			Representation = representation ?? format;
			Catalog = new EvaluateDoubleCatalog();
			Evaluation = Catalog.Parse(format);
		}

		protected abstract double Expected { get; }

		[TestMethod, Description("Compares the parsed evalution to the expected value.")]
		public void Evaluate()
		{
			Assert.AreEqual(
				Expected,
				Evaluation.Evaluate(PV),
				GetType() + ".Evaluate() failed.");
		}

		[TestMethod, Description("Compares the parsed evalution .ToString(context) to the actual formatted string.")]
		public void ToStringValues()
		{
			Assert.AreEqual(
				string.Format(Representation, PV.Cast<object>().ToArray()),
				Evaluation.ToString(PV),
				GetType()+ ".ToStringValues() failed.");
		}

		[TestMethod, Description("Compares the parsed evalution .ToStringRepresentation() to the provided format string.")]
		public void ToStringRepresentation()
		{
			Assert.AreEqual(
				Representation,
				Evaluation.ToStringRepresentation(),
				GetType() + ".ToStringRepresentation() failed.");
		}

	}
}
