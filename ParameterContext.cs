using Open.Evaluation.Core;
using System;
using System.Collections.Concurrent;

namespace Open.Evaluation
{
	public class ParameterContext
		: ConcurrentDictionary<IEvaluate, object>, IDisposable
	{
		public object Context { get; private set; }

		public ParameterContext(object context)
		{
			Context = context;
		}

		public TResult GetOrAdd<TResult>(IEvaluate key, Func<IEvaluate, TResult> factory)
		{
			return base.GetOrAdd(key, factory) is TResult r ? r
				: throw new InvalidCastException("Result doesn't match factory return type.");
		}

		public void Dispose()
		{
			Context = null;
			Clear();
		}
	}
}
