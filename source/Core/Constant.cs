/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

namespace Open.Evaluation.Core;

[DebuggerDisplay("Value = {Value}")]
public class Constant<TValue>
	: EvaluationBase<TValue>, IConstant<TValue>, IReproducable<TValue, IEvaluate<TValue>>
	where TValue : notnull, IComparable<TValue>, IComparable
{
	protected Constant(TValue value) => Value = value;

	/// <inheritdoc />
	public TValue Value
	{
		get;
	}

	IComparable IConstant.Value => Value;

	protected static string ToStringRepresentation(in TValue value)
	{
		Debug.Assert(value is not null);
		return value.ToString();
	}

	protected override string ToStringRepresentationInternal()
		=> ToStringRepresentation(Value);

	protected override TValue EvaluateInternal(object context) => Value;

	protected override string ToStringInternal(object context) => ToStringRepresentation();

	internal static Constant<TValue> Create(ICatalog<IEvaluate<TValue>> catalog, TValue value)
		=> catalog.Register(ToStringRepresentation(in value), _ => new Constant<TValue>(value));

	/// <inheritdoc />
	public virtual IEvaluate<TValue> NewUsing(ICatalog<IEvaluate<TValue>> catalog, TValue param)
		=> catalog.Register(ToStringRepresentation(in param), _ => new Constant<TValue>(param));

	public static implicit operator TValue(Constant<TValue> c)
		=> c.Value;

	public static TValue operator *(Constant<TValue> a, Constant<TValue> b)
		=> (dynamic)a.Value * (dynamic)b.Value;

	public static TValue operator +(Constant<TValue> a, Constant<TValue> b)
		=> (dynamic)a.Value + (dynamic)b.Value;

	// Avoid repeated boxing unboxing.
	internal static readonly Lazy<TValue> Zero = new(() => (TValue)(dynamic)0);
	internal static readonly Lazy<TValue> One = new(() => (TValue)(dynamic)1);
	internal static readonly Lazy<TValue> NegativeOne = new(() => (TValue)(dynamic)(-1));
	internal static readonly Lazy<TValue> FloatNaN = new(() => (TValue)(dynamic)float.NaN);
	internal static readonly Lazy<TValue> DoubleNaN = new(() => (TValue)(dynamic)double.NaN);

	internal static readonly Func<IConstant<TValue>, bool> IsFloatNaN = c => c is IConstant<float> d && float.IsNaN(d.Value);
	internal static readonly Func<IConstant<TValue>, bool> IsDoubleNaN = c => c is IConstant<double> d && double.IsNaN(d.Value);
}

public static partial class ConstantExtensions
{
	public static Constant<TValue> GetConstant<TValue>(
		this ICatalog<IEvaluate<TValue>> catalog,
		in TValue value)
		where TValue : notnull, IComparable<TValue>, IComparable
	{
		Debug.Assert(catalog is not null);
		// ReSharper disable once SuspiciousTypeConversion.Global
		return catalog is ICatalog<IEvaluate<double>> dCat && value is double d
			? (Constant<TValue>)(dynamic)Constant.Create(dCat, d)
			: Constant<TValue>.Create(catalog, value);
	}

	public static Constant<TValue> SumOfConstants<TValue>(
		this ICatalog<IEvaluate<TValue>> catalog,
		in TValue c1, IEnumerable<IConstant<TValue>> constants)
		where TValue : notnull, IComparable<TValue>, IComparable
	{
		catalog.ThrowIfNull();
		constants.ThrowIfNull();

		if (typeof(TValue) == typeof(float))
		{
			// ReSharper disable once PossibleMultipleEnumeration
			if (float.IsNaN((float)(dynamic)c1) || constants.Any(Constant<TValue>.IsFloatNaN))
				return catalog.GetConstant(Constant<TValue>.FloatNaN.Value);
		}

		if (typeof(TValue) == typeof(double))
		{
			// ReSharper disable once PossibleMultipleEnumeration
			if (double.IsNaN((double)(dynamic)c1) || constants.Any(Constant<TValue>.IsDoubleNaN))
				return catalog.GetConstant(Constant<TValue>.DoubleNaN.Value);
		}

		dynamic result = c1;
		// ReSharper disable once PossibleMultipleEnumeration
		// ReSharper disable once LoopCanBeConvertedToQuery
		foreach (var c in constants)
		{
			result += c.Value;
		}
		return GetConstant(catalog, (TValue)result);
	}

	public static Constant<TValue> SumOfConstants<TValue>(
		this ICatalog<IEvaluate<TValue>> catalog,
		IEnumerable<IConstant<TValue>> constants)
		where TValue : notnull, IComparable<TValue>, IComparable
		=> SumOfConstants(catalog, Constant<TValue>.Zero.Value, constants);

	public static Constant<TValue> SumOfConstants<TValue>(
		this ICatalog<IEvaluate<TValue>> catalog,
		in TValue c1, in IConstant<TValue> c2, params IConstant<TValue>[] rest)
		where TValue : notnull, IComparable<TValue>, IComparable
		=> SumOfConstants(catalog, c1, rest.Prepend(c2));

	public static Constant<TValue> SumOfConstants<TValue>(
		this ICatalog<IEvaluate<TValue>> catalog,
		in IConstant<TValue> c1, in IConstant<TValue> c2, params IConstant<TValue>[] rest)
		where TValue : notnull, IComparable<TValue>, IComparable
	{
		c1.ThrowIfNull();
		c2.ThrowIfNull();
		Contract.EndContractBlock();

		return SumOfConstants(catalog, c1.Value, rest.Prepend(c2));
	}

	public static Constant<TValue> ProductOfConstants<TValue>(
		this ICatalog<IEvaluate<TValue>> catalog,
		in TValue c1, IEnumerable<IConstant<TValue>> constants)
		where TValue : notnull, IComparable<TValue>, IComparable
	{
		catalog.ThrowIfNull();
		constants.ThrowIfNull();

		if (typeof(TValue) == typeof(float))
		{
			// ReSharper disable once PossibleMultipleEnumeration
			if (float.IsNaN((float)(dynamic)c1) || constants.Any(Constant<TValue>.IsFloatNaN))
				return catalog.GetConstant(Constant<TValue>.FloatNaN.Value);
		}

		if (typeof(TValue) == typeof(double))
		{
			// ReSharper disable once PossibleMultipleEnumeration
			if (double.IsNaN((double)(dynamic)c1) || constants.Any(Constant<TValue>.IsDoubleNaN))
				return catalog.GetConstant(Constant<TValue>.DoubleNaN.Value);
		}

		dynamic zero = (TValue)(dynamic)0;
		dynamic result = c1;
		// ReSharper disable once PossibleMultipleEnumeration
		foreach (var c in constants)
		{
			var val = c.Value;
			if (val == zero) return GetConstant(catalog, (TValue)zero);
			result *= val;
		}
		return GetConstant(catalog, (TValue)result);
	}

	public static Constant<TValue> ProductOfConstants<TValue>(
		this ICatalog<IEvaluate<TValue>> catalog,
		IEnumerable<IConstant<TValue>> constants)
		where TValue : notnull, IComparable<TValue>, IComparable
		=> ProductOfConstants(catalog, (TValue)(dynamic)1, constants);

	public static Constant<TValue> ProductOfConstants<TValue>(
		this ICatalog<IEvaluate<TValue>> catalog,
		in IConstant<TValue> c1,
		in IConstant<TValue> c2,
		params IConstant<TValue>[] rest)
		where TValue : notnull, IComparable<TValue>, IComparable
	{
		c1.ThrowIfNull();
		c2.ThrowIfNull();
		Contract.EndContractBlock();

		return ProductOfConstants(catalog, c1.Value, rest.Prepend(c2));
	}

	public static Constant<TValue> ProductOfConstants<TValue>(
		this ICatalog<IEvaluate<TValue>> catalog,
		in TValue c1, IConstant<TValue> c2, params IConstant<TValue>[] rest)
		where TValue : notnull, IComparable<TValue>, IComparable
		=> ProductOfConstants(catalog, c1, rest.Prepend(c2));
}
