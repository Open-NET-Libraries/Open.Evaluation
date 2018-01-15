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
		protected readonly string Reduction;

		protected readonly IEvaluate<double> Evaluation;
		protected readonly IEvaluate<double> EvaluationReduced;

		protected ParseTestBase(string format, string representation = null, string reduction = null)
		{
			Format = format ?? throw new ArgumentNullException(nameof(format));
			Representation = representation ?? format;
			Reduction = reduction ?? Representation;
			Catalog = new EvaluateDoubleCatalog();
			Evaluation = Catalog.Parse(format);
			EvaluationReduced = Catalog.GetReduced(Evaluation);
		}

		protected abstract double Expected { get; }

		[TestMethod, Description("Compares the parsed evalution to the expected value.")]
		public void Evaluate()
		{
			Assert.AreEqual(
				Expected,
				Evaluation.Evaluate(PV),
				GetType() + ".Evaluate() failed.");

			if (Reduction == Representation)
			{
				Assert.AreEqual(Evaluation, EvaluationReduced, "The same format string has produced a reduction.");
			}
			else
			{
				Assert.AreNotEqual(Evaluation, EvaluationReduced, "No reduction occurred but there was one expected.");

				Assert.AreEqual(
					Expected,
					EvaluationReduced.Evaluate(PV),
					GetType() + ".Evaluate() reduced failed.");
			}

		}


		[TestMethod, Description("Compares the parsed evalution .ToString(context) to the actual formatted string.")]
		public void ToStringValues()
		{
			Assert.AreEqual(
				string.Format(Representation, PV.Cast<object>().ToArray()),
				Evaluation.ToString(PV),
				GetType() + ".ToStringValues() failed.");

			if (Reduction != Representation)
			{
				Assert.AreEqual(
					string.Format(Reduction, PV.Cast<object>().ToArray()),
					EvaluationReduced.ToString(PV),
					GetType() + ".ToStringValues() reduced failed.");
			}
		}

		[TestMethod, Description("Compares the parsed evalution .ToStringRepresentation() to the provided format string.")]
		public void ToStringRepresentation()
		{
			Assert.AreEqual(
				Representation,
				Evaluation.ToStringRepresentation(),
				GetType() + ".ToStringRepresentation() failed.");


			if (Reduction != Representation)
			{
				Assert.AreEqual(
					Reduction,
					EvaluationReduced.ToStringRepresentation(),
					GetType() + ".ToStringRepresentation() reduced failed.");
			}

		}

	}
}
