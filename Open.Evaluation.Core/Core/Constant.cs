/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Throw;

namespace Open.Evaluation.Core;

[DebuggerDisplay("Value = {Value}")]
public class Constant<TValue>
	: EvaluationBase<TValue>, IConstant<TValue>, IReproducable<TValue, IEvaluate<TValue>>
	where TValue : notnull, IEquatable<TValue>, IComparable<TValue>
{
	protected Constant(TValue value)
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
		=> catalog.Register(ToStringRepresentation(in value), _ => new Constant<TValue>(value));

	/// <inheritdoc />
	public virtual IEvaluate<TValue> NewUsing(ICatalog<IEvaluate<TValue>> catalog, TValue param)
		=> catalog.Register(ToStringRepresentation(in param), _ => new Constant<TValue>(param));
}

public static partial class ConstantExtensions
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
