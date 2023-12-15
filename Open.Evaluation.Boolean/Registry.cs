using Open.Evaluation.Boolean.Counting;
using Open.Evaluation.Core;
using Open.RandomizationExtensions;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using Throw;

namespace Open.Evaluation.Boolean;
public static class Registry
{
	public static readonly ImmutableArray<char> Operators = [Glyphs.And, Glyphs.Or];
	public static readonly ImmutableArray<char> Functions = [Glyphs.Not, Glyphs.Conditional];
	public static readonly ImmutableArray<string> CountingFunctions = [nameof(AtLeast), nameof(AtMost), nameof(Exactly)];

	public static IEvaluate<bool> GetOperator(
		[DisallowNull, NotNull] ICatalog<IEvaluate<bool>> catalog,
		char op,
		[DisallowNull, NotNull] IEnumerable<IEvaluate<bool>> children)
	{
		catalog.ThrowIfNull();
		children.ThrowIfNull();
		Debug.Assert(op != '\0'); // May have created a 'default' value for an operator upstream.
		Contract.EndContractBlock();

		return op switch
		{
			Glyphs.And => catalog.And(children),
			Glyphs.Or => catalog.Or(children),

			_ => throw new ArgumentException($"Invalid operator: {op}", nameof(op)),
		};
	}

	public static IEvaluate<bool>? GetRandomOperator(
		[DisallowNull, NotNull] ICatalog<IEvaluate<bool>> catalog,
		[DisallowNull, NotNull] IEnumerable<IEvaluate<bool>> children)
	{
		catalog.ThrowIfNull();
		children.ThrowIfNull();
		Contract.EndContractBlock();

		return GetOperator(catalog, Operators.RandomSelectOne(), children);
	}

	public static IEvaluate<bool>? GetRandomOperator(
		[DisallowNull, NotNull] ICatalog<IEvaluate<bool>> catalog,
		[DisallowNull, NotNull] IEnumerable<IEvaluate<bool>> children,
		char except, params char[] others)
	{
		catalog.ThrowIfNull();
		children.ThrowIfNull();
		Contract.EndContractBlock();

		return Operators.TryRandomSelectOneExcept(out var op, except, others)
			? GetOperator(catalog, op, children)
			: null;
	}

	public static IEvaluate<bool>? GetRandomOperator(
		[DisallowNull, NotNull] ICatalog<IEvaluate<bool>> catalog,
		[DisallowNull, NotNull] IEnumerable<IEvaluate<bool>> children,
		IEnumerable<char> except)
	{
		catalog.ThrowIfNull();
		children.ThrowIfNull();
		Contract.EndContractBlock();

		return except is null
			? GetOperator(catalog, Operators.RandomSelectOne(), children)
			: Operators.TryRandomSelectOne(out var op, new HashSet<char>(except))
			? GetOperator(catalog, op, children)
			: null;
	}
}
