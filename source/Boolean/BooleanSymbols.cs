using Open.Evaluation.Core;

namespace Open.Evaluation.Boolean;
public static class BooleanSymbols
{
	public static readonly Symbol And = new(Registry.Boolean.AND, true);
	public static readonly Symbol Or = new(Registry.Boolean.OR, true);
	public static readonly Symbol Not = new(Registry.Boolean.NOT);
	public static readonly Symbol Conditional = new(Registry.Boolean.CONDITIONAL, true);
}
