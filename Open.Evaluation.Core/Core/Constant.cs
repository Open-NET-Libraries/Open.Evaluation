/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

namespace Open.Evaluation.Core;

[DebuggerDisplay("Value = {Value}")]
public class Constant<TValue>
	: EvaluationBase<TValue>, IConstant<TValue>, IReproducable<TValue, IEvaluate<TValue>>
	where TValue : notnull, IEquatable<TValue>, IComparable<TValue>
{
	protected Constant(in TValue value)
	{
		Value = value.ThrowIfNull();
		_result = new(Value, Description);
	}

	/// <inheritdoc />
	public TValue Value
	{
		get;
	}

	private readonly EvaluationResult<TValue> _result;

	protected static string ToStringRepresentation(in TValue value)
	{
		Debug.Assert(value is not null);
		return value.ToString()!;
	}

	protected override string Describe()
		=> ToStringRepresentation(Value);

	protected override EvaluationResult<TValue> EvaluateInternal(Context context)
		=> _result;

	internal static Constant<TValue> Create(ICatalog<IEvaluate<TValue>> catalog, TValue value)
	{
		// TODO: maybe introduce a faster method of acquiring a contant?
		var constant = catalog.Register(ToStringRepresentation(in value), _ => new Constant<TValue>(value));
		Debug.Assert(constant.Value.Equals(value));
		return constant;
	}

	/// <inheritdoc />
	public virtual IEvaluate<TValue> NewUsing(ICatalog<IEvaluate<TValue>> catalog, TValue param)
		=> Create(catalog, param);

	public static implicit operator TValue(Constant<TValue> constant)
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
