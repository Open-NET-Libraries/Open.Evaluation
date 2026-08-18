using Open.Collections;
using Open.Evaluation.Core;
using System.Buffers;
using System.Collections.Concurrent;

namespace Open.Evaluation;

public class Context : DisposableBase
{
	// A ConcurrentDictionary keyed to a SINGLE Lazy<IEvaluationResult> per entry preserves
	// exactly-once evaluation under contention: concurrent GetOrAdd calls racing to populate the
	// SAME new key may each construct their own throwaway Lazy wrapper (cheap - the wrapper's
	// constructor does no evaluation work), but ConcurrentDictionary.GetOrAdd guarantees only ONE
	// of those wrapper instances is ever stored; every caller - the winner and every loser alike -
	// then reads THAT instance's .Value, and Lazy<T>'s default LazyThreadSafetyMode
	// (ExecutionAndPublication) guarantees the wrapped factory itself runs exactly once while the
	// other threads block and observe the winner's result. An "optimistic" GetOrAdd (storing the
	// evaluation result directly, with no Lazy) would let every racing thread actually RUN the
	// (potentially expensive, shared-branch) factory concurrently before discarding all but one
	// result - see issue #14.
	private readonly ConcurrentDictionary<IEvaluate, Lazy<IEvaluationResult>> _registry = new();

	public EvaluationResult<T> GetOrAdd<T>(IEvaluate key, Func<IEvaluate, EvaluationResult<T>> factory)
		where T : notnull
	{
		// Preserve the original two-gate shape: an initial liveness check before the cheap fast-path
		// read, and a second one immediately before the (now lock-free, but still not re-checked
		// again past this point) slow-path population - see "Disposal ordering" in the PR notes.
		AssertIsAlive();

		if (!_registry.TryGetValue(key, out var lazy))
		{
			AssertIsAlive();

			lazy = _registry.GetOrAdd(key,
				static (k, f) => new Lazy<IEvaluationResult>(() => f(k)),
				factory);
		}

		var result = lazy.Value;
		return result is EvaluationResult<T> r ? r
			: throw new InvalidCastException($"Cannot coerce from {result.GetType()} to {typeof(T)}.");
	}

	public EvaluationResult<T> GetOrAdd<T>(IEvaluate key, Func<EvaluationResult<T>> factory)
		where T : notnull
	{
		AssertIsAlive();

		if (!_registry.TryGetValue(key, out var lazy))
		{
			AssertIsAlive();

			lazy = _registry.GetOrAdd(key,
				static (_, f) => new Lazy<IEvaluationResult>(() => f()),
				factory);
		}

		return EvaluationResult<T>.Coerce(lazy.Value);
	}

	public EvaluationResult<T> GetOrAdd<T>(IEvaluate key, T value)
		where T : notnull
	{
		AssertIsAlive();

		return GetOrAdd(key, () => EvaluationResult.Create(value));
	}

	public bool TryGetResult<T>(IEvaluate key, out EvaluationResult<T> result)
		where T : notnull
	{
		AssertIsAlive();

		if (_registry.TryGetValue(key, out var lazy))
		{
			result = EvaluationResult<T>.Coerce(lazy.Value);
			return true;
		}

		result = default;
		return false;
	}

	// Mirrors Dictionary<TKey,TValue>.Add's contract (throws on a duplicate key) even though the
	// backing store is now a ConcurrentDictionary, whose own IDictionary<TKey,TValue>.Add is only
	// reachable via an explicit interface cast.
	private void AddCore(IEvaluate key, Lazy<IEvaluationResult> value)
	{
		if (!_registry.TryAdd(key, value))
			throw new ArgumentException($"An entry with the same key has already been added. Key: {key}", nameof(key));
	}

	public Context Add(IEvaluate key, Lazy<IEvaluationResult> value)
	{
		AssertIsAlive();

		AddCore(key, value);

		return this;
	}

	public Context AddRange(IEnumerable<KeyValuePair<IEvaluate, Lazy<IEvaluationResult>>> values)
	{
		AssertIsAlive();

		foreach (var (key, value) in values)
			AddCore(key, value);

		return this;
	}

	public Context AddRange(ReadOnlySpan<KeyValuePair<IEvaluate, Lazy<IEvaluationResult>>> values)
	{
		AssertIsAlive();

		foreach (var (key, value) in values)
			AddCore(key, value);

		return this;
	}

	public Context AddParam<T>(ICatalog<IEvaluate<T>> catalog, ushort id, T value)
		where T : notnull, IEquatable<T>, IComparable<T>
		=> Add(
			Parameter<T>.Create(catalog, id),
			EvaluationResult.Create(value));

	public Context InitRange<T>(ICatalog<IEvaluate<T>> catalog, IEnumerable<T> value)
		where T : notnull, IEquatable<T>, IComparable<T>
		=> AddRange(
			value.Select((v, i) => Collections.KeyValuePair.Create(
				(IEvaluate)Parameter<T>.Create(catalog, i),
				new Lazy<IEvaluationResult>(EvaluationResult.Create(v)))));

	public Context Init<T>(ICatalog<IEvaluate<T>> catalog, ReadOnlySpan<T> value)
	where T : notnull, IEquatable<T>, IComparable<T>
	{
		using var lease = MemoryPool<KeyValuePair<IEvaluate, Lazy<IEvaluationResult>>>.Shared.Rent(value.Length);
		var pairs = lease.Memory.Span.Slice(0, value.Length);

		for (var i = 0; i < value.Length; i++)
		{
			ref readonly T v = ref value[i];
			pairs[i] = Collections.KeyValuePair.Create(
				(IEvaluate)Parameter<T>.Create(catalog, i),
				new Lazy<IEvaluationResult>(EvaluationResult.Create(v)));
		}

		return AddRange(pairs);
	}

	// Allows for re-use.
	public void Clear()
	{
		AssertIsAlive();

		_registry.Clear();
	}

	protected override void OnDispose()
		=> _registry.Clear();

	public class ContextPool : InterlockedArrayObjectPool<Context>
	{
		public ContextPool()
			: base(() => new Context(), c => c.Clear(), c => c.Dispose(), 100) { }

		[Obsolete("Shared pools do not support disposal.")]
		public new void Dispose()
				=> throw new NotSupportedException("Shared pools cannot be disposed.");
	}

	public static ContextPool Shared { get; } = new();

	public static Context Get()
		=> Shared.Take();

	public static RecycleHelper<Context> Rent()
		=> Shared.Rent();

	public static void Use(Action<Context> handler)
	=> Shared.Rent(handler);

	public static EvaluationResult<T> Evaluate<T>(IEvaluate<T> e, ReadOnlySpan<T> values)
		where T : notnull, IEquatable<T>, IComparable<T>
	{
		var context = Get();
		context.Init(e.Catalog, values);
		var result = e.Evaluate(context);
		Shared.Give(context);
		return result;
	}
}
