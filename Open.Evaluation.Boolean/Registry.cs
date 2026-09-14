using Open.Evaluation.Boolean.Counting;
using Open.Evaluation.Core;
using Open.RandomizationExtensions;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.Numerics;
using Throw;

namespace Open.Evaluation.Boolean;
public static class Registry
{
	public static readonly ImmutableArray<char> Operators = [Glyphs.And, Glyphs.Or];
	public static readonly ImmutableArray<char> Functions = [Glyphs.Not, Glyphs.Conditional];
	public static readonly ImmutableArray<string> CountingFunctions = [nameof(AtLeast), nameof(AtMost), nameof(Exactly)];

	// Kept separate from Operators/Functions: those two lists drive the homogeneous
	// IEvaluate<bool>-children factories (GetOperator, GetRandomOperator, and the Solve-side
	// generators built on them), and a comparison's children are IEvaluate<T> for some numeric T,
	// not IEvaluate<bool>. Folding the glyphs in there would let an untyped factory draw a
	// comparison with the wrong child type; GetComparison is the only door in.
	public static readonly ImmutableArray<char> Comparisons = [Glyphs.GreaterThan, Glyphs.LessThan];

	public static IEvaluate<bool> GetComparison<T>(
		ICatalog<IEvaluate<bool>> catalog,
		char op,
		IEvaluate<T> left,
		IEvaluate<T> right)
		where T : notnull, IEquatable<T>, IComparable<T>, IComparisonOperators<T, T, bool>
	{
		catalog.ThrowIfNull();
		left.ThrowIfNull();
		right.ThrowIfNull();
		op.Throw("May have created a 'default' value for an operator upstream.").IfEquals('\0').OnlyInDebug();
		Contract.EndContractBlock();

		return op switch
		{
			Glyphs.GreaterThan => catalog.GreaterThan((left, right)),
			Glyphs.LessThan => catalog.LessThan((left, right)),

			_ => throw new ArgumentException($"Invalid operator: {op}", nameof(op)),
		};
	}

	public static IEvaluate<bool> GetOperator(
		ICatalog<IEvaluate<bool>> catalog,
		char op,
		IEnumerable<IEvaluate<bool>> children)
	{
		catalog.ThrowIfNull();
		children.ThrowIfNull();
		op.Throw("May have created a 'default' value for an operator upstream.").IfEquals('\0').OnlyInDebug();
		Contract.EndContractBlock();

		return op switch
		{
			Glyphs.And => catalog.And(children),
			Glyphs.Or => catalog.Or(children),

			_ => throw new ArgumentException($"Invalid operator: {op}", nameof(op)),
		};
	}

	public static IEvaluate<bool>? GetRandomOperator(
		ICatalog<IEvaluate<bool>> catalog,
		IEnumerable<IEvaluate<bool>> children)
	{
		catalog.ThrowIfNull();
		children.ThrowIfNull();
		Contract.EndContractBlock();

		return GetOperator(catalog, Operators.RandomSelectOne(), children);
	}

	public static IEvaluate<bool>? GetRandomOperator(
		ICatalog<IEvaluate<bool>> catalog,
		IEnumerable<IEvaluate<bool>> children,
		char except, params char[] others)
	{
		catalog.ThrowIfNull();
		children.ThrowIfNull();
		Contract.EndContractBlock();

		return Operators.TryRandomSelectOneExcept(out char op, except, others)
			? GetOperator(catalog, op, children)
			: null;
	}

	public static IEvaluate<bool>? GetRandomOperator(
		ICatalog<IEvaluate<bool>> catalog,
		IEnumerable<IEvaluate<bool>> children,
		IEnumerable<char> except)
	{
		catalog.ThrowIfNull();
		children.ThrowIfNull();
		Contract.EndContractBlock();

		return except is null
			? GetOperator(catalog, Operators.RandomSelectOne(), children)
			: Operators.TryRandomSelectOne(out char op, new HashSet<char>(except))
			? GetOperator(catalog, op, children)
			: null;
	}
}
