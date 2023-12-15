using System.Diagnostics.CodeAnalysis;
using Throw;

namespace Open.Evaluation.Core;
public readonly record struct Symbol
{
	public Symbol(char character, bool pad = false)
		: this(character, pad ? $" {character} " : new string(character, 1))
	{ }

	public Symbol(char character, string text)
	{
		text.ThrowIfNull();

		Character = character;
		Text = text;
	}

	public char Character { get; }
	public string Text { get; }

	public static implicit operator char(Symbol symbol) => symbol.Character;

	public static implicit operator string(Symbol symbol) => symbol.Text;
}
