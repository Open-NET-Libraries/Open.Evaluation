using Open.Evaluation.Core;
using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Open.Evaluation;

public class ParameterContext // Using a Lazy to differentiate between the value and the factories.  Also ensures execution and publication.
	: ConcurrentDictionary<IEvaluate, Lazy<object>>, IDisposable
{
	public object Context { get; private set; }

	public ParameterContext(object context)
		=> Context = context ?? throw new ArgumentNullException(nameof(context));

	private const string DoesntMatch = "Result doesn't match factory return type.";

	public TResult GetOrAdd<TResult>(IEvaluate key, Func<IEvaluate, TResult> factory)
		=> Context is null
			? throw new ObjectDisposedException(nameof(ParameterContext))
			: base.GetOrAdd(key, k => new Lazy<object>(() => factory(k)!)).Value is TResult r
			? r : throw new InvalidCastException(ParameterContext.DoesntMatch);

	public TResult GetOrAdd<TResult>(IEvaluate key, Func<TResult> factory)
		=> Context is null
			? throw new ObjectDisposedException(nameof(ParameterContext))
			: base.GetOrAdd(key, _ => new Lazy<object>(factory)).Value is TResult r
			? r : throw new InvalidCastException(ParameterContext.DoesntMatch);

	#region IDisposable Support
	private bool disposedValue; // To detect redundant calls

	protected virtual void Dispose(bool disposing)
	{
		if (!disposedValue)
		{
			if (disposing)
			{
				Context = null!;
				Clear();
			}

			// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
			// TODO: set large fields to null.

			disposedValue = true;
		}
	}

	// This code added to correctly implement the disposable pattern.
	[SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize", Justification = "<Pending>")]
	public void Dispose() =>
		// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		Dispose(true);// TODO: uncomment the following line if the finalizer is overridden above.// GC.SuppressFinalize(this);
	#endregion
}
