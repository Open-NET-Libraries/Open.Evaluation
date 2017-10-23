using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Open.Evaluation
{
    internal static class Utility
    {
		public static IEnumerable<T> Concat<T>(this IEnumerable<T> source, T next)
		{
			return source
				.Concat(Enumerable.Repeat(next, 1));
		}

		public static IEnumerable<T> ReplaceAt<T>(this IEnumerable<T> source, int index, T replacement)
		{
			if (index < 0) throw new ArgumentOutOfRangeException("index", index, "Must be at least zero.");

			return source
				.Take(index)
				.Concat(replacement)
				.Concat(source.Skip(index + 1));
		}

		public static IEnumerable<T> Splice<T>(this IEnumerable<T> source, int index, IEnumerable<T> injection)
		{
			if (index < 0) throw new ArgumentOutOfRangeException("index", index, "Must be at least zero.");

			return source
				.Take(index)
				.Concat(injection)
				.Concat(source.Skip(index));
		}

		public static IEnumerable<T> Splice<T>(this IEnumerable<T> source, int index, T injection)
		{
			if (index < 0) throw new ArgumentOutOfRangeException("index", index, "Must be at least zero.");

			return source
				.Take(index)
				.Concat(injection)
				.Concat(source.Skip(index));
		}
	}
}
