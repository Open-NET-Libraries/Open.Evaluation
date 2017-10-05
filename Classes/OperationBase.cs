/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using System;

namespace Open.Evaluation
{
	public abstract class OperationBase<TResult>
		: EvaluationBase<TResult>, IFunction<TResult>, IReducibleEvaluation<TResult>
	{

		protected OperationBase(char symbol, string symbolString) : base()
		{
			if (symbolString == null)
				throw new ArgumentNullException("symbolString");

			Symbol = symbol;
			SymbolString = symbolString;
		}

		public char Symbol { get; private set; }
		public string SymbolString { get; private set; }

		protected override string ToStringInternal(object contents)
		{
			return string.Format("{0}({1})", SymbolString, contents);
		}

		public IEvaluate<TResult> AsReduced()
		{
			var r = Reduction();
			if (r != null && r.ToStringRepresentation() == this.ToStringRepresentation()) r = this;
			return r ?? this;
		}

		// Override this if reduction is possible.  Return null if you can't reduce.
		public virtual IEvaluate<TResult> Reduction()
		{
			return null;
		}


	}

}