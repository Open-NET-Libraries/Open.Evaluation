using Open.Evaluation.Core;

namespace Open.Evaluation.Arithmetic;
public static class ArithmeticSymbols
{
	public static readonly Symbol Sum = new('+', true);
	public static readonly Symbol Product = new('*', true);
	public static readonly Symbol Exponent = new('^');
}
