using Open.Disposable;
using Open.Hierarchy;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Open.Evaluation.Core
{
	public class Catalog<T> : DisposableBase, ICatalog<T>
		where T : class, IEvaluate
	{
		public Catalog() { }

		protected override void OnDispose(bool calledExplicitly)
		{
			if (calledExplicitly)
			{
				Registry.Clear();
				//Reductions.Clear();
			}
		}

		readonly ConcurrentDictionary<string, T> Registry = new ConcurrentDictionary<string, T>();

		public void Register<TItem>(ref TItem item)
			where TItem : T
		{
			item = (TItem)Registry.GetOrAdd(item.ToStringRepresentation(), item);
		}

		public TItem Register<TItem>(TItem item)
			where TItem : T
			=> (TItem)Registry.GetOrAdd(item.ToStringRepresentation(), item);

		public TItem Register<TItem>(string id, Func<string, TItem> factory)
			where TItem : T
		{
			return (TItem)Registry.GetOrAdd(id, k =>
			{
				var e = factory(k);
				if (e.ToStringRepresentation() != k)
					throw new Exception("Provided ID does not match instance.ToStringRepresentation().");
				return e;
			});
		}

		public bool TryGetItem<TItem>(string id, out TItem item)
			where TItem : T
		{
			var result = Registry.TryGetValue(id, out T e);
			item = (TItem)e;
			return result;
		}

		public readonly Node<T>.Factory Factory = new Node<T>.Factory();

		readonly ConditionalWeakTable<IReducibleEvaluation<T>, T> Reductions = new ConditionalWeakTable<IReducibleEvaluation<T>, T>();

		public T GetReduced(in T source)
		{
			var src = Register(source);
			return src is IReducibleEvaluation<T> s
				? Reductions.GetValue(s, k => s.TryGetReduced(this, out T r) ? Register(r) : src)
				: src;
		}

		public bool TryGetReduced(in T source, out T reduction)
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

		public abstract class SubmoduleBase<TCatalog>
			where TCatalog : Catalog<T>
		{
			internal readonly TCatalog Source;
			internal readonly Node<T>.Factory Factory;

			protected SubmoduleBase(in TCatalog source)
			{
				Source = source;
				Factory = source.Factory;
			}
		}
	}
}
