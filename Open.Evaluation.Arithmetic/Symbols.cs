using Open.Evaluation.Core;

namespace Open.Evaluation.Arithmetic;
public static class Symbols
{
	public static readonly Symbol Sum = new(Glyphs.Sum, true);
	public static readonly Symbol Product = new(Glyphs.Product, true);
	public static readonly Symbol Exponent = new(Glyphs.Exponent);
}

public static class Glyphs
{
	public const char Sum = '+';
	public const char Product = '*';
	public const char Exponent = '^';

	public const char Square = '²';
	public const char Invert = '/';
	public const char SquareRoot = '√';
}