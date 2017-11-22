/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using System;

namespace Open.Evaluation.Core
{
	public abstract class OperationBase<TResult>
		: EvaluationBase<TResult>, IFunction<TResult>, IReducibleEvaluation<IEvaluate<TResult>>
	{

		protected OperationBase(char symbol, string symbolString) : base()
		{
			SymbolString = symbolString ?? throw new ArgumentNullException("symbolString");
			Symbol = symbol;
		}

		public char Symbol { get; private set; }
		public string SymbolString { get; private set; }

		protected override string ToStringInternal(object contents)
		{
			return $"{SymbolString}({contents})";
		}

		protected virtual IEvaluate<TResult> Reduction(ICatalog<IEvaluate<TResult>> catalog)
		{
			return this;
		}

		// Override this if reduction is possible.  Return null if you can't reduce.
		public bool TryGetReduced(ICatalog<IEvaluate<TResult>> catalog, out IEvaluate<TResult> reduction)
		{
			reduction = Reduction(catalog);
			return reduction!=this;
		}
	}

}