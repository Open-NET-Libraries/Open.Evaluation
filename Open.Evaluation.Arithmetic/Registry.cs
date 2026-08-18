using Open.RandomizationExtensions;

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

		using RecycleHelper<HashSet<char>> lease = HashSetPool<char>.Rent();
        HashSet<char> hs = lease.Item;
#if NETSTANDARD2_1_OR_GREATER
		hs.EnsureCapacity(moreExcept.Length + 1);
#endif
		hs.Add(except);
		foreach (char e in moreExcept) hs.Add(e);
		return Operators.TryRandomSelectOne(out char op, hs)
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
		};
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

		char op;
		if (Value<T>.IsFloatingPoint)
		{
			op = Functions.RandomSelectOne();
		}
		else
		{
			// Non-float T can't represent the SquareRoot power (T.One/(T.One+T.One) would
			// truncate to zero), so it must never be drawn as a candidate for T.
			bool selected = Functions.TryRandomSelectOneExcept(out op, Glyphs.SquareRoot);
			Debug.Assert(selected, "Excluding one glyph from a 3-element set must always succeed.");
		}

		// GetFunction can never produce a SquareRoot (it always throws NotSupportedException,
		// regardless of T -- only GetFloatFunction can), so compute it directly here whenever
		// it's drawn. Only reachable when T is floating-point capable (see above), so the
		// division below is provably safe.
		return op == Glyphs.SquareRoot && children.Count == 1
			? catalog.GetExponent(children[0], Value<T>.Half)
			: GetFunction(catalog, op, children);
	}

	[SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Preferred verbosity")]
	public static IEvaluate<T>? GetRandomFunction<T>(
		ICatalog<IEvaluate<T>> catalog,
		IReadOnlyList<IEvaluate<T>> children,
		char except,
		params char[] moreExcept)
		where T : notnull, INumber<T>
	{
		catalog.ThrowIfNull();
		Contract.EndContractBlock();

		using RecycleHelper<HashSet<char>> lease = HashSetPool<char>.Rent();
        HashSet<char> hs = lease.Item;
#if NETSTANDARD2_1_OR_GREATER
		hs.EnsureCapacity(moreExcept.Length+1);
#endif
		hs.Add(except);
		foreach (char e in moreExcept) hs.Add(e);
		if (!Value<T>.IsFloatingPoint) hs.Add(Glyphs.SquareRoot);

		if (!Functions.TryRandomSelectOne(out char op, hs))
			return null;

		return op == Glyphs.SquareRoot && children.Count == 1
			? catalog.GetExponent(children[0], Value<T>.Half)
			: GetFunction(catalog, op, children);
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

		// Non-float T can't represent the SquareRoot power (T.One/(T.One+T.One) would truncate
		// to zero), so it must never be drawn as a candidate for T.
		bool excludeSquareRoot = !Value<T>.IsFloatingPoint;

		char op;
		if (!excludeSquareRoot && (except is null || except.Length == 0))
		{
			op = Functions.RandomSelectOne();
		}
		else
		{
			HashSet<char> hs = except is null ? [] : new HashSet<char>(except);
			if (excludeSquareRoot) hs.Add(Glyphs.SquareRoot);
			if (!Functions.TryRandomSelectOne(out op, hs))
				throw new InvalidOperationException("The exclusion set eliminates every available function for this T.");
		}

		// GetFunction can never produce a SquareRoot (it always throws NotSupportedException,
		// regardless of T -- only GetFloatFunction can), so compute it directly here whenever
		// it's drawn. Only reachable when T is floating-point capable (see above), so the
		// division below is provably safe.
		return op == Glyphs.SquareRoot
			? catalog.GetExponent(child, Value<T>.Half)
			: GetFunction(catalog, op, child);
	}

	public static IEvaluate<T> GetRandomFunction<T>(
		EvaluationCatalog<T>.SubmoduleBase catalog,
		IEvaluate<T> child,
		params char[] except)
		where T : notnull, INumber<T>
		=> catalog is null ? throw new ArgumentNullException(nameof(catalog))
		: GetRandomFunction(catalog.Catalog, child, except);
}
