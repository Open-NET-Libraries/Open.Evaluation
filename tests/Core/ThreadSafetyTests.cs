using Open.Evaluation.Arithmetic;

namespace Open.Evaluation.Tests.Core;

/// <summary>
/// Stress-tests the concurrent contract of <see cref="Context"/>'s memo table and
/// <see cref="Catalog{T}"/>'s interning registry. Every parallel workload here is wrapped in
/// <see cref="RunBounded"/> so that if a scenario turns out to deadlock/livelock, the TEST fails
/// fast with a clear message instead of hanging the whole run - which matters a great deal here,
/// see the documented QUESTION FOR AUTHOR below for a scenario that reproduces exactly that.
/// </summary>
[TestClass]
public class ThreadSafetyTests
{
	/// <summary>
	/// Runs <paramref name="action"/> on a background task and fails fast (rather than hanging the
	/// test run) if it doesn't complete within <paramref name="timeout"/>.
	/// </summary>
	private static void RunBounded(Action action, TimeSpan timeout)
	{
		var task = Task.Run(action);
		task.Wait(timeout).Should().BeTrue(
			$"the workload must complete within {timeout} - if it doesn't, that's a deadlock/livelock, not a slow pass");
	}

	[TestMethod]
	public void ConcurrentRegistration_SameCatalog_InterningHoldsUnderRace()
	{
		// Catalog<T>'s registry is a ConditionalWeakTable and its id pool is a ConcurrentDictionary
		// - both genuinely thread-safe BCL types - so this is expected to be safe.
		using var catalog = new EvaluationCatalog<double>();
		const int iterations = 200;
		var paramResults = new IParameter<double>[iterations];
		var sumResults = new IEvaluate<double>[iterations];

		RunBounded(() => Parallel.For(0, iterations, i =>
		{
			// Every worker asks for the exact same parameter id and the exact same sum shape -
			// the catalog must serialize/interning-dedupe this correctly under contention.
			paramResults[i] = catalog.GetParameter(7);
			sumResults[i] = catalog.SumOf(catalog.GetParameter(1), catalog.GetParameter(2));
		}), TimeSpan.FromSeconds(10));

		paramResults.Should().OnlyContain(p => ReferenceEquals(p, paramResults[0]));
		sumResults.Should().OnlyContain(s => ReferenceEquals(s, sumResults[0]));
	}

	[TestMethod]
	public void ConcurrentGetOrAdd_SameContextSingleSharedNewKey_FactoryInvokedExactlyOnce()
	{
		// Many threads racing to be the FIRST to populate ONE brand-new key on a fresh Context.
		// Verified safe (100+ iterations, no hang) - only one thread ever performs the Dictionary
		// insert here; everyone else blocks on the (correctly) recursion-safe Lazy<T>.
		using var catalog = new EvaluationCatalog<double>();
		var c0 = catalog.GetConstant(5d);
		using var context = new Context();

		var factoryCalls = 0;

		RunBounded(() => Parallel.For(0, 64, _ =>
		{
			context.GetOrAdd<double>(c0, () =>
			{
				Interlocked.Increment(ref factoryCalls);
				return EvaluationResult.Create(5d);
			});
		}), TimeSpan.FromSeconds(10));

		factoryCalls.Should().Be(1, "GetOrAdd must memoize per key even under concurrent first-access races");
	}

	[TestMethod]
	public void ConcurrentEvaluation_PreWarmedContext_ManyWorkersManyRoots_ConsistentResults()
	{
		// A context that has ALREADY been fully populated (every node evaluated once,
		// single-threaded) before the parallel section starts. Every GetOrAdd call in the parallel
		// section below therefore only ever hits the "already present" fast path - no concurrent
		// Dictionary INSERTS race a read. See the QUESTION FOR AUTHOR test below for what happens
		// when the population race is allowed to happen for real (spoiler: it hangs).
		using var catalog = new EvaluationCatalog<double>();
		const int paramCount = 8;
		var parameters = Enumerable.Range(0, paramCount)
			.Select(i => (IEvaluate<double>)catalog.GetParameter(i))
			.ToArray();
		var shared = catalog.SumOf(parameters);

		const int rootCount = 25;
		var roots = Enumerable.Range(0, rootCount)
			.Select(i => (IEvaluate<double>)catalog.ProductOf(shared, catalog.GetConstant((double)(i + 1))))
			.ToArray();

		double[] values = [.. Enumerable.Range(0, paramCount).Select(i => (double)(i + 1))];

		using var context = new Context();
		context.Init(catalog, (ReadOnlySpan<double>)values);

		// Pre-warm: single-threaded evaluation of every root populates the whole memo table.
		var expected = new double[rootCount];
		for (var i = 0; i < rootCount; i++)
			expected[i] = roots[i].Evaluate(context).Result;

		RunBounded(() => Parallel.For(0, 16, workerIndex =>
		{
			for (var i = 0; i < rootCount; i++)
			{
				var idx = (i + workerIndex) % rootCount;
				var result = roots[idx].Evaluate(context).Result;
				result.Should().Be(expected[idx],
					$"root {idx} read concurrently from an already-populated context must match its pre-warmed result");
			}
		}), TimeSpan.FromSeconds(10));

		context.TryGetResult<double>(shared, out var sharedResult).Should().BeTrue();
		sharedResult.Result.Should().Be(values.Sum());
	}

	[TestMethod]
	public void ConcurrentEvaluation_ColdContext_ManyWorkersManyDistinctNewNodes_CompletesWithConsistentResults()
	{
		// Formerly the #14 repro (issue: Context.GetOrAdd's unlocked Dictionary fast-path read raced
		// locked inserts and hung reliably under this exact pattern). Fixed by switching the memo
		// table to ConcurrentDictionary<IEvaluate, Lazy<IEvaluationResult>> - see Context.cs.
		using var catalog = new EvaluationCatalog<double>();
		const int paramCount = 8;
		var parameters = Enumerable.Range(0, paramCount)
			.Select(i => (IEvaluate<double>)catalog.GetParameter(i))
			.ToArray();
		var shared = catalog.SumOf(parameters);

		const int rootCount = 25;
		var roots = Enumerable.Range(0, rootCount)
			.Select(i => (IEvaluate<double>)catalog.ProductOf(shared, catalog.GetConstant((double)(i + 1))))
			.ToArray();

		double[] values = [.. Enumerable.Range(0, paramCount).Select(i => (double)(i + 1))];
		var expectedShared = values.Sum();
		var expected = Enumerable.Range(0, rootCount)
			.Select(i => expectedShared * (i + 1))
			.ToArray();

		using var context = new Context();
		context.Init(catalog, (ReadOnlySpan<double>)values); // Only the 8 PARAMETER leaves are pre-populated.

		// Deliberately NOT pre-warmed: every root's Sum/Product node is being computed for the very
		// first time, concurrently, by whichever of the 16 workers reaches it first.
		RunBounded(() => Parallel.For(0, 16, workerIndex =>
		{
			for (var i = 0; i < rootCount; i++)
			{
				var idx = (i + workerIndex) % rootCount;
				var result = roots[idx].Evaluate(context).Result;
				result.Should().Be(expected[idx],
					$"root {idx}, computed for the first time under concurrent population evaluation, must still be correct");
			}
		}), TimeSpan.FromSeconds(5));

		context.TryGetResult<double>(shared, out var sharedResult).Should().BeTrue();
		sharedResult.Result.Should().Be(expectedShared);
	}
}
