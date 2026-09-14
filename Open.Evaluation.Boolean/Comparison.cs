using Open.Evaluation.Core;
using System.Diagnostics.Contracts;
using System.Numerics;
using Throw;

namespace Open.Evaluation.Boolean;

/// <summary>
/// True when <see cref="Left"/> is strictly greater than <see cref="Right"/>.
/// </summary>
/// <remarks>
/// The comparison goes through <typeparamref name="T"/>'s own
/// <see cref="IComparisonOperators{TSelf, TOther, TResult}"/> rather than
/// <see cref="IComparable{T}.CompareTo(T)"/>, so it inherits that type's own idea of ordering: for
/// a floating-point <typeparamref name="T"/> this is IEEE 754, where any comparison touching NaN
/// is simply false -- never true, never thrown. CompareTo instead ranks NaN as an ordinary (if
/// surprising) value, which is not the answer a symbolic-regression condition should silently get.
/// </remarks>
public sealed partial class GreaterThan<T>
	: OperatorBase<IEvaluate<T>, bool>,
		IReproducable<(IEvaluate<T> Left, IEvaluate<T> Right), IEvaluate<bool>>
	where T : notnull, IEquatable<T>, IComparable<T>, IComparisonOperators<T, T, bool>
{
	private GreaterThan(
		ICatalog<IEvaluate<bool>> catalog,
		IEvaluate<T> left,
		IEvaluate<T> right)
		: base(catalog, Symbols.GreaterThan, [left, right])
	{
		left.ThrowIfNull().OnlyInDebug();
		right.ThrowIfNull().OnlyInDebug();
		Left = left;
		Right = right;
	}

	public IEvaluate<T> Left { get; }

	public IEvaluate<T> Right { get; }

	protected override EvaluationResult<bool> EvaluateInternal(Context context)
	{
		EvaluationResult<T> left = Left.Evaluate(context);
		EvaluationResult<T> right = Right.Evaluate(context);
		return new(left.Result > right.Result, Describe([left.Description, right.Description]));
	}

	internal static GreaterThan<T> Create(
		ICatalog<IEvaluate<bool>> catalog,
		(IEvaluate<T> Left, IEvaluate<T> Right) param)
	{
		catalog.ThrowIfNull().OnlyInDebug();
		param.Left.ThrowIfNull().OnlyInDebug();
		param.Right.ThrowIfNull().OnlyInDebug();
		Contract.EndContractBlock();

		return catalog.Register(new GreaterThan<T>(catalog, param.Left, param.Right));
	}

	public GreaterThan<T> NewUsing(
		ICatalog<IEvaluate<bool>> catalog,
		(IEvaluate<T> Left, IEvaluate<T> Right) param)
		=> Create(catalog, param);

	public GreaterThan<T> NewUsing((IEvaluate<T> Left, IEvaluate<T> Right) param)
		=> NewUsing(Catalog, param);

	IEvaluate<bool> IReproducable<(IEvaluate<T> Left, IEvaluate<T> Right), IEvaluate<bool>>.NewUsing(ICatalog<IEvaluate<bool>> catalog, (IEvaluate<T> Left, IEvaluate<T> Right) param)
		=> NewUsing(catalog, param);

	IEvaluate<bool> IReproducable<(IEvaluate<T> Left, IEvaluate<T> Right), IEvaluate<bool>>.NewUsing((IEvaluate<T> Left, IEvaluate<T> Right) param)
		=> NewUsing(param);
}

/// <summary>
/// True when <see cref="Left"/> is strictly less than <see cref="Right"/>.
/// </summary>
/// <remarks>
/// See <see cref="GreaterThan{T}"/> for why the comparison is drawn from
/// <see cref="IComparisonOperators{TSelf, TOther, TResult}"/>. Reduction canonicalizes every
/// <see cref="LessThan{T}"/> to a <see cref="GreaterThan{T}"/> with the operands swapped (see
/// Reductions.cs), so this type never survives a reduction pass; it exists purely as the
/// convenient, natural-reading spelling for construction.
/// </remarks>
public sealed partial class LessThan<T>
	: OperatorBase<IEvaluate<T>, bool>,
		IReproducable<(IEvaluate<T> Left, IEvaluate<T> Right), IEvaluate<bool>>
	where T : notnull, IEquatable<T>, IComparable<T>, IComparisonOperators<T, T, bool>
{
	private LessThan(
		ICatalog<IEvaluate<bool>> catalog,
		IEvaluate<T> left,
		IEvaluate<T> right)
		: base(catalog, Symbols.LessThan, [left, right])
	{
		left.ThrowIfNull().OnlyInDebug();
		right.ThrowIfNull().OnlyInDebug();
		Left = left;
		Right = right;
	}

	public IEvaluate<T> Left { get; }

	public IEvaluate<T> Right { get; }

	protected override EvaluationResult<bool> EvaluateInternal(Context context)
	{
		EvaluationResult<T> left = Left.Evaluate(context);
		EvaluationResult<T> right = Right.Evaluate(context);
		return new(left.Result < right.Result, Describe([left.Description, right.Description]));
	}

	internal static LessThan<T> Create(
		ICatalog<IEvaluate<bool>> catalog,
		(IEvaluate<T> Left, IEvaluate<T> Right) param)
	{
		catalog.ThrowIfNull().OnlyInDebug();
		param.Left.ThrowIfNull().OnlyInDebug();
		param.Right.ThrowIfNull().OnlyInDebug();
		Contract.EndContractBlock();

		return catalog.Register(new LessThan<T>(catalog, param.Left, param.Right));
	}

	public LessThan<T> NewUsing(
		ICatalog<IEvaluate<bool>> catalog,
		(IEvaluate<T> Left, IEvaluate<T> Right) param)
		=> Create(catalog, param);

	public LessThan<T> NewUsing((IEvaluate<T> Left, IEvaluate<T> Right) param)
		=> NewUsing(Catalog, param);

	IEvaluate<bool> IReproducable<(IEvaluate<T> Left, IEvaluate<T> Right), IEvaluate<bool>>.NewUsing(ICatalog<IEvaluate<bool>> catalog, (IEvaluate<T> Left, IEvaluate<T> Right) param)
		=> NewUsing(catalog, param);

	IEvaluate<bool> IReproducable<(IEvaluate<T> Left, IEvaluate<T> Right), IEvaluate<bool>>.NewUsing((IEvaluate<T> Left, IEvaluate<T> Right) param)
		=> NewUsing(param);
}

public static class ComparisonExtensions
{
	public static IEvaluate<bool> GreaterThan<T>(
		this ICatalog<IEvaluate<bool>> catalog,
		(IEvaluate<T> Left, IEvaluate<T> Right) operands)
		where T : notnull, IEquatable<T>, IComparable<T>, IComparisonOperators<T, T, bool>
		=> Boolean.GreaterThan<T>.Create(catalog, operands);

	public static IEvaluate<bool> LessThan<T>(
		this ICatalog<IEvaluate<bool>> catalog,
		(IEvaluate<T> Left, IEvaluate<T> Right) operands)
		where T : notnull, IEquatable<T>, IComparable<T>, IComparisonOperators<T, T, bool>
		=> Boolean.LessThan<T>.Create(catalog, operands);
}
