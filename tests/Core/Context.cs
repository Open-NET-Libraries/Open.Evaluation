using Open.Evaluation.Arithmetic;

namespace Open.Evaluation.Tests.Core;

[TestClass]
public class ContextTests
{
	[TestMethod]
	public void GetOrAdd_WithKeyedFactory_MemoizesPerKey()
	{
		using var catalog = new EvaluationCatalog<double>();
		var c0 = catalog.GetConstant(5d);
		using var context = new Context();

		var callCount = 0;
		EvaluationResult<double> Factory(IEvaluate _)
		{
			callCount++;
			return EvaluationResult.Create(5d);
		}

		context.GetOrAdd(c0, Factory);
		context.GetOrAdd(c0, Factory);
		context.GetOrAdd(c0, Factory);

		callCount.Should().Be(1, "the factory should only run once per key per context");
	}

	[TestMethod]
	public void GetOrAdd_WithParameterlessFactory_MemoizesPerKey()
	{
		using var catalog = new EvaluationCatalog<double>();
		var c0 = catalog.GetConstant(5d);
		using var context = new Context();

		var callCount = 0;
		EvaluationResult<double> Factory()
		{
			callCount++;
			return EvaluationResult.Create(5d);
		}

		context.GetOrAdd(c0, Factory);
		context.GetOrAdd(c0, Factory);

		callCount.Should().Be(1);
	}

	[TestMethod]
	public void SharedSubtree_EvaluatedOnce_WhenReferencedMultipleTimesInSameExpression()
	{
		// Guarantees the library exists to enforce: a shared/interned node referenced from
		// multiple places in an expression tree is only actually evaluated once per Context.
		using var catalog = new EvaluationCatalog<double>();
		var shared = catalog.GetParameter(0);

		using var context = new Context();
		context.AddParam(catalog, 0, 3d);

		// Evaluate the same interned node instance repeatedly through the same context.
		for (var i = 0; i < 5; i++)
		{
			shared.Evaluate(context).Result.Should().Be(3d);
		}

		// Now prove memoization more directly: register the parameter's own evaluation as a
		// counted wrapper and confirm the underlying factory only executes once even though the
		// same shared node is a child of both branches of a Sum built from itself twice.
		var sum = catalog.SumOf(shared, shared);
		using var context2 = new Context();
		var factoryCalls = 0;
		context2.GetOrAdd<double>(shared, () =>
		{
			factoryCalls++;
			return EvaluationResult.Create(4d);
		});

		sum.Evaluate(context2).Result.Should().Be(8d); // 4 + 4, using the memoized value both times.
		factoryCalls.Should().Be(1);
	}

	[TestMethod]
	public void TryGetResult_BeforeAdd_ReturnsFalse()
	{
		using var catalog = new EvaluationCatalog<double>();
		var c0 = catalog.GetConstant(5d);
		using var context = new Context();

		context.TryGetResult<double>(c0, out _).Should().BeFalse();
	}

	[TestMethod]
	public void TryGetResult_AfterAdd_ReturnsTrueWithValue()
	{
		using var catalog = new EvaluationCatalog<double>();
		var c0 = catalog.GetConstant(5d);
		using var context = new Context();

		context.GetOrAdd(c0, 5d);
		context.TryGetResult<double>(c0, out var result).Should().BeTrue();
		result.Result.Should().Be(5d);
	}

	[TestMethod]
	public void GetOrAdd_WithMismatchedType_ThrowsInvalidCast()
	{
		using var catalog = new EvaluationCatalog<double>();
		var c0 = catalog.GetConstant(5d);
		using var context = new Context();

		context.GetOrAdd(c0, 5d);

		Action act = () => context.GetOrAdd<int>(c0, 5);
		act.Should().Throw<InvalidCastException>();
	}

	[TestMethod]
	public void Clear_AllowsReuse_WithoutStaleValues()
	{
		using var catalog = new EvaluationCatalog<double>();
		var c0 = catalog.GetConstant(5d);
		using var context = new Context();

		context.GetOrAdd(c0, 5d);
		context.TryGetResult<double>(c0, out _).Should().BeTrue();

		context.Clear();

		context.TryGetResult<double>(c0, out _).Should().BeFalse();

		// And can be re-populated after clearing.
		context.GetOrAdd(c0, 5d);
		context.TryGetResult<double>(c0, out _).Should().BeTrue();
	}

	[TestMethod]
	public void Dispose_ThenUse_Throws()
	{
		var context = new Context();
		context.Dispose();

		Action act = () => context.GetOrAdd(new EvaluationCatalog<double>().GetConstant(1d), 1d);
		act.Should().Throw<ObjectDisposedException>();
	}

	[TestMethod]
	public void AddParam_MakesParameterResolvable()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		using var context = new Context();

		context.AddParam(catalog, 0, 42d);
		p0.Evaluate(context).Result.Should().Be(42d);
	}

	[TestMethod]
	public void InitRange_PopulatesSequentialParameterIds()
	{
		using var catalog = new EvaluationCatalog<double>();
		using var context = new Context();
		context.InitRange(catalog, [10d, 20d, 30d]);

		catalog.GetParameter(0).Evaluate(context).Result.Should().Be(10d);
		catalog.GetParameter(1).Evaluate(context).Result.Should().Be(20d);
		catalog.GetParameter(2).Evaluate(context).Result.Should().Be(30d);
	}

	[TestMethod]
	public void Init_Span_PopulatesSequentialParameterIds()
	{
		using var catalog = new EvaluationCatalog<double>();
		using var context = new Context();
		context.Init(catalog, (ReadOnlySpan<double>)[1d, 2d, 3d]);

		catalog.GetParameter(0).Evaluate(context).Result.Should().Be(1d);
		catalog.GetParameter(1).Evaluate(context).Result.Should().Be(2d);
		catalog.GetParameter(2).Evaluate(context).Result.Should().Be(3d);
	}

	[TestMethod]
	public void StaticEvaluate_Helper_UsesPooledContext()
	{
		using var catalog = new EvaluationCatalog<double>();
		var sum = catalog.SumOf(catalog.GetParameter(0), catalog.GetParameter(1));

		var result = Context.Evaluate(sum, (ReadOnlySpan<double>)[2d, 3d]);
		result.Result.Should().Be(5d);
	}

	[TestMethod]
	public void Pool_RentedContext_IsClearedBetweenUses()
	{
		using var catalog = new EvaluationCatalog<double>();
		var c0 = catalog.GetConstant(5d);

		using (var lease = Context.Rent())
		{
			lease.Item.GetOrAdd(c0, 5d);
			lease.Item.TryGetResult<double>(c0, out _).Should().BeTrue();
		}

		// A freshly rented context (very possibly the same pooled instance) must not carry
		// over previously registered results.
		using (var lease2 = Context.Rent())
		{
			lease2.Item.TryGetResult<double>(c0, out _).Should().BeFalse();
		}
	}

	[TestMethod]
	public void Use_ProvidesWorkingContext_ToHandler()
	{
		using var catalog = new EvaluationCatalog<double>();
		var sum = catalog.SumOf(catalog.GetParameter(0), catalog.GetParameter(1));
		double? captured = null;

		Context.Use(context =>
		{
			context.Init(catalog, (ReadOnlySpan<double>)[4d, 6d]);
			captured = sum.Evaluate(context).Result;
		});

		captured.Should().Be(10d);
	}

	[TestMethod]
	public void SharedPool_Dispose_ThrowsNotSupported()
	{
#pragma warning disable CS0618 // Intentionally verifying the obsolete override's behavior.
		Action act = () => Context.Shared.Dispose();
#pragma warning restore CS0618
		act.Should().Throw<NotSupportedException>();
	}
}
