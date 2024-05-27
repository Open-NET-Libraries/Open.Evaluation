using Open.Evaluation.Arithmetic;

namespace Open.Evaluation.Tests;

public abstract class ParseTestBase
{
	protected readonly double[] PV = { 2, 3, 4, 5 };
	protected readonly EvaluationCatalog<double> Catalog;
	// ReSharper disable once NotAccessedField.Global
	protected readonly string Format;
	protected readonly string Representation;
	protected readonly string RepresentationResolved;
	protected readonly string Reduction;
	protected readonly string ReductionResolved;

	protected readonly IEvaluate<double> Evaluation;
	protected readonly IEvaluate<double> EvaluationReduced;

	protected ParseTestBase(string format, string? representation = null, string? reduction = null)
	{
		Format = format ?? throw new ArgumentNullException(nameof(format));
		Representation = representation ?? format;
		RepresentationResolved = string.Format(Representation, PV.Cast<object>().ToArray());
		Reduction = reduction ?? Representation;
		ReductionResolved = reduction is null ? RepresentationResolved : string.Format(reduction, PV.Cast<object>().ToArray());
		Catalog = new EvaluationCatalog<double>();
		Evaluation = Catalog.Parse(format);
		EvaluationReduced = Catalog.GetReduced(Evaluation);
	}

	protected abstract double Expected { get; }

	[TestMethod, Description("Compares the parsed evalution to the expected value.")]
	public void Evaluate()
	{
		using var lease = Context.Rent();
		var context = lease.Item.Init(Catalog, PV);
		Evaluation.Evaluate(context).Result
			.Should().Be(Expected);

		if (Reduction == Representation)
		{
			EvaluationReduced.Description.Value
				.Should().Be(Evaluation.Description.Value);
		}
		else
		{
			EvaluationReduced.Description.Value
				.Should().NotBe(Evaluation.Description.Value);

			EvaluationReduced.Evaluate(context).Result
				.Should().Be(Expected);
		}
	}

	[TestMethod, Description("Compares the parsed evalution .ToString(context) to the actual formatted string.")]
	public void ToStringValues()
	{
		using var lease = Context.Rent();
		var context = lease.Item.Init(Catalog, PV);

		Evaluation.Evaluate(context).Description.Value
			.Should().Be(RepresentationResolved);

		if (Reduction != Representation)
		{
			EvaluationReduced.Evaluate(context).Description.Value
				.Should().Be(ReductionResolved);
		}
	}

	[TestMethod, Description("Compares the parsed evalution .ToStringRepresentation() to the provided format string.")]
	public void ToStringRepresentation()
	{
		Evaluation.Description.Value
			.Should().Be(Representation);

		if (Reduction != Representation)
		{
			EvaluationReduced.Description.Value
				.Should().Be(Reduction);
		}
	}
}
