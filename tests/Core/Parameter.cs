using Microsoft.VisualStudio.TestTools.UnitTesting;
using Open.Evaluation.Core;

namespace Open.Evaluation.Tests
{
    [TestClass]
    public class ParameterTests
    {
        [TestMethod]
        public void Parameter_Instantiation()
        {
			using (var catalog = new EvaluateDoubleCatalog())
			{
				Assert.AreEqual((ushort)5, catalog.GetParameter(5).ID);
			}
        }
    }
}
