/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

using System.Diagnostics.CodeAnalysis;

namespace Open.Evaluation.Core;

public interface IReducibleEvaluation<T> : IEvaluate
	where T : IEvaluate
{
	/// <summary>
	/// Attempts a recduction on this instance.
	/// </summary>
	/// <returns><see langword="true"/> if <paramref name="reduction"/> is different than this isntance; otherwise <see langword="false"/>.</returns>
	bool TryGetReduced(ICatalog<T> catalog, [NotNullWhen(true)] out T reduction);
}
