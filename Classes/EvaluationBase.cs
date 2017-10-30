/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using System;
using System.Threading;

namespace Open.Evaluation
{

	public abstract class EvaluationBase<TResult> : IEvaluate<TResult>
	{
		protected EvaluationBase()
		{
			ResetToStringRepresentation();
		}

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
				new Lazy<string>(
					ToStringRepresentationInternal,
					LazyThreadSafetyMode.ExecutionAndPublication)
			);
		}

		protected abstract TResult EvaluateInternal(object context);

		protected abstract string ToStringInternal(object context);

		object IEvaluate.Evaluate(object context)
		{
			return Evaluate(context);
		}

		public TResult Evaluate(object context)
		{
			// Use existing context... // Caches results...
			if (context is ParameterContext pc)
				return pc.GetOrAdd(this, k => EvaluateInternal(pc));

			// Create a new one for this tree...
			using (var newPc = new ParameterContext(context))
				return Evaluate(newPc);
		}

		public virtual string ToString(object context)
		{
			return ToStringInternal(EvaluateInternal(context));
		}

	}
	
}