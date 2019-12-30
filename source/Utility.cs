using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace Open.Evaluation
{
	internal static class Utility
	{
		public static IEnumerable<T> Concat<T>(this IEnumerable<T> source, in T next)
		{
			if (source is null) throw new ArgumentNullException(nameof(source));

			return source
				.Concat(Enumerable.Repeat(next, 1));
		}

		public static IEnumerable<T> SkipAt<T>(this IEnumerable<T> source, int index)
		{
			if (source is null) throw new ArgumentNullException(nameof(source));
			if (index < 0) throw new ArgumentOutOfRangeException(nameof(index), index, "Must be at least zero.");
			Contract.EndContractBlock();

			using var e = source.GetEnumerator();
			var count = 0;
			while (e.MoveNext())
			{
				if (count != index)
					yield return e.Current;

				count++;
			}
		}

		public static IEnumerable<T> ReplaceAt<T>(this IEnumerable<T> source, int index, T replacement)
		{
			if (source is null) throw new ArgumentNullException(nameof(source));
			if (index < 0) throw new ArgumentOutOfRangeException(nameof(index), index, "Must be at least zero.");
			Contract.EndContractBlock();

			using var e = source.GetEnumerator();
			var count = 0;
			while (e.MoveNext())
			{
				if (count == index)
					yield return replacement;
				else
					yield return e.Current;

				count++;
			}
		}

		public static IEnumerable<T> InsertAt<T>(this IEnumerable<T> source, int index, IEnumerable<T> injection)
		{
			if (source is null) throw new ArgumentNullException(nameof(source));
			if (index < 0) throw new ArgumentOutOfRangeException(nameof(index), index, "Must be at least zero.");
			Contract.EndContractBlock();

			using var e = source.GetEnumerator();
			var count = 0;
			while (e.MoveNext())
			{
				if (count == index)
				{
					// ReSharper disable once PossibleMultipleEnumeration
					foreach (var i in injection)
						yield return i;
				}

				yield return e.Current;

				count++;
			}
		}

		public static IEnumerable<T> InsertAt<T>(this IEnumerable<T> source, int index, T injection)
		{
			if (index < 0) throw new ArgumentOutOfRangeException(nameof(index), index, "Must be at least zero.");

			using var e = source.GetEnumerator();
			var count = 0;
			while (e.MoveNext())
			{
				if (count == index)
					yield return injection;

				yield return e.Current;

				count++;
			}
		}

		public static List<T> Extract<T>(this IList<T> target, in Func<T, bool> predicate)
		{
			var extracted = new List<T>();
			var len = target.Count;
			var removed = ArrayPool<int>.Shared.Rent(len);
			var removedCount = 0;

			for (var i = 0; i < len; i++)
			{
				var c = target[i];
				if (!predicate(c)) continue;
				extracted.Add(c);
				removed[removedCount++] = i;
			}

			// Doing this in reverse lessens the load on removing from the list.
			while (0 != removedCount--)
				target.RemoveAt(removed[removedCount]);

			ArrayPool<int>.Shared.Return(removed);
			return extracted;
		}

		public static List<TExtract> ExtractType<TExtract>(this IList target)
		{
			var extracted = new List<TExtract>();
			var len = target.Count;
			var removed = ArrayPool<int>.Shared.Rent(len);
			var removedCount = 0;

			for (var i = 0; i < len; i++)
			{
				if (!(target[i] is TExtract e)) continue;
				extracted.Add(e);
				removed[removedCount++] = i;
			}

			// Doing this in reverse lessens the load on removing from the list.
			while (0 != removedCount--)
				target.RemoveAt(removed[removedCount]);

			ArrayPool<int>.Shared.Return(removed);
			return extracted;
		}
	}
}
