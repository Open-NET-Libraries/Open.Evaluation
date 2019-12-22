using Open.Evaluation.Core;
using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Open.Evaluation
{
	public class ParameterContext // Using a Lazy to differentiate between the value and the factories.  Also ensures execution and publication.
		: ConcurrentDictionary<IEvaluate, Lazy<object>>, IDisposable
	{
		public object Context { get; private set; }

		public ParameterContext(object context)
		{
			Context = context;
		}

		[return: NotNull]
		public TResult GetOrAdd<TResult>(IEvaluate key, Func<IEvaluate, TResult> factory)
			=> base.GetOrAdd(key, k => new Lazy<object>(() => factory(k)!)).Value is TResult r ? r
				: throw new InvalidCastException("Result doesn't match factory return type.");

		public void Dispose()
		{
			Context = null!;
			Clear();
		}
	}
}
