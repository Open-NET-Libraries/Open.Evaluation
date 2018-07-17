using Microsoft.VisualStudio.TestTools.UnitTesting;
using Open.Evaluation.Arithmetic;
using Open.Evaluation.Catalogs;
using Open.Evaluation.Core;
using System.Linq;

namespace Open.Evaluation.Tests.Catalogs
{
	[TestClass]
	public class EvaluationCatalog
	{

		[TestMethod]
		public void FixHierarchy()
		{
			using (var catalog = new EvaluationCatalog<double>())
			{
				var branches = Enumerable
					.Range(0, 3)
					.Select(i => catalog.ProductOf(
						catalog.GetParameter(i),
						catalog.GetParameter(i + 1)
					))
					.ToArray();

				var group01 = catalog.SumOf(branches);

				var group02 = catalog.SumOf(branches.Skip(1));

				var group03 = catalog.ProductOf(group01, group02);

				var group04 = catalog.SumOf(group03, catalog.GetParameter(0), catalog.GetConstant(5));

				var map = catalog.Factory.Map(group04);

				map.RemoveAt(2);
				map.AddValue(branches[1], true);

				var result = catalog.FixHierarchy(map);

				Assert.AreEqual(
					"(((({0} * {1}) + ({1} * {2}) + ({2} * {3})) * (({1} * {2}) + ({2} * {3}))) + ({1} * {2}) + {0})",
					result.Value.ToStringRepresentation());
			}
		}

	}
}
