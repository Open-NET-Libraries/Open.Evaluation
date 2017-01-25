using System;
using System.Threading;

namespace EvaluationFramework
{
	public abstract class EvaluationBase<TContext, TResult> : IEvaluate<TContext, TResult>
	{
		protected EvaluationBase()
		{
			ResetToStringRepresentation();
		}
		public abstract TResult Evaluate(TContext context);

		public abstract string ToString(TContext context);

		protected abstract string ToStringRepresentationInternal();
		Lazy<string> _toStringRepresentation;
		public string ToStringRepresentation()
		{
			return _toStringRepresentation.Value;
		}

		// Preferrably this should never be needed, but because of potential implementations it is exposed for assurance of control.
		public void ResetToStringRepresentation()
		{
			Interlocked.Exchange(
				ref _toStringRepresentation,
				new Lazy<string>(ToStringRepresentationInternal, LazyThreadSafetyMode.ExecutionAndPublication)
			);
		}

	}
}