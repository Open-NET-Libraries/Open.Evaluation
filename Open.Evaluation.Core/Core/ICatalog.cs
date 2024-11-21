using System.Diagnostics.CodeAnalysis;

namespace Open.Evaluation.Core;

/// <summary>
/// A catalog of evaluations.
/// </summary>
public interface ICatalog<T> : IDisposable
	where T : notnull, IEvaluate
{
	string GetPooledId(string id);

	TItem Register<TItem>(TItem item)
		where TItem : notnull, T;

	void Register<TItem>(ref TItem item)
		where TItem : notnull, T;

	TItem Register<TItem>(string id, Func<string, ICatalog<T>, TItem> factory)
		where TItem : notnull, T;

	TItem Register<TItem, TParam>(string id, TParam param, Func<string, ICatalog<T>, TParam, TItem> factory)
		where TItem : notnull, T;

	bool TryGetItem<TItem>(string id, [MaybeNullWhen(false)] out TItem item)
		where TItem : notnull, T;

	T GetReduced(T source);

	// ReSharper disable once UnusedMemberInSuper.Global
	bool TryGetReduced(T source, [NotNull] out T reduction);
}
