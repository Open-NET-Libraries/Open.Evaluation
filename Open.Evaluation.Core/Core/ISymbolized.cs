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
	/// The symbol that differentiates this.
	/// </summary>
	Symbol Symbol { get; }
}
