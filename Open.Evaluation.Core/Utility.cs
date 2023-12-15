using Open.Disposable;
using System.Buffers;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using Throw;

namespace Open.Evaluation;

public static class Lazy
{
	public static Lazy<T> New<T>(Func<T> factory) => new(factory);
	public static Lazy<T> New<T>(T value) => new(value);
}

public static class Utility
{
	const int POOL_ARRAY_LEN = 128;
	const int MAX_ARRAY_LEN = int.MaxValue / 2;
	public static void Rent<T, TParam>(this ArrayPool<T> pool, int minLength, TParam param, Action<TParam, T[]> action)
	{
		if (minLength is > POOL_ARRAY_LEN and < MAX_ARRAY_LEN)
		{
			var a = pool.Rent(minLength);
			try
			{
				action(param, a);
			}
			finally
			{
				pool.Return(a);
			}
		}
		else
		{
			action(param, new T[minLength]);
		}
	}

	public static void Rent<T>(this ArrayPool<T> pool, int minLength, Action<T[]> action)
	{
		if (minLength is > POOL_ARRAY_LEN and < MAX_ARRAY_LEN)
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

	public static TResult Rent<T, TParam, TResult>(
		this ArrayPool<T> pool,
		int minLength,
		[DisallowNull] TParam param,
		Func<TParam, T[], TResult> action)
	{
		if (minLength is > POOL_ARRAY_LEN and < MAX_ARRAY_LEN)
		{
			var a = pool.Rent(minLength);
			try
			{
				return action(param, a);
			}
			finally
			{
				pool.Return(a);
			}
		}
		else
		{
			return action(param, new T[minLength]);
		}
	}

	public static TResult Rent<T, TResult>(
		this ArrayPool<T> pool,
		int minLength,
		Func<T[], TResult> action)
	{
		pool.ThrowIfNull().OnlyInDebug();
		action.ThrowIfNull().OnlyInDebug();

		if (minLength is > POOL_ARRAY_LEN and < MAX_ARRAY_LEN)
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

	public static IEnumerable<T> SkipAt<T>(
		this IEnumerable<T> source,
		int index)
	{
		source.ThrowIfNull().OnlyInDebug();
		index.Throw().IfLessThan(0);
		Contract.EndContractBlock();

		return SkipAtCore(source, index);

		static IEnumerable<T> SkipAtCore(IEnumerable<T> source, int index)
		{
			using var e = source.GetEnumerator();
			for (var count = 0; e.MoveNext(); count++)
			{
				if (count != index)
					yield return e.Current;
			}
		}
	}

	public static IEnumerable<T> ReplaceAt<T>(
		this IEnumerable<T> source,
		int index,
		T replacement)
	{
		source.ThrowIfNull().OnlyInDebug();
		index.Throw().IfLessThan(0);
		Contract.EndContractBlock();

		return ReplaceAtCore(source, index, replacement);

		static IEnumerable<T> ReplaceAtCore(IEnumerable<T> source, int index, T replacement)
		{
			using var e = source.GetEnumerator();
			for (var count = 0; e.MoveNext(); count++)
			{
				yield return count == index ? replacement : e.Current;
			}
		}
	}

	public static IEnumerable<T> InsertAt<T>(
		this IEnumerable<T> source,
		int index,
		IEnumerable<T> injection)
	{
		source.ThrowIfNull().OnlyInDebug();
		index.Throw().IfLessThan(0);
		injection.ThrowIfNull().OnlyInDebug();
		Contract.EndContractBlock();

		return InsertAtCore(source, index, injection);

		static IEnumerable<T> InsertAtCore(IEnumerable<T> source, int index, IEnumerable<T> injection)
		{
			using var e = source.GetEnumerator();
			for (var count = 0; e.MoveNext(); count++)
			{
				if (count == index)
				{
					// ReSharper disable once PossibleMultipleEnumeration
					foreach (var i in injection)
						yield return i;
				}

				yield return e.Current;
			}
		}
	}

	public static IEnumerable<T> InsertAt<T>(
		this IEnumerable<T> source,
		int index,
		T injection)
	{
		return index < 0
			? throw new ArgumentOutOfRangeException(nameof(index), index, "Must be at least zero.")
			: InsertAtCore(source, index, injection);

		static IEnumerable<T> InsertAtCore(IEnumerable<T> source, int index, T injection)
		{
			using var e = source.GetEnumerator();
			for (var count = 0; e.MoveNext(); count++)
			{
				if (count == index)
					yield return injection;

				yield return e.Current;
			}
		}
	}

	public static List<T> Extract<T>(
		this IList<T> target,
		Func<T, bool> predicate)
	{
		target.ThrowIfNull().OnlyInDebug();
		predicate.ThrowIfNull().OnlyInDebug();

		var extracted = ListPool<T>.Shared.Take();
		using var lease = MemoryPool<int>.Shared.Rent(target.Count);
		var removed = lease.Memory.Span;
		var removedCount = 0;
		var len = target.Count;

		for (var i = 0; i < len; i++)
		{
			var c = target[i];
			if (!predicate(c)) continue;
			extracted.Add(c);
			removed[removedCount++] = i;
		}

		// Doing this in reverse lessens the load on removing from the list.
		while (removedCount-- != 0)
			target.RemoveAt(removed[removedCount]);

		return extracted;
	}

	public static List<TExtract> ExtractType<TExtract>(
		this IList target)
	{
		target.ThrowIfNull().OnlyInDebug();
		var extracted = ListPool<TExtract>.Shared.Take();
		using var lease = MemoryPool<int>.Shared.Rent(target.Count);
		var removed = lease.Memory.Span;
		var removedCount = 0;
		var len = target.Count;

		for (var i = 0; i < len; i++)
		{
			if (target[i] is not TExtract e) continue;
			extracted.Add(e);
			removed[removedCount++] = i;
		}

		// Doing this in reverse lessens the load on removing from the list.
		while (removedCount-- != 0)
			target.RemoveAt(removed[removedCount]);

		return extracted;
	}
}
