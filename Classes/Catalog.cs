using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Open.Evaluation
{
	public class Catalog<T>
		where T : IEvaluate
	{
		ConcurrentDictionary<string, T> Registry = new ConcurrentDictionary<string, T>();

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



		/// <summary>
		/// For any evaluation node, correct the hierarchy to match.
		/// </summary>
		/// <param name="target">The node tree to correct.</param>
		/// <returns>The updated tree.</returns>
		public Hierarchy.Node FixHierarchy(Hierarchy.Node target)
		{
			// Does this node's value contain children?
			if (target.Value is IReproducable r)
			{
				// This recursion technique will operate on the leaves of the tree first.
				var node = Hierarchy.Get(
					(T)r.CreateNewFrom(
						r.ReproductionParam,
						// Using new children... Rebuild using new structure and check for registration.
						target.Select(n => (IEvaluate)Register(FixHierarchy(n).Value))
					)
				);
				node.Parent = target.Parent;
				return node;
			}
			// No children? Then clear any child notes.
			else
			{
				if (target.Count != 0)
					target.Clear();

				var old = target.Value;
				var registered = Register(target.Value);
				if (!old.Equals(registered))
					target.Value = registered;

				return target;
			}
		}
	}

	public class Catalog : Catalog<IEvaluate>
	{

	}
}
