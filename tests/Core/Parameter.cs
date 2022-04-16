using Microsoft.VisualStudio.TestTools.UnitTesting;
using Open.Evaluation.Catalogs;
using Open.Evaluation.Core;

namespace Open.Evaluation.Tests;

[TestClass]
public class Parameter
{
	[TestMethod]
	public void Instantiation()
	{
		using var catalog = new EvaluationCatalog<double>();
		Assert.AreEqual((ushort)5, catalog.GetParameter(5).ID);
	}
}
