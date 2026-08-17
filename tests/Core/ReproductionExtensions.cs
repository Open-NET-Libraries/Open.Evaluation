using Open.Evaluation.Boolean;

namespace Open.Evaluation.Tests.Core;

[TestClass]
public class ReproductionExtensionsTests
{
	static (EvaluationCatalog<bool> Catalog, IEvaluate<bool> P0, IEvaluate<bool> P1, IEvaluate<bool> P2, And And) Setup()
	{
		var catalog = new EvaluationCatalog<bool>();
		var p0 = catalog.GetParameter(0);
		var p1 = catalog.GetParameter(1);
		var p2 = catalog.GetParameter(2);
		var and = catalog.And([p0, p1, p2]);
		return (catalog, p0, p1, p2, and);
	}

	[TestMethod]
	public void NewWithIndexRemoved_WithCatalog_DropsTheChildAtThatIndex()
	{
		var (catalog, p0, _, p2, and) = Setup();

		var result = and.NewWithIndexRemoved<And, IEvaluate<bool>, IEvaluate<bool>>(catalog, 1);

		((And)result).Children.Should().BeEquivalentTo([p0, p2]);
	}

	[TestMethod]
	public void NewWithIndexRemoved_InstanceOverload_UsesTargetsOwnCatalog()
	{
		var (_, p0, _, p2, and) = Setup();

		var result = and.NewWithIndexRemoved<And, IEvaluate<bool>, IEvaluate<bool>>(1);

		((And)result).Children.Should().BeEquivalentTo([p0, p2]);
	}

	[TestMethod]
	public void NewWithIndexReplaced_SubstitutesTheChildAtThatIndex()
	{
		var (catalog, p0, _, p2, and) = Setup();
		var replacement = catalog.GetParameter(9);

		var result = and.NewWithIndexReplaced<And, IEvaluate<bool>, IEvaluate<bool>>(catalog, 1, replacement);

		((And)result).Children.Should().BeEquivalentTo([p0, p2, replacement]);
	}

	[TestMethod]
	public void NewWithAppended_Enumerable_AddsTrailingChildren()
	{
		var (catalog, p0, p1, p2, and) = Setup();
		var extra = catalog.GetParameter(9);

		var result = and.NewWithAppended<And, IEvaluate<bool>, IEvaluate<bool>>(catalog, [extra]);

		((And)result).Children.Should().BeEquivalentTo([p0, p1, p2, extra]);
	}

	[TestMethod]
	public void NewWithAppended_ParamsOverload_AddsTrailingChildren()
	{
		var (catalog, p0, p1, p2, and) = Setup();
		var extra1 = catalog.GetParameter(9);
		var extra2 = catalog.GetParameter(10);

		var result = and.NewWithAppended<And, IEvaluate<bool>, IEvaluate<bool>>(catalog, extra1, extra2);

		((And)result).Children.Should().BeEquivalentTo([p0, p1, p2, extra1, extra2]);
	}

	[TestMethod]
	public void NewUsing_ChildPlusParamsRest_BuildsFromThoseChildrenOnly()
	{
		var (catalog, p0, p1, _, and) = Setup();

		var result = and.NewUsing<And, IEvaluate<bool>, IEvaluate<bool>>(catalog, p0, p1);

		((And)result).Children.Should().BeEquivalentTo([p0, p1]);
	}

	[TestMethod]
	public void NewUsing_ChildPlusParamsRest_InstanceOverload_UsesTargetsOwnCatalog()
	{
		var (_, p0, p1, _, and) = Setup();

		var result = and.NewUsing<And, IEvaluate<bool>, IEvaluate<bool>>(p0, p1);

		((And)result).Children.Should().BeEquivalentTo([p0, p1]);
	}

	[TestMethod]
	public void NewWithIndexReplaced_InstanceOverload_UsesTargetsOwnCatalog()
	{
		var (_, p0, _, p2, and) = Setup();
		var replacement = ((And)and).Catalog.GetParameter(9);

		var result = and.NewWithIndexReplaced<And, IEvaluate<bool>, IEvaluate<bool>>(1, replacement);

		((And)result).Children.Should().BeEquivalentTo([p0, p2, replacement]);
	}

	[TestMethod]
	public void NewWithAppended_Enumerable_InstanceOverload_UsesTargetsOwnCatalog()
	{
		var (_, p0, p1, p2, and) = Setup();
		var extra = ((And)and).Catalog.GetParameter(9);

		var result = and.NewWithAppended<And, IEvaluate<bool>, IEvaluate<bool>>([extra]);

		((And)result).Children.Should().BeEquivalentTo([p0, p1, p2, extra]);
	}

	[TestMethod]
	public void NewWithAppended_ParamsOverload_InstanceOverload_UsesTargetsOwnCatalog()
	{
		var (_, p0, p1, p2, and) = Setup();
		var catalog = ((And)and).Catalog;
		var extra1 = catalog.GetParameter(9);
		var extra2 = catalog.GetParameter(10);

		var result = and.NewWithAppended<And, IEvaluate<bool>, IEvaluate<bool>>(extra1, extra2);

		((And)result).Children.Should().BeEquivalentTo([p0, p1, p2, extra1, extra2]);
	}
}
