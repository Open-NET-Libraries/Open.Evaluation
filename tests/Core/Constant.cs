using Open.Evaluation.Arithmetic;

namespace Open.Evaluation.Tests.Core;

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

	[TestMethod]
	public void NewUsing_WithCatalog_ReturnsInternedConstantForThatValue()
	{
		using var catalog = new EvaluationCatalog<double>();
		var c5 = (global::Open.Evaluation.Core.Constant<double>)catalog.GetConstant(5d);

		var c9 = c5.NewUsing(catalog, 9d);

		c9.Value.Should().Be(9d);
		ReferenceEquals(c9, catalog.GetConstant(9d)).Should().BeTrue();
	}

	[TestMethod]
	public void NewUsing_InstanceOverload_UsesOwnCatalog()
	{
		using var catalog = new EvaluationCatalog<double>();
		var c5 = (global::Open.Evaluation.Core.Constant<double>)catalog.GetConstant(5d);

		var c9 = c5.NewUsing(9d);

		ReferenceEquals(c9, catalog.GetConstant(9d)).Should().BeTrue();
	}

	[TestMethod]
	public void NewUsing_ExplicitInterfaceImplementations_ProduceInternedConstant()
	{
		using var catalog = new EvaluationCatalog<double>();
		IReproducable<double, IEvaluate<double>> c5 = (global::Open.Evaluation.Core.Constant<double>)catalog.GetConstant(5d);

		var viaCatalog = c5.NewUsing(catalog, 11d);
		var viaInstance = c5.NewUsing(13d);

		ReferenceEquals(viaCatalog, catalog.GetConstant(11d)).Should().BeTrue();
		ReferenceEquals(viaInstance, catalog.GetConstant(13d)).Should().BeTrue();
	}
}
