using Open.Evaluation.Core;

namespace Open.Evaluation.Boolean;
public static class Symbols
{
	public static readonly Symbol Counting = new(Boolean.Counting.CountingBase.Glyph, ", ");
	public static readonly Symbol And = new(Boolean.And.Glyph, true);
	public static readonly Symbol Or = new(Boolean.Or.Glyph, true);
	public static readonly Symbol Not = new(Boolean.Not.Glyph);
	public static readonly Symbol Conditional = new(Boolean.Conditional.Glyph, true);
}