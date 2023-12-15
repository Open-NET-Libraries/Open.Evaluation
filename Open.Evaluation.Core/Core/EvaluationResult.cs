using System.Diagnostics.CodeAnalysis;

namespace Open.Evaluation.Core;

public interface IEvaluationResult : IDescribe
{
	[NotNull]
	object Result { get; }
}

public readonly record struct EvaluationResult<T> : IEvaluationResult
{
	public EvaluationResult(
		[DisallowNull, NotNull] T result,
		[DisallowNull, NotNull] Lazy<string> description)
	{
		Result = result ?? throw new ArgumentNullException(nameof(result));
		Description = description ?? throw new ArgumentNullException(nameof(description));
	}

	public EvaluationResult(
		[DisallowNull, NotNull] T result,
		[DisallowNull, NotNull] Func<T, string> descriptionFactory)
		: this(result, Lazy.New(() => descriptionFactory(result)))
	{ }

	public EvaluationResult(
		[DisallowNull, NotNull] T result,
		[DisallowNull, NotNull] string description)
		: this(result, Lazy.New(description ?? throw new ArgumentNullException(nameof(description))))
	{ }

	public EvaluationResult(
		[DisallowNull, NotNull] T result)
		: this(result, Lazy.New(() => result.ToString() ?? throw new Exception("result.ToString() returned null")))
	{ }

	[NotNull]
	public T Result { get; }

	[NotNull]
	public Lazy<string> Description { get; }

	object IEvaluationResult.Result => Result;

	public static implicit operator T(EvaluationResult<T> result) => result.Result;

	public static implicit operator EvaluationResult<object>(EvaluationResult<T> result)
		=> new(result.Result, result.Description);

	public static explicit operator EvaluationResult<T>(EvaluationResult<object> result)
	{
		var r = result.Result;
		return r is T v
			? new(v, result.Description)
			: throw new InvalidCastException($"Cannot coerce from {r.GetType()} to {typeof(T)}.");
	}
}
