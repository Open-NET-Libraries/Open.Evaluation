/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */


namespace Open.Evaluation.Core
{
	public interface IReducibleEvaluation<T> : IEvaluate
		where T : IEvaluate
	{
		/// <returns>Returns this instance if no reduction possible.  Otherwise returns the reduction.</returns>
		bool TryGetReduced(ICatalog<T> catalog, out T reduction);
	}

}
