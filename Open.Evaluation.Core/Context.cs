using Open.Evaluation.Core;
using System.Diagnostics.CodeAnalysis;

namespace Open.Evaluation;

public class Context : DisposableBase
{
	private readonly Dictionary<IEvaluate, Lazy<IEvaluationResult>> _registry = [];

	public EvaluationResult<TResult> GetOrAdd<TResult>(IEvaluate key, Func<IEvaluate, EvaluationResult<TResult>> factory)
	{
		IEvaluationResult result;
		if (_registry.TryGetValue(key, out var lazy))
		{
			result = lazy.Value;
			goto resultAcquired;
		}

		AssertIsAlive();
		Lazy<EvaluationResult<TResult>> tLazy;
		lock (_registry)
		{
			if (_registry.TryGetValue(key, out lazy))
			{
				result = lazy.Value;
				goto resultAcquired;
			}

			tLazy = new Lazy<EvaluationResult<TResult>>(() => factory(key));
			_registry[key] = new Lazy<IEvaluationResult>(() => tLazy.Value);
		}

		return tLazy.Value;

	resultAcquired:
		return result is EvaluationResult<TResult> r ? r
			: throw new InvalidCastException($"Cannot coerce from {result.GetType()} to {typeof(TResult)}.");
	}

	public EvaluationResult<TResult> GetOrAdd<TResult>(IEvaluate key, Func<EvaluationResult<TResult>> factory)
	{
		IEvaluationResult result;
		if (_registry.TryGetValue(key, out var lazy))
		{
			result = lazy.Value;
			goto resultAcquired;
		}

		AssertIsAlive();
		Lazy<EvaluationResult<TResult>> tLazy;
		lock (_registry)
		{
			if (_registry.TryGetValue(key, out lazy))
			{
				result = lazy.Value;
				goto resultAcquired;
			}

			tLazy = new Lazy<EvaluationResult<TResult>>(factory);
			_registry[key] = new Lazy<IEvaluationResult>(() => tLazy.Value);
		}

		return tLazy.Value;

	resultAcquired:
		return result is EvaluationResult<TResult> r ? r
			: throw new InvalidCastException($"Cannot coerce from {result.GetType()} to {typeof(TResult)}.");
	}

	public EvaluationResult<TResult> GetOrAdd<TResult>(IEvaluate key, [DisallowNull] TResult value)
		=> GetOrAdd(key, () => new EvaluationResult<TResult>(value));

	// Allows for re-use.
	public void Clear()
	{
		lock (_registry) _registry.Clear();
	}

	protected override void OnDispose() => Clear();
}
