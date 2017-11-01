using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Open.Evaluation
{
	internal static class Utility
	{
		public static IEnumerable<T> Concat<T>(this IEnumerable<T> source, T next)
		{
			return source
				.Concat(Enumerable.Repeat(next, 1));
		}

		public static IEnumerable<T> SkipAt<T>(this IEnumerable<T> source, int index)
		{
			if (index < 0) throw new ArgumentOutOfRangeException("index", index, "Must be at least zero.");

			var count = 0;
			var e = source.GetEnumerator();
			while (e.MoveNext())
			{
				if (count != index)
					yield return e.Current;

				count++;
			}
		}

		public static IEnumerable<T> ReplaceAt<T>(this IEnumerable<T> source, int index, T replacement)
		{
			if (index < 0) throw new ArgumentOutOfRangeException("index", index, "Must be at least zero.");

			var count = 0;
			var e = source.GetEnumerator();
			while(e.MoveNext())
			{
				if (count == index)
					yield return replacement;
				else
					yield return e.Current;

				count++;
			}
		}

		public static IEnumerable<T> InsertAt<T>(this IEnumerable<T> source, int at, IEnumerable<T> injection)
		{
			if (at < 0) throw new ArgumentOutOfRangeException("index", at, "Must be at least zero.");

			var count = 0;
			var e = source.GetEnumerator();
			while (e.MoveNext())
			{
				if (count == at)
				{
					foreach(var i in injection)
						yield return i;
				}

				yield return e.Current;

				count++;
			}
		}

		public static IEnumerable<T> InsertAt<T>(this IEnumerable<T> source, int at, T injection)
		{
			if (at < 0) throw new ArgumentOutOfRangeException("index", at, "Must be at least zero.");

			var count = 0;
			var e = source.GetEnumerator();
			while (e.MoveNext())
			{
				if (count == at)
				{
					yield return injection;
				}

				yield return e.Current;

				count++;
			}
		}

		public static List<T> Extract<T>(this IList<T> target, Func<T, bool> predicate)
		{
			var extracted = new List<T>();
			var contents = target.ToArray();
			foreach (var c in contents)
			{
				if (predicate(c)) target.Remove(c);
				extracted.Add(c);
			}
			return extracted;
		}

		public static List<TExtract> ExtractType<TExtract>(this IList target)
		{
			var extracted = new List<TExtract>();
			var removed = new List<int>();
			var len = target.Count;
			for (var i = 0; i < len; i++)
			{
				if (target[i] is TExtract e)
				{
					extracted.Add(e);
					removed.Add(i);
				}
			}
			foreach (var i in removed)
				target.RemoveAt(i);

			return extracted;
		}
	}
}
