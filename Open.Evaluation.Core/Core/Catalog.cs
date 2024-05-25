﻿using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Open.Evaluation.Core;

public class Catalog<T> : DisposableBase, ICatalog<T>
	where T : class, IEvaluate
{
	private static Catalog<T>? _instance;
	public static Catalog<T> Shared
		=> LazyInitializer.EnsureInitialized(ref _instance);

	protected override void OnDispose()
	{
		Registry.Clear();
		Reductions.Clear();
	}

	readonly ConcurrentDictionary<string, T> Registry = new();

	public void Register<TItem>([NotNull] ref TItem item)
		where TItem : T
	{
		item.ThrowIfNull();
		Contract.EndContractBlock();

		item = Register(item);
	}

	protected virtual TItem OnBeforeRegistration<TItem>(TItem item)
		=> item;

	[return: NotNull]
	public TItem Register<TItem>(TItem item)
		where TItem : T
	{
		item.ThrowIfNull();
		Contract.EndContractBlock();

		string key = item.ToString().ThrowIfNull();
		var result = Registry.GetOrAdd(key, OnBeforeRegistration(item));
		Debug.Assert(result is not null);
		Debug.Assert(result is TItem);
		return (TItem)result;
	}

	[return: NotNull]
	public TItem Register<TItem>(string id, Func<string, TItem> factory)
		where TItem : T
	{
		id.ThrowIfNull();
		factory.ThrowIfNull();
		Contract.EndContractBlock();

		return (TItem)Registry.GetOrAdd(id, k =>
		{
			var e = factory(k);
			Debug.Assert(e is not null);
			var hash = e.ToString();
			Debug.Assert(hash == k);
			return hash != k ? throw new ArgumentException($"Does not match instance.ToString().\nkey: {k}\nhash: {hash}", nameof(id))
				: (T)OnBeforeRegistration(e);
		});
	}

	[return: NotNull]
	public TItem Register<TItem, TParam>(string id, TParam param, Func<string, TParam, TItem> factory)
		where TItem : T
	{
		id.ThrowIfNull();
		factory.ThrowIfNull();
		Contract.Ensures(Contract.Result<TItem>() is not null);
		Contract.EndContractBlock();

		return (TItem)Registry.GetOrAdd(id, k =>
		{
			var e = factory(k, param);
			Debug.Assert(e is not null);
			var hash = e.ToString();
			Debug.Assert(hash == k);
			return hash != k ? throw new ArgumentException($"Does not match instance.ToStringRepresentation().\nkey: {k}\nhash: {hash}", nameof(id))
				: (T)OnBeforeRegistration(e);
		});
	}

	public bool TryGetItem<TItem>(string id, [NotNullWhen(true)] out TItem item)
		where TItem : T
	{
		id.ThrowIfNull();
		Contract.EndContractBlock();

		var result = Registry.TryGetValue(id, out var e);
		Debug.Assert(e is not null);
		item = (TItem)e;
		return result;
	}

	public Node<T>.Factory Factory { get; } = new Node<T>.Factory();

	readonly ConditionalWeakTable<IReducibleEvaluation<T>, T> Reductions = [];

	[return: NotNull]
	public T GetReduced(T source)
	{
		var src = Register(source);
		return src is IReducibleEvaluation<T> s
			? Reductions.GetValue(s, _ =>
			{
				var count = 0;
				var result = src;
				while (result is IReducibleEvaluation<T> red
					   && red.TryGetReduced(this, out var r) && r != result)
				{
					result = r;
					count++;
#if DEBUG
					if (count > 3)
						Debugger.Break();
#endif
					if (count > 10)
						break;
				}

				return Register(result);
			})
			: src;
	}

	public bool TryGetReduced(
		T source, [NotNull] out T reduction)
	{
		reduction = GetReduced(source);
		return !reduction.Equals(source);
	}

	public abstract class SubmoduleBase(ICatalog<T> catalog, Node<T>.Factory factory)
	{
		// ReSharper disable once UnusedAutoPropertyAccessor.Global
		public ICatalog<T> Catalog { get; } = catalog ?? throw new ArgumentNullException(nameof(catalog));
		internal readonly Node<T>.Factory Factory = factory ?? throw new ArgumentNullException(nameof(factory));
	}

	public abstract class SubmoduleBase<TCatalog>(TCatalog catalog)
		: SubmoduleBase(catalog ?? throw new ArgumentNullException(nameof(catalog)), catalog.Factory)
		where TCatalog : Catalog<T>
	{
		public new TCatalog Catalog { get; } = catalog;
	}
}