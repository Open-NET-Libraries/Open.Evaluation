﻿using System.Diagnostics.CodeAnalysis;

namespace Open.Evaluation.Core;

public interface IEvaluationResult : IDescribe
{
	[NotNull]
	object Result { get; }
}

public readonly record struct EvaluationResult<T> : IEvaluationResult
{
	public EvaluationResult(
		[DisallowNull] in T result,
		Lazy<string> description)
	{
		Result = result ?? throw new ArgumentNullException(nameof(result));
		Description = description ?? throw new ArgumentNullException(nameof(description));
	}

	public EvaluationResult(
		[DisallowNull] T result,
		Func<T, string> descriptionFactory)
		: this(result, Lazy.New(() => descriptionFactory(result))) { }

	public EvaluationResult(
		[DisallowNull] T result,
		string description)
		: this(result, Lazy.New(description ?? throw new ArgumentNullException(nameof(description)))) { }

	public EvaluationResult(
		[DisallowNull] T result)
		: this(result, Lazy.New(() => result.ToString() ?? throw new Exception("result.ToString() returned null"))) { }

	[NotNull]
	public T Result { get; }

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

	public static implicit operator Lazy<EvaluationResult<T>>(EvaluationResult<T> result)
		=> new(result);

	public static implicit operator Lazy<IEvaluationResult>(EvaluationResult<T> result)
		=> Lazy.New<IEvaluationResult>(result);

	public static EvaluationResult<T> Coerce(IEvaluationResult result)
		=> result is EvaluationResult<T> r ? r
			: throw new InvalidCastException($"Cannot coerce from {result.GetType()} to {typeof(T)}.");
}

public static class EvaluationResult
{
	public static EvaluationResult<T> Create<T>(
		[DisallowNull] in T result,
		Lazy<string> description)
		=> new(in result, description);

	public static EvaluationResult<T> Create<T>(
		[DisallowNull] T result,
		Func<T, string> descriptionFactory)
		=> new(result, descriptionFactory);

	public static EvaluationResult<T> Create<T>(
		[DisallowNull] T result,
		string description)
		=> new(result, description);

	public static EvaluationResult<T> Create<T>(
		[DisallowNull] T result)
		=> new(result);
}