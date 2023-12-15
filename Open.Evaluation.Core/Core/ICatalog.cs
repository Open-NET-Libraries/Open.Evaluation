﻿using Open.Hierarchy;
using System.Diagnostics.CodeAnalysis;

namespace Open.Evaluation.Core;

/// <summary>
/// A catalog of evaluations.
/// </summary>
public interface ICatalog<T> : IDisposable
	where T : IEvaluate
{
	[return: NotNull]
	TItem Register<TItem>(TItem item)
		where TItem : T;

	void Register<TItem>([DisallowNull, NotNull] ref TItem item)
		where TItem : T;

	[return: NotNull]
	TItem Register<TItem>(string id, Func<string, TItem> factory)
		where TItem : T;

	[return: NotNull]
	TItem Register<TItem, TParam>(string id, TParam param, Func<string, TParam, TItem> factory)
		where TItem : T;

	bool TryGetItem<TItem>(string id, [MaybeNullWhen(false)] out TItem item)
		where TItem : T;

	[return: NotNull]
	T GetReduced(T source);

	// ReSharper disable once UnusedMemberInSuper.Global
	bool TryGetReduced(T source, [NotNull] out T reduction);

	IEnumerable<T> Flatten<TFlat>(IEnumerable<T> source)
		where TFlat : IParent<T>;
}
