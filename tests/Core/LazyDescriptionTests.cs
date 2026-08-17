using Open.Evaluation.Arithmetic;

namespace Open.Evaluation.Tests.Core;

/// <summary>
/// Pins only the OBSERVABLE semantics of the lazy description contract: the description text is
/// correct, resolvable after evaluation completes (and after the context that produced the result
/// has been recycled), and safe to read concurrently. Deliberately does NOT assert anything about
/// allocation behavior or the laziness mechanics themselves.
/// </summary>
[TestClass]
public class LazyDescriptionTests
{
	[TestMethod]
	public void Description_ResolvableAfterEvaluationCompletes()
	{
		using var catalog = new EvaluationCatalog<double>();
		var sum = catalog.SumOf(catalog.GetParameter(0), catalog.GetParameter(1));

		using var context = new Context();
		context.Init(catalog, (ReadOnlySpan<double>)[2d, 3d]);
		var result = sum.Evaluate(context);

		// result.Description is the VALUE-RESOLVED description (actual parameter values
		// substituted); sum.Description is the node's own unparameterized template.
		result.Description.Value.Should().Be("(2 + 3)");
		sum.Description.Value.Should().Be("({0} + {1})");
	}

	[TestMethod]
	public void Description_ResolvableAfterContextReturnedToPool()
	{
		using var catalog = new EvaluationCatalog<double>();
		var sum = catalog.SumOf(catalog.GetParameter(0), catalog.GetParameter(1));

		EvaluationResult<double> result;
		using (var lease = Context.Rent())
		{
			var context = lease.Item.Init(catalog, (ReadOnlySpan<double>)[2d, 3d]);
			result = sum.Evaluate(context);
		} // The context is cleared and returned to the shared pool here.

		// The captured EvaluationResult's Lazy<string> Description must still resolve correctly
		// even though the Context that produced it is gone.
		result.Description.Value.Should().Be("(2 + 3)");
	}

	[TestMethod]
	public void Description_ThreadSafeToReadConcurrently()
	{
		using var catalog = new EvaluationCatalog<double>();
		var sum = catalog.SumOf(catalog.GetParameter(0), catalog.GetParameter(1), catalog.GetParameter(2));

		var results = new string[16];
		Parallel.For(0, results.Length, i => results[i] = sum.Description.Value);

		results.Should().OnlyContain(r => r == "({0} + {1} + {2})");
	}

	[TestMethod]
	public void EvaluationResultDescription_ThreadSafeToReadConcurrently()
	{
		using var catalog = new EvaluationCatalog<double>();
		var sum = catalog.SumOf(catalog.GetParameter(0), catalog.GetParameter(1));

		using var context = new Context();
		context.Init(catalog, (ReadOnlySpan<double>)[2d, 3d]);
		var result = sum.Evaluate(context);

		var texts = new string[16];
		Parallel.For(0, texts.Length, i => texts[i] = result.Description.Value);

		texts.Should().OnlyContain(t => t == "(2 + 3)");
	}
}
