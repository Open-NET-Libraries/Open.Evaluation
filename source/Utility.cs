using Open.Disposable;
using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace Open.Evaluation
{
	internal static class Utility
	{
		const int POOL_ARRAY_LEN = 128;
		public static void Rent<T>(this ArrayPool<T> pool, int minLength, Action<T[]> action)
		{
			if (minLength > POOL_ARRAY_LEN)
			{
				var a = pool.Rent(minLength);
				try
				{
					action(a);
				}
				finally
				{
					pool.Return(a);
				}
			}
			else
			{
				action(new T[minLength]);
			}
		}

		public static TResult Rent<T, TResult>(this ArrayPool<T> pool, int minLength, Func<T[], TResult> action)
		{
			if (minLength > POOL_ARRAY_LEN)
			{
				var a = pool.Rent(minLength);
				try
				{
					return action(a);
				}
				finally
				{
					pool.Return(a);
				}
			}
			else
			{
				return action(new T[minLength]);
			}
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

		public static List<T> Extract<T>(this IList<T> target, Func<T, bool> predicate)
		{
			var len = target.Count;
			return ArrayPool<int>.Shared.Rent(len, removed =>
			{
				var extracted = ListPool<T>.Shared.Take();
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

				return extracted;
			});
		}

		public static List<TExtract> ExtractType<TExtract>(this IList target)
		{
			var len = target.Count;
			return ArrayPool<int>.Shared.Rent(len, removed =>
			{
				var extracted = ListPool<TExtract>.Shared.Take();
				var removedCount = 0;

				for (var i = 0; i < len; i++)
				{
					if (target[i] is not TExtract e) continue;
					extracted.Add(e);
					removed[removedCount++] = i;
				}

				// Doing this in reverse lessens the load on removing from the list.
				while (0 != removedCount--)
					target.RemoveAt(removed[removedCount]);

				return extracted;
			});
		}
	}
}
