using Open.Evaluation.Core;
using System.Collections.Immutable;

namespace Open.Evaluation.Arithmetic;
public static class Symbols
{
	public static readonly Symbol Sum = new(Glyphs.Sum, true);
	public static readonly Symbol Product = new(Glyphs.Product, true);
	public static readonly Symbol Exponent = new(Glyphs.Exponent);
}

internal static class Glyphs
{
	public const char Sum = '+';
	public const char Product = '*';
	public const char Exponent = '^';

	public const char Square = '²';
	public const char Invert = '/';
	public const char SquareRoot = '√';

	public static readonly ImmutableArray<char> Operators
		= ImmutableArray.Create(Sum, Product);

	public static readonly ImmutableArray<char> Functions
		= ImmutableArray.Create(Square, Invert, SquareRoot);
}