using System.Numerics;
using System.Runtime.CompilerServices;

namespace Open.Evaluation.Arithmetic;
internal static class Value<T> where T
	: notnull, INumber<T>
{
	public static readonly T Two = T.One + T.One;
	public static readonly T Three = Two + T.One;

	static bool CheckFloat()
	{
		var onepointfive = Three / Two;
		return onepointfive > T.One && onepointfive < Two;
	}

	public static readonly bool IsFloatingPoint = CheckFloat();
}

internal static class ValueFloat<T> where T
	: notnull, INumber<T>, IFloatingPoint<T>
{
	public static readonly T Two = T.One + T.One;
	public static readonly T Half = T.One / Two;
}

internal static class ValueUtility
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsInteger<T>(this T value)
		where T : notnull, INumber<T> => value % T.One == T.Zero;
}