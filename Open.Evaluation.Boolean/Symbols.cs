using Open.Evaluation.Core;

namespace Open.Evaluation.Boolean;
public static class Symbols
{
	public static readonly Symbol Counting = new(Glyphs.Counting, ", ");
	public static readonly Symbol And = new(Glyphs.And, true);
	public static readonly Symbol Or = new(Glyphs.Or, true);
	public static readonly Symbol Not = new(Glyphs.Not);
	public static readonly Symbol Conditional = new(Glyphs.Conditional, true);
}

public static class Glyphs
{
	public const char Counting = ',';
	public const char And = '&';
	public const char Or = '|';
	public const char Not = '!';
	public const char Conditional = '?';
}