namespace Open.Evaluation.Tests;

[TestClass]
public class Parameter
{
	[TestMethod]
	public void Instantiation()
	{
		using var catalog = new EvaluationCatalog<double>();
		catalog.GetParameter(5).Id.Should().Be(5);
	}
}
