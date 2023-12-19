using Open.Disposable;
using Open.Evaluation.Core;
using Open.RandomizationExtensions;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using Throw;
using System.Numerics;

namespace Open.Evaluation.Arithmetic;
public static class Registry
{
	public static readonly ImmutableArray<char> Operators = [Glyphs.Sum, Glyphs.Product];

	public static readonly ImmutableArray<char> Functions = [Glyphs.Square, Glyphs.Invert, Glyphs.SquareRoot];

	public static IEvaluate<T> GetOperator<T>(
		ICatalog<IEvaluate<T>> catalog,
		char op,
		IEnumerable<IEvaluate<T>> children)
		where T : notnull, INumber<T>
	{
		catalog.ThrowIfNull().OnlyInDebug();
		children.ThrowIfNull().OnlyInDebug();
		Debug.Assert(op != '\0'); // May have created a 'default' value for an operator upstream.
		Contract.EndContractBlock();

		return op switch
		{
			Glyphs.Sum => catalog.SumOf(children),
			Glyphs.Product => catalog.ProductOf(children),
			_ => throw new ArgumentException($"Invalid operator: {op}", nameof(op)),
		};
	}

	public static IEvaluate<T> GetOperator<T>(
		EvaluationCatalog<T>.SubmoduleBase catalog,
		char op, IEnumerable<IEvaluate<T>> children)
		where T : notnull, INumber<T>
	{
		catalog.ThrowIfNull().OnlyInDebug();
		return GetOperator(catalog.Catalog, op, children);
	}

	public static IEvaluate<T>? GetRandomOperator<T>(
		ICatalog<IEvaluate<T>> catalog,
		IEnumerable<IEvaluate<T>> children)
		where T : notnull, INumber<T>
	{
		catalog.ThrowIfNull().OnlyInDebug();
		children.ThrowIfNull();
		Contract.EndContractBlock();

		return GetOperator(catalog, Operators.RandomSelectOne(), children);
	}

	public static IEvaluate<T>? GetRandomOperator<T>(
		ICatalog<IEvaluate<T>> catalog,
		IEnumerable<IEvaluate<T>> children,
		char except,
		params char[] moreExcept)
		where T : notnull, INumber<T>
	{
		catalog.ThrowIfNull();
		children.ThrowIfNull();
		Contract.EndContractBlock();

		using var lease = HashSetPool<char>.Rent();
		var hs = lease.Item;
#if NETSTANDARD2_1_OR_GREATER
		hs.EnsureCapacity(moreExcept.Length + 1);
#endif
		hs.Add(except);
		foreach (var e in moreExcept) hs.Add(e);
		return Operators.TryRandomSelectOne(out var op, hs)
			? GetOperator(catalog, op, children)
			: null;
	}

	public static IEvaluate<T>? GetRandomOperator<T>(
		EvaluationCatalog<T>.SubmoduleBase catalog,
		IEnumerable<IEvaluate<T>> children)
		where T : notnull, INumber<T>
	{
		catalog.ThrowIfNull().OnlyInDebug();
		return GetRandomOperator(catalog.Catalog, children);
	}

	public static IEvaluate<T>? GetRandomOperator<T>(
		EvaluationCatalog<T>.SubmoduleBase catalog,
		IEnumerable<IEvaluate<T>> children,
		char except,
		params char[] moreExcept)
		where T : notnull, INumber<T>
	{
		catalog.ThrowIfNull().OnlyInDebug();
		return GetRandomOperator(catalog.Catalog, children, except, moreExcept);
	}

	public static IEvaluate<T> GetFunction<T>(
		ICatalog<IEvaluate<T>> catalog,
		char op,
		IReadOnlyList<IEvaluate<T>> children)
		where T : notnull, INumber<T>
	{
		catalog.ThrowIfNull().OnlyInDebug();
		children.ThrowIfNull().OnlyInDebug();
		Contract.EndContractBlock();

		if (children.Count == 1)
			return GetFunction(catalog, op, children[0]);

		switch (op)
		{
			case Glyphs.Exponent:
				if (children.Count != 2) throw new ArgumentException("Must have 2 child params for an exponent.", nameof(children));
				return catalog.GetExponent(children[0], children[1]);
		}

		throw new ArgumentException("Invalid function.", nameof(op));
	}

	public static IEvaluate<T> GetFunction<T>(
		ICatalog<IEvaluate<T>> catalog,
		char op,
		IEvaluate<T> child)
		where T : notnull, INumber<T>
	{
		catalog.ThrowIfNull();
		child.ThrowIfNull();
		Contract.EndContractBlock();

		return op switch
		{
			Glyphs.Square => catalog.GetExponent(child, Value<T>.Two),
			Glyphs.Invert => catalog.GetExponent(child, -T.One),
			Glyphs.SquareRoot => throw new NotSupportedException("Can only get square-roots from confirmed floating/decimal point capable types."),
			Glyphs.Exponent => throw new ArgumentException("Must have 2 child params for an exponent."),
			_ => throw new ArgumentException("Invalid function.", nameof(op)),
		}; ;
	}

	public static IEvaluate<T> GetFloatFunction<T>(
		ICatalog<IEvaluate<T>> catalog,
		char op,
		IEvaluate<T> child)
		where T : notnull, INumber<T>, IFloatingPoint<T>
	{
		catalog.ThrowIfNull();
		child.ThrowIfNull();
		Contract.EndContractBlock();

		return op switch
		{
			Glyphs.Square => catalog.GetExponent(child, ValueFloat<T>.Two),
			Glyphs.Invert => catalog.GetExponent(child, -T.One),
			Glyphs.SquareRoot => catalog.GetExponent(child, ValueFloat<T>.Half),
			Glyphs.Exponent => throw new ArgumentException("Must have 2 child params for an exponent."),
			_ => throw new ArgumentException("Invalid function.", nameof(op)),
		};
	}

	public static IEvaluate<T> GetFunction<T>(
		EvaluationCatalog<T>.SubmoduleBase catalog,
		char op,
		IReadOnlyList<IEvaluate<T>> children)
		where T : notnull, INumber<T>
		=> catalog is null ? throw new ArgumentNullException(nameof(catalog))
		: GetFunction(catalog.Catalog, op, children);

	public static IEvaluate<T>? GetRandomFunction<T>(
		ICatalog<IEvaluate<T>> catalog,
		IReadOnlyList<IEvaluate<T>> children)
		where T : notnull, INumber<T>
	{
		catalog.ThrowIfNull();
		Contract.EndContractBlock();

		return GetFunction(catalog, Functions.RandomSelectOne(), children);
	}

	public static IEvaluate<T>? GetRandomFunction<T>(
		ICatalog<IEvaluate<T>> catalog,
		IReadOnlyList<IEvaluate<T>> children,
		char except,
		params char[] moreExcept)
		where T : notnull, INumber<T>
	{
		catalog.ThrowIfNull();
		Contract.EndContractBlock();

		using var lease = HashSetPool<char>.Rent();
		var hs = lease.Item;
#if NETSTANDARD2_1_OR_GREATER
		hs.EnsureCapacity(moreExcept.Length+1);
#endif
		hs.Add(except);
		foreach (var e in moreExcept) hs.Add(e);
		return Functions.TryRandomSelectOne(out var op, hs)
			? GetFunction(catalog, op, children)
			: null;
	}

	public static IEvaluate<T>? GetRandomFunction<T>(
		EvaluationCatalog<T>.SubmoduleBase catalog,
		IReadOnlyList<IEvaluate<T>> children)
		where T : notnull, INumber<T>
		=> catalog is null ? throw new ArgumentNullException(nameof(catalog))
		: GetRandomFunction(catalog.Catalog, children);

	public static IEvaluate<T>? GetRandomFunction<T>(
		EvaluationCatalog<T>.SubmoduleBase catalog,
		IReadOnlyList<IEvaluate<T>> children,
		char except,
		params char[] moreExcept)
		where T : notnull, INumber<T>
		=> catalog is null ? throw new ArgumentNullException(nameof(catalog))
		: GetRandomFunction(catalog.Catalog, children, except, moreExcept);

	public static IEvaluate<T> GetRandomFunction<T>(
		ICatalog<IEvaluate<T>> catalog,
		IEvaluate<T> child,
		params char[] except)
		where T : notnull, INumber<T>
	{
		catalog.ThrowIfNull();
		child.ThrowIfNull();
		Contract.EndContractBlock();

		char op;
		if (except is null || except.Length == 0)
			op = Functions.RandomSelectOne();
		else
			Functions.TryRandomSelectOne(out op, new HashSet<char>(except));

		return GetFunction(catalog, op, child);
	}

	public static IEvaluate<T> GetRandomFunction<T>(
		EvaluationCatalog<T>.SubmoduleBase catalog,
		IEvaluate<T> child,
		params char[] except)
		where T : notnull, INumber<T>
		=> catalog is null ? throw new ArgumentNullException(nameof(catalog))
		: GetRandomFunction(catalog.Catalog, child, except);
}
