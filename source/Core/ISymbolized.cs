/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using System.Diagnostics.CodeAnalysis;

namespace Open.Evaluation.Core
{
	[SuppressMessage("ReSharper", "UnusedMemberInSuper.Global")]
	public interface ISymbolized
	{
		char Symbol { get; }
		string SymbolString { get; }
	}
}
