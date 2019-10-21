/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using System.Threading;

namespace Open.Evaluation.Core
{

	/*
	 * The main idea here is to envorce an immutable class and it's related sub classes.  No changes allowed after construction
	 * A clone can only be created by 'recreating' or 'reconstructing'.
	 */

	public abstract class EvaluationBase<TResult> : IEvaluate<TResult>
	{
		protected abstract string ToStringRepresentationInternal();
		string _toStringRepresentation; // Was using a Lazy<string> before, but seems overkill for an immutable structure.
		public string ToStringRepresentation()
		{
			return LazyInitializer.EnsureInitialized(ref _toStringRepresentation, ToStringRepresentationInternal);
		}

		protected abstract TResult EvaluateInternal(object context); // **

		protected abstract string ToStringInternal(object context);

		object IEvaluate.Evaluate(object context) => Evaluate(context);

		/// <inheritdoc />
		public TResult Evaluate(object context)
		{
			// Use existing context... // Caches results...
			if (context is ParameterContext pc)
				return pc.GetOrAdd(this, k => EvaluateInternal(pc)); // **

			// Create a new one for this tree...
			using (var newPc = new ParameterContext(context))
				return Evaluate(newPc);
		}

		/// <inheritdoc />
		public virtual string ToString(object context) => ToStringInternal(Evaluate(context));

	}

}
