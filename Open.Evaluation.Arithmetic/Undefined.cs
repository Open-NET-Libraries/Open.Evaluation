/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

namespace Open.Evaluation.Arithmetic;

/// <summary>
/// The reduction of an expression that is undefined <em>everywhere</em> -- a constant zero raised
/// to a negative power (division by zero: <c>x / 0</c> is <c>x · 0⁻¹</c>), <c>0⁰</c> under
/// <see cref="Exponent.PowerOfZeroReduction.Undefined"/>, or any expression that contains such a
/// sub-expression. Because reductions are reliable, an invalid reduction condemns its source: if
/// <c>(a - a)⁻¹</c> reduces to Undefined, the original was undefined too. Reduction is therefore the
/// validity detector -- see <see cref="UndefinedExtensions.IsValid{T}"/>.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately <b>not</b> an <see cref="IConstant{T}"/>: reduction folds constants by reading their
/// values, and an undefined "value" must never take part in that. As a distinct leaf it is invisible
/// to every constant-folding site (exactly as a parameter is); each operator's reduction instead
/// checks explicitly for it and reduces to it -- containment poisons the whole tree.
/// </para>
/// <para>
/// An expression that is undefined only at <em>some</em> inputs (<c>1/{0}</c>, <c>√{0}</c>) is NOT
/// Undefined -- that is a pole, handled numerically at evaluation. Only forms that constant-fold to
/// an undefined value are.
/// </para>
/// <para>
/// Evaluating it never throws: the result is NaN for numeric types that have one (double, float,
/// Half, NFloat) and zero for every other (decimal, all integers, BigInteger). Doing so is
/// nonetheless a caller error -- validate before evaluating -- and DEBUG builds fail fast on it so
/// the value is never silently observed in normal operation.
/// </para>
/// </remarks>
[DebuggerDisplay("Undefined")]
public sealed class Undefined<T> : EvaluationBase<T>, IUndefined<T>
	where T : notnull, INumber<T>
{
	/// <summary>The rendering of every Undefined expression -- and its catalog key.</summary>
	public const string Token = "Undefined";

	// Saturating conversion of NaN is exactly the rule "NaN where the type has one, zero elsewhere"
	// across all of INumber<T> -- no reflection, no interface sniffing, no static-init hazards.
	private static readonly T ResultValue = T.CreateSaturating(double.NaN);

	private readonly EvaluationResult<T> _result;

	private Undefined(ICatalog<IEvaluate<T>> catalog)
		: base(catalog)
	{
		_result = new(ResultValue, Description);
	}

	protected override string Describe() => Token;

	protected override EvaluationResult<T> EvaluateInternal(Context context)
	{
		Debug.Fail("An Undefined expression was evaluated. Validate before evaluating "
			+ "(Catalog.IsValid / reduction); this value must never be observed in normal operation.");
		return _result;
	}

	internal static Undefined<T> Create(ICatalog<IEvaluate<T>> catalog)
		=> catalog.Register(Token, static (_, c) => new Undefined<T>(c));
}

public static class UndefinedExtensions
{
	/// <summary>
	/// The catalog's single interned <see cref="Undefined{T}"/> expression (find-or-create).
	/// </summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Undefined<T> GetUndefined<T>(this ICatalog<IEvaluate<T>> catalog)
		where T : notnull, INumber<T>
	{
		catalog.ThrowIfNull().OnlyInDebug();
		return Undefined<T>.Create(catalog);
	}

	/// <summary>True if <paramref name="evaluation"/> is the Undefined expression itself.</summary>
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsUndefined(this IEvaluate evaluation)
		=> evaluation is IUndefined;

	/// <summary>
	/// True if <paramref name="evaluation"/> is valid: its reduction (memoized by the catalog) is
	/// not Undefined. Consumers should check this once, when admitting an expression, and never
	/// evaluate an invalid one.
	/// </summary>
	public static bool IsValid<T>(this ICatalog<IEvaluate<T>> catalog, IEvaluate<T> evaluation)
		where T : notnull, INumber<T>
	{
		catalog.ThrowIfNull().OnlyInDebug();
		evaluation.ThrowIfNull().OnlyInDebug();
		return catalog.GetReduced(evaluation) is not IUndefined;
	}
}
