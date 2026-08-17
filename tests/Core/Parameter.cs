namespace Open.Evaluation.Tests.Core;

[TestClass]
public class Parameter
{
	[TestMethod]
	public void Instantiation()
	{
		using var catalog = new EvaluationCatalog<double>();
		catalog.GetParameter(5).Id.Should().Be(5);
	}

	[TestMethod]
	public void NewUsing_WithCatalog_ReturnsInternedParameterForThatId()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p0 = (global::Open.Evaluation.Core.Parameter<double>)catalog.GetParameter(0);

		var p5 = p0.NewUsing(catalog, 5);

		p5.Id.Should().Be(5);
		ReferenceEquals(p5, catalog.GetParameter(5)).Should().BeTrue();
	}

	[TestMethod]
	public void NewUsing_InstanceOverload_UsesOwnCatalog()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p0 = (global::Open.Evaluation.Core.Parameter<double>)catalog.GetParameter(0);

		var p5 = p0.NewUsing(5);

		ReferenceEquals(p5, catalog.GetParameter(5)).Should().BeTrue();
	}

	[TestMethod]
	public void NewUsing_ExplicitInterfaceImplementations_ProduceInternedParameter()
	{
		using var catalog = new EvaluationCatalog<double>();
		IReproducable<ushort, IEvaluate<double>> p0 = (global::Open.Evaluation.Core.Parameter<double>)catalog.GetParameter(0);

		var viaCatalog = p0.NewUsing(catalog, 6);
		var viaInstance = p0.NewUsing(7);

		ReferenceEquals(viaCatalog, catalog.GetParameter(6)).Should().BeTrue();
		ReferenceEquals(viaInstance, catalog.GetParameter(7)).Should().BeTrue();
	}

	[TestMethod]
	[Ignore("QUESTION FOR AUTHOR: Parameter<T>.EvaluateInternal's own " +
		"`throw new InvalidOperationException($\"Parameter {Id} result not found in the context.\")` " +
		"branch appears to be unreachable dead code. Context.GetOrAdd's parameterless-factory overload " +
		"stores the wrapper Lazy<IEvaluationResult> into _registry BEFORE invoking the factory, so by " +
		"the time EvaluateInternal calls context.TryGetResult(this, ...), the key is already present " +
		"(pointing back at the very entry that's mid-computation). TryGetResult then recursively " +
		"touches that same Lazy<T>, and System.Lazy<T> (ExecutionAndPublication mode) throws ITS OWN " +
		"InvalidOperationException ('ValueFactory attempted to access the Value property of this " +
		"instance.') before Parameter's friendly message is ever reached. Observed directly: evaluating " +
		"an unbound Parameter (never populated through AddParam/Init/Add) throws " +
		"System.InvalidOperationException with the Lazy<T> recursion message, never the 'Parameter {id} " +
		"result not found in the context.' message. Is the friendly message meant to be reachable? If " +
		"so, does Context.GetOrAdd's wrapper-Lazy need to register the computed value AFTER the factory " +
		"runs rather than a self-referencing wrapper before it runs?")]
	public void Evaluate_UnboundParameter_ThrowsFromLazyRecursion_NotParametersOwnMessage()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		using var context = new Context();

		Action act = () => p0.Evaluate(context);

		act.Should().Throw<InvalidOperationException>()
			.WithMessage("*ValueFactory attempted to access the Value property*");
	}
}
