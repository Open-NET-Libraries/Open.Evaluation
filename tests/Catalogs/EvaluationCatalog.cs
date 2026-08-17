using Open.Evaluation.Arithmetic;
using Open.Hierarchy;

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

	[TestMethod]
	public void FixHierarchy_WithExponentNode_RebuildsViaTwoElementTupleReproduction()
	{
		// Sum/Product's children reconstruct via IReproducable<IEnumerable<...>>, exercised above.
		// Exponent reconstructs via the two-element-tuple IReproducable<(base, power)> overload -
		// a structurally different path through FixHierarchy that needs its own coverage. Directly
		// mutating the Exponent node's power child (RemoveAt + AddValue) marks it "mapped" (dirty)
		// so FixHierarchy actually rebuilds it, rather than taking the Unmapped fast-path.
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		var p1 = catalog.GetParameter(1);
		var exponent = catalog.GetExponent(p0, catalog.GetConstant(3d));
		var sum = catalog.SumOf(exponent, p1);

		Open.Hierarchy.Node<IEvaluate<double>> map = catalog.Factory.Map(sum);
		try
		{
			Open.Hierarchy.Node<IEvaluate<double>> exponentNode = map
				.GetDescendantsOfType()
				.Single(n => n.Value is Exponent<double>);

			exponentNode.RemoveAt(1); // remove the power child (constant 3).
			exponentNode.AddValue(catalog.GetConstant(4d), true); // replace it with power 4.

			Open.Hierarchy.Node<IEvaluate<double>> result = catalog.FixHierarchy(map);
			try
			{
				using var lease = Context.Rent();
				var context = lease.Item.Init(catalog, (ReadOnlySpan<double>)[2d, 5d]);
				result.Value!.Evaluate(context).Result.Should().Be(21d, "2^4 + 5");
			}
			finally
			{
				if (result != map) result.Recycle();
			}
		}
		finally
		{
			map.Recycle();
		}
	}
}
