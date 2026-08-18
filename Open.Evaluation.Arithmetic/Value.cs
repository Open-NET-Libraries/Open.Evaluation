namespace Open.Evaluation.Arithmetic;
internal static class Value<T> where T
	: notnull, INumber<T>
{
	public static readonly T Two = T.One + T.One;
	public static readonly T Three = Two + T.One;

	// NOTE: deliberately NO Half here. A named half in INumber space is an attractive
	// nuisance -- it truncates to zero for integer T. ValueFloat<T>.Half (IFloatingPoint-
	// constrained) is the only named half; sites that provably gate on IsFloatingPoint
	// compute the value inline at the point of use instead of importing a name.

	[Pure]
	static bool CheckFloat()
	{
		T onepointfive = Three / Two;
		return onepointfive > T.One && onepointfive < Two;
	}

	public static readonly bool IsFloatingPoint = CheckFloat();

	// Cached once per closed generic T (not re-checked per call). Exponentiation-by-squaring
	// is bit-identical to a sequential multiplication loop only when T's unchecked
	// multiplication is an exact commutative-ring operation: true for T : IBinaryInteger<T>
	// (mod 2^n for fixed-width integers, exact for arbitrary-precision types such as
	// BigInteger), where grouping/order of the multiplications can never change the
	// resulting bit pattern -- including wraparound overflow. It is NOT true for
	// floating-point T, where multiplication is not associative under rounding, so those
	// types must keep using the sequential loop. See Exponent.Pow<T> for the guarded usage.
	// NOTE: deliberately NOT typeof(IBinaryInteger<>).MakeGenericType(typeof(T)).IsAssignableFrom(...):
	// IBinaryInteger<TSelf> constrains TSelf : IBinaryInteger<TSelf>, so MakeGenericType throws
	// ArgumentException for any T that doesn't already satisfy that constraint (e.g. double/float/
	// decimal) -- and since that throw happens inside this static field initializer, it permanently
	// poisons Value<T> for that T (TypeInitializationException on every subsequent access, including
	// IsFloatingPoint above, for the lifetime of the process). Inspecting T's already-realized
	// interface list instead never asks the type system to construct an illegal instantiation.
	public static readonly bool IsBinaryInteger = Array.Exists(
		typeof(T).GetInterfaces(),
		static i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IBinaryInteger<>));
}

internal static class ValueFloat<T> where T
	: notnull, INumber<T>, IFloatingPoint<T>
{
	public static readonly T Two = T.One + T.One;
	public static readonly T Half = T.One / Two;
}

internal static class ValueUtility
{
	[Pure]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsInteger<T>(this T value)
		where T : notnull, INumber<T> => value % T.One == T.Zero;
}