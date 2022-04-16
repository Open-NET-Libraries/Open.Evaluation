using Open.Disposable;
using Open.Hierarchy;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace Open.Evaluation.Core;

public class Catalog<T> : DisposableBase, ICatalog<T>
	where T : class, IEvaluate
{
	protected override void OnDispose() => Registry.Clear();//Reductions.Clear();

	readonly ConcurrentDictionary<string, T> Registry = new();

	public void Register<TItem>(
#if NETSTANDARD2_1_OR_GREATER
		[NotNull]
#endif
		ref TItem item)
		where TItem : T
	{
		if (item is null) throw new ArgumentNullException(nameof(item));
		Contract.EndContractBlock();

		item = Register(item);
	}

	protected virtual TItem OnBeforeRegistration<TItem>(TItem item)
		=> item;

	public TItem Register<TItem>(TItem item)
		where TItem : T
	{
		if (item is null) throw new ArgumentNullException(nameof(item));
		Contract.Ensures(Contract.Result<TItem>() is not null);
		Contract.EndContractBlock();
		var result = Registry.GetOrAdd(item.ToStringRepresentation(), OnBeforeRegistration(item));
		Debug.Assert(result is not null);
		Debug.Assert(result is TItem);
		return (TItem)result;
	}

	public TItem Register<TItem>(string id, Func<string, TItem> factory)
		where TItem : T
	{
		if (id is null) throw new ArgumentNullException(nameof(id));
		if (factory is null) throw new ArgumentNullException(nameof(factory));
		Contract.Ensures(Contract.Result<TItem>() is not null);
		Contract.EndContractBlock();

		return (TItem)Registry.GetOrAdd(id, k =>
		{
			var e = factory(k);
			Debug.Assert(e is not null);
			var hash = e.ToStringRepresentation();
			Debug.Assert(hash == k);
			if (hash != k)
				throw new Exception($"Provided ID does not match instance.ToStringRepresentation().\nkey: {k}\nhash: {hash}");
			return OnBeforeRegistration(e);
		});
	}

	public TItem Register<TItem, TParam>(string id, TParam param, Func<string, TParam, TItem> factory)
		where TItem : T
	{
		if (id is null) throw new ArgumentNullException(nameof(id));
		if (factory is null) throw new ArgumentNullException(nameof(factory));
		Contract.Ensures(Contract.Result<TItem>() is not null);
		Contract.EndContractBlock();

		return (TItem)Registry.GetOrAdd(id, k =>
		{
			var e = factory(k, param);
			Debug.Assert(e is not null);
			var hash = e.ToStringRepresentation();
			Debug.Assert(hash == k);
			if (hash != k)
				throw new Exception($"Provided ID does not match instance.ToStringRepresentation().\nkey: {k}\nhash: {hash}");
			return OnBeforeRegistration(e);
		});
	}

	public bool TryGetItem<TItem>(string id,
#if NETSTANDARD2_1_OR_GREATER
		[NotNullWhen(true)]
#endif
		out TItem item)
		where TItem : T
	{
		if (id is null) throw new ArgumentNullException(nameof(id));
		Contract.EndContractBlock();

		var result = Registry.TryGetValue(id, out var e);
		item = (TItem)e;
		return result;
	}

	public Node<T>.Factory Factory { get; } = new Node<T>.Factory();

	readonly ConditionalWeakTable<IReducibleEvaluation<T>, T> Reductions = new();

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
		T source,
#if NETSTANDARD2_1_OR_GREATER
		[NotNullWhen(true)]
#endif
		out T reduction)
	{
		reduction = GetReduced(source);
		return !reduction.Equals(source);
	}

	public IEnumerable<T> Flatten<TFlat>(IEnumerable<T> source)
		where TFlat : IParent<T>
	{
		return source is null
			? throw new ArgumentNullException(nameof(source))
			: FlattenCore(this, source);

		static IEnumerable<T> FlattenCore(Catalog<T> catalog, IEnumerable<T> source)
		{
			foreach (var child in source)
			{
				var c = catalog.GetReduced(child);
				if (c is TFlat flat)
				{
					foreach (var sc in flat.Children)
						yield return sc;
				}
				else
				{
					yield return c;
				}
			}
		}
	}

	public abstract class SubmoduleBase
	{
		// ReSharper disable once UnusedAutoPropertyAccessor.Global
		internal ICatalog<T> Catalog { get; }
		internal readonly Node<T>.Factory Factory;

		protected SubmoduleBase(ICatalog<T> catalog, Node<T>.Factory factory)
		{
			Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
			Factory = factory ?? throw new ArgumentNullException(nameof(factory));
		}
	}

	public abstract class SubmoduleBase<TCatalog> : SubmoduleBase
		where TCatalog : Catalog<T>
	{
		internal new TCatalog Catalog { get; }

		protected SubmoduleBase(TCatalog catalog) : base(catalog ?? throw new ArgumentNullException(nameof(catalog)), catalog.Factory) => Catalog = catalog;
	}
}
