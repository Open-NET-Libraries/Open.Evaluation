using Microsoft.VisualStudio.TestTools.UnitTesting;
using Open.Evaluation.Arithmetic;
using Open.Evaluation.Catalogs;
using Open.Evaluation.Core;

namespace Open.Evaluation.Tests
{
	[TestClass]
	public class Constant
	{
		[TestMethod]
		public void Instantiation()
		{
			using var catalog = new EvaluationCatalog<double>();
			catalog.GetConstant(5).ValidateValue(5);
		}

		[TestMethod]
		public void Sum()
		{
			using var catalog = new EvaluationCatalog<double>();

			catalog
				.SumOfConstants(5, catalog.GetConstant(4))
				.ValidateValue(9);

			catalog
				.SumOf(catalog.GetConstant(5), catalog.GetConstant(4))
				.ValidateValue(9);
		}

		[TestMethod]
		public void Product()
		{
			using var catalog = new EvaluationCatalog<double>();

			catalog
				.ProductOfConstants(5, catalog.GetConstant(4))
				.ValidateValue(20);

			catalog
				.ProductOf(catalog.GetConstant(5), catalog.GetConstant(4))
				.ValidateValue(20);
		}
	}
}
