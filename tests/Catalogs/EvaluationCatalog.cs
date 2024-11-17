using Open.Evaluation.Arithmetic;

namespace Open.Evaluation.Tests.Catalogs;

[TestClass]
public class EvaluationCatalog
{
	[TestMethod]
	public void FixHierarchy()
	{
		using var catalog = new EvaluationCatalog<double>();
        IEvaluate<double>[] branches = Enumerable
			.Range(0, 3)
			.Select(i => catalog.ProductOf(
				catalog.GetParameter(i),
				catalog.GetParameter(i + 1)
			))
			.ToArray();

        IEvaluate<double> group01 = catalog.SumOf(branches);

        IEvaluate<double> group02 = catalog.SumOf(branches.Skip(1));

        IEvaluate<double> group03 = catalog.ProductOf(group01, group02);

        IEvaluate<double> group04 = catalog.SumOf(
			group03,
			catalog.GetParameter(0),
			catalog.GetConstant(5));

        Open.Hierarchy.Node<IEvaluate<double>> map = catalog.Factory.Map(group04);

		map.RemoveAt(2);
		map.AddValue(branches[1], true);

        Open.Hierarchy.Node<IEvaluate<double>> result = catalog.FixHierarchy(map);

		Assert.AreEqual(
			"(((({0} * {1}) + ({1} * {2}) + ({2} * {3})) * (({1} * {2}) + ({2} * {3}))) + ({1} * {2}) + {0})",
			result.Value.Description.Value);
	}
}
