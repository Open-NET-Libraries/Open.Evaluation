/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using System;

namespace Open.Evaluation
{
	public abstract class FunctionBase<TResult>
		: OperatorBase<IEvaluate, TResult>, IFunction<TResult>
		where TResult : IComparable
	{

		protected FunctionBase(char symbol, string symbolString, IEvaluate<TResult> evaluation) : base(symbol, symbolString)
		{
			if (evaluation == null)
				throw new ArgumentNullException("contents");

			Evaluation = evaluation;
			
			// Provide a standard means for discovery.
			ChildrenInternal.Add(evaluation);
		}

		public IEvaluate<TResult> Evaluation
		{
			get;
			private set;
		}

		protected override TResult EvaluateInternal(object context)
		{
			return Evaluation.Evaluate(context);
		}

		protected override string ToStringRepresentationInternal()
		{
			return ToStringInternal(Evaluation.ToStringRepresentation());
		}

	}

}