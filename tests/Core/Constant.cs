using Microsoft.VisualStudio.TestTools.UnitTesting;
using Open.Evaluation.Arithmetic;
using Open.Evaluation.Core;

namespace Open.Evaluation.Tests
{
    [TestClass]
    public class ConstantTests
    {
        [TestMethod]
        public void Constant_Instantiation()
        {
			using (var catalog = new EvaluateDoubleCatalog())
			{
				catalog.GetConstant(5).ValidateValue(5);
			}
        }

		[TestMethod]
		public void Constant_Sum()
		{
			using (var catalog = new EvaluateDoubleCatalog())
			{
				catalog
					.SumOfConstants(5, catalog.GetConstant(4))
					.ValidateValue(9);

				catalog
					.SumOf(catalog.GetConstant(5), catalog.GetConstant(4))
					.ValidateValue(9);

			}
		}

		[TestMethod]
		public void Constant_Product()
		{
			using (var catalog = new EvaluateDoubleCatalog())
			{
				catalog
					.ProductOfConstants(5, catalog.GetConstant(4))
					.ValidateValue(20);

				catalog
					.ProductOf(catalog.GetConstant(5), catalog.GetConstant(4))
					.ValidateValue(20);
			}
		}
	}
}
