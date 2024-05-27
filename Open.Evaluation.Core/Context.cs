using Open.Collections;
using Open.Evaluation.Core;
using System.Diagnostics.CodeAnalysis;

namespace Open.Evaluation;

public class Context : DisposableBase
{
	private readonly Dictionary<IEvaluate, Lazy<IEvaluationResult>> _registry = [];

	public EvaluationResult<T> GetOrAdd<T>(IEvaluate key, Func<IEvaluate, EvaluationResult<T>> factory)
	{
		AssertIsAlive();

		IEvaluationResult result;
		if (_registry.TryGetValue(key, out var lazy))
		{
			result = lazy.Value;
			goto resultAcquired;
		}

		AssertIsAlive();
		Lazy<EvaluationResult<T>> tLazy;
		lock (_registry)
		{
			if (_registry.TryGetValue(key, out lazy))
			{
				result = lazy.Value;
				goto resultAcquired;
			}

			tLazy = Lazy.New(() => factory(key));
			_registry[key] = new Lazy<IEvaluationResult>(() => tLazy.Value);
		}

		return tLazy.Value;

	resultAcquired:
		return result is EvaluationResult<T> r ? r
			: throw new InvalidCastException($"Cannot coerce from {result.GetType()} to {typeof(T)}.");
	}

	public EvaluationResult<T> GetOrAdd<T>(IEvaluate key, Func<EvaluationResult<T>> factory)
	{
		AssertIsAlive();

		IEvaluationResult result;
		if (_registry.TryGetValue(key, out var lazy))
		{
			result = lazy.Value;
			goto resultAcquired;
		}

		AssertIsAlive();
		Lazy<EvaluationResult<T>> tLazy;
		lock (_registry)
		{
			if (_registry.TryGetValue(key, out lazy))
			{
				result = lazy.Value;
				goto resultAcquired;
			}

			tLazy = Lazy.New(factory);
			_registry[key] = new Lazy<IEvaluationResult>(() => tLazy.Value);
		}

		return tLazy.Value;

	resultAcquired:
		return EvaluationResult<T>.Coerce(result);
	}

	public EvaluationResult<T> GetOrAdd<T>(IEvaluate key, [DisallowNull] T value)
	{
		AssertIsAlive();

		return GetOrAdd(key, () => EvaluationResult.Create(value));
	}

	public bool TryGetResult<T>(IEvaluate key, out EvaluationResult<T> result)
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

	public Context Add(IEvaluate key, Lazy<IEvaluationResult> value)
	{
		AssertIsAlive();

		lock (_registry)
			_registry.Add(key, value);

		return this;
	}

	public Context AddRange(IEnumerable<KeyValuePair<IEvaluate, Lazy<IEvaluationResult>>> values)
	{
		AssertIsAlive();

		lock (_registry)
			_registry.AddRange(values);

		return this;
	}

	public Context AddParam<T>(ICatalog<IEvaluate<T>> catalog, ushort id, T value)
		where T : notnull, IEquatable<T>, IComparable<T>
		=> Add(
			Parameter<T>.Create(catalog, id),
			new Lazy<IEvaluationResult>(EvaluationResult.Create(value)));

	public Context Init<T>(ICatalog<IEvaluate<T>> catalog, IEnumerable<T> value)
		where T : notnull, IEquatable<T>, IComparable<T>
		=> AddRange(
			value.Select((v, i) => Collections.KeyValuePair.Create(
				(IEvaluate)Parameter<T>.Create(catalog, i),
				new Lazy<IEvaluationResult>(() => EvaluationResult.Create(v)))));

	// Allows for re-use.
	public void Clear()
	{
		AssertIsAlive();

		lock (_registry)
			_registry.Clear();
	}

	protected override void OnDispose()
	{
		lock (_registry)
			_registry.Clear();
	}

	public static SharedPool<Context> Shared { get; }
		= new(() => new(), c => c.Clear(), c => c.Dispose(), 100);
}
