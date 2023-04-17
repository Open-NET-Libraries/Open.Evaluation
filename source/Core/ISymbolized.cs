/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

namespace Open.Evaluation.Core;

/// <summary>
/// Represents something that has a symbol. For example, "sum" operator's symbol is: <c>+</c>
/// </summary>
public interface ISymbolized
{
	/// <summary>
	/// A single character which differentiates this.
	/// </summary>
	char Symbol { get; }

	/// <summary>
	/// A potential longer form version of the symbol.
	/// </summary>
	string SymbolString { get; }
}
