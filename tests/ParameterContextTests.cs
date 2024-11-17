namespace Open.Evaluation.Tests;

public static class ParameterContextTests
{
	[TestMethod]
	public static void GetOrAddTest()
	{
		var catalog = new EvaluationCatalog<double>();
		// Shouldn't throw.
		using Disposable.RecycleHelper<Context> lease = Context.Rent();
        Context context = lease.Item;

        Constant<double> c0 = catalog.GetConstant(0);
		context.GetOrAdd(c0, 0d);
        Constant<double> c1 = catalog.GetConstant(1);
		context.GetOrAdd(c1, 1d);
	}
}
