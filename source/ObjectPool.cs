using Open.Disposable;
using System;
using System.Text;

namespace Open.Evaluation
{
	public static class ObjectPool<T>
		where T : class, new()
	{
		public static readonly OptimisticArrayObjectPool<T> Instance
			= OptimisticArrayObjectPool.Create<T>();
	}

	public static class StringBuilderPool
	{
		public static readonly ConcurrentQueueObjectPool<StringBuilder> Instance
			= new(
				() => new StringBuilder(),
				sb =>
				{
					sb.Clear();
					if (sb.Capacity > 16) sb.Capacity = 16;
				},
				null,
				1024);

		public static string Rent(Action<StringBuilder> action)
		{
			if (action is null) throw new ArgumentNullException(nameof(action));

			var sb = Instance.Take();
			action(sb);
			var result = sb.ToString();
			Instance.Give(sb);
			return result;
		}
	}
}
