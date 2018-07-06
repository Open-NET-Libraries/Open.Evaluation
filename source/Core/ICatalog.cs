using Open.Hierarchy;
using System;
using System.Collections.Generic;

namespace Open.Evaluation.Core
{
	public interface ICatalog<T> : IDisposable
		where T : IEvaluate
	{
		TItem Register<TItem>(TItem item)
			where TItem : T;

		void Register<TItem>(ref TItem item)
			where TItem : T;

		TItem Register<TItem>(string id, Func<string, TItem> factory)
			where TItem : T;

		bool TryGetItem<TItem>(string id, out TItem item)
			where TItem : T;

		T GetReduced(in T source);

		bool TryGetReduced(in T source, out T reduction);

		IEnumerable<T> Flatten<TFlat>(IEnumerable<T> source)
			where TFlat : IParent<T>;
	}
}
