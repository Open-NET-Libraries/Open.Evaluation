using Open.Evaluation.Hierarchy;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace Open.Evaluation
{
	public class Catalog<T>
		where T : IEvaluate
	{
		readonly ConcurrentDictionary<string, T> Registry = new ConcurrentDictionary<string, T>();

		public TItem Register<TItem>(TItem item)
			where TItem : T
		{
			return (TItem)Registry.GetOrAdd(item.ToStringRepresentation(), item);
		}

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
	}

	public class Catalog : Catalog<IEvaluate>
	{

	}
}
