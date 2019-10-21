using Open.Disposable;
using Open.Hierarchy;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace Open.Evaluation.Core
{
	public class Catalog<T> : DisposableBase, ICatalog<T>
		where T : class, IEvaluate
	{
		protected override void OnDispose()
		{
			Registry.Clear();
			//Reductions.Clear();
		}

		readonly ConcurrentDictionary<string, T> Registry = new ConcurrentDictionary<string, T>();

		public void Register<TItem>(ref TItem item)
			where TItem : T
		{
			if (item == null) throw new ArgumentNullException(nameof(item));
			Contract.Ensures(Contract.Result<TItem>() != null);
			Contract.EndContractBlock();

			item = Register(item);
		}

		protected virtual TItem OnBeforeRegistration<TItem>(TItem item)
			=> item;

		public TItem Register<TItem>(TItem item)
			where TItem : T
		{
			if (item == null) throw new ArgumentNullException(nameof(item));
			Contract.Ensures(Contract.Result<TItem>() != null);
			Contract.EndContractBlock();
			var result = Registry.GetOrAdd(item.ToStringRepresentation(), OnBeforeRegistration(item));
			Debug.Assert(result != null);
			Debug.Assert(result is TItem);
			return (TItem)result;
		}

		public TItem Register<TItem>(string id, Func<string, TItem> factory)
			where TItem : T
		{
			if (id == null) throw new ArgumentNullException(nameof(id));
			if (factory == null) throw new ArgumentNullException(nameof(factory));
			Contract.Ensures(Contract.Result<TItem>() != null);
			Contract.EndContractBlock();

			return (TItem)Registry.GetOrAdd(id, k =>
			{
				var e = factory(k);
				Debug.Assert(e != null);
				var hash = e.ToStringRepresentation();
				Debug.Assert(hash == k);
				if (hash != k)
					throw new Exception($"Provided ID does not match instance.ToStringRepresentation().\nkey: {k}\nhash: {hash}");
				return OnBeforeRegistration(e);
			});
		}

		public bool TryGetItem<TItem>(string id, out TItem item)
			where TItem : T
		{
			if (id == null) throw new ArgumentNullException(nameof(id));
			Contract.Ensures(Contract.Result<TItem>() != null);
			Contract.EndContractBlock();

			var result = Registry.TryGetValue(id, out var e);
			item = (TItem)e;
			return result;
		}

		public readonly Node<T>.Factory Factory = new Node<T>.Factory();

		readonly ConditionalWeakTable<IReducibleEvaluation<T>, T> Reductions
			= new ConditionalWeakTable<IReducibleEvaluation<T>, T>();

		public T GetReduced(T source)
		{
			var src = Register(source);
			return src is IReducibleEvaluation<T> s
				? Reductions.GetValue(s, k =>
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

		public bool TryGetReduced(T source, out T reduction)
		{
			reduction = GetReduced(source);
			return !reduction.Equals(source);
		}

		public IEnumerable<T> Flatten<TFlat>(IEnumerable<T> source)
			where TFlat : IParent<T>
		{
			foreach (var child in source)
			{
				var c = GetReduced(child);
				if (c is TFlat)
				{
					var f = (IParent<T>)c;
					foreach (var sc in f.Children)
						yield return sc;
				}
				else
				{
					yield return c;
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
				Catalog = catalog;
				Factory = factory;
			}
		}

		public abstract class SubmoduleBase<TCatalog> : SubmoduleBase
			where TCatalog : Catalog<T>
		{
			internal new TCatalog Catalog { get; }

			protected SubmoduleBase(TCatalog catalog) : base(catalog, catalog.Factory)
			{
				Catalog = catalog;
			}
		}
	}
}
