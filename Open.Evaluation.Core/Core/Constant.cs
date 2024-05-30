namespace Open.Evaluation.Core;

[DebuggerDisplay("Value = {Value}")]
public class Constant<T>
	: EvaluationBase<T>, IConstant<T>, IReproducable<T, IEvaluate<T>>
	where T : notnull, IEquatable<T>, IComparable<T>
{
	protected Constant(ICatalog<IEvaluate<T>> catalog,  in T value)
		: base(catalog)
	{
		Value = value.ThrowIfNull();
		_result = new(Value, Description);
	}

	/// <inheritdoc />
	public T Value
	{
		get;
	}

	private readonly EvaluationResult<T> _result;

	protected static string ToStringRepresentation(in T value)
	{
		Debug.Assert(value is not null);
		return value.ToString()!;
	}

	protected override string Describe()
		=> ToStringRepresentation(Value);

	protected override EvaluationResult<T> EvaluateInternal(Context context)
		=> _result;

	internal static Constant<T> Create(ICatalog<IEvaluate<T>> catalog, T value)
	{
		// TODO: maybe introduce a faster method of acquiring a contant?
		var constant = catalog.Register(ToStringRepresentation(in value), value, (_, c, v) => new Constant<T>(c, v));
		Debug.Assert(constant.Value.Equals(value));
		return constant;
	}

	/// <inheritdoc cref="IReproducable{TParam, TEval}.NewUsing(ICatalog{TEval}, TParam)" />
	public virtual Constant<T> NewUsing(ICatalog<IEvaluate<T>> catalog, T param)
		=> Create(catalog, param);

	/// <inheritdoc cref="IReproducable{TParam, TEval}.NewUsing(TParam)" />
	public Constant<T> NewUsing(T param)
		=> NewUsing(Catalog, param);
	IEvaluate<T> IReproducable<T, IEvaluate<T>>.NewUsing(ICatalog<IEvaluate<T>> catalog, T param)
		=> NewUsing(Catalog, param);
	IEvaluate<T> IReproducable<T, IEvaluate<T>>.NewUsing(T param)
		=> NewUsing(param);

	public static implicit operator T(Constant<T> constant)
		=> constant.Value;
}

public static class ConstantExtensions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Constant<TValue> GetConstant<TValue>(
		this ICatalog<IEvaluate<TValue>> catalog,
		in TValue value)
		where TValue : notnull, IEquatable<TValue>, IComparable<TValue>
	{
		catalog.ThrowIfNull().OnlyInDebug();

		// ReSharper disable once SuspiciousTypeConversion.Global
		return Constant<TValue>.Create(catalog, value);
	}
}
