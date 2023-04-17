using Open.Disposable;
using Open.Evaluation.Arithmetic;
using Open.Evaluation.Boolean;
using Open.Evaluation.Boolean.Counting;
using Open.Evaluation.Core;
using Open.RandomizationExtensions;
using System.Collections.Immutable;
using EvaluationCatalogSubmodule = Open.Evaluation.Catalogs.EvaluationCatalog<double>.SubmoduleBase;

namespace Open.Evaluation;
public static class Registry
{
	public static class Arithmetic
	{
		// Operators...
		public const char ADD = Sum.SYMBOL;
		public const char MULTIPLY = Product.SYMBOL;

		// Functions. Division is simply an 'inversion'.
		public const char SQUARE = '²';
		public const char INVERT = '/';
		public const char SQUARE_ROOT = '√';

		public static readonly ImmutableArray<char> Operators
			= ImmutableArray.Create(ADD, MULTIPLY);
		public static readonly ImmutableArray<char> Functions
			= ImmutableArray.Create(SQUARE, INVERT, SQUARE_ROOT);

		public static IEvaluate<double> GetOperator(
			ICatalog<IEvaluate<double>> catalog,
			char op,
			IEnumerable<IEvaluate<double>> children)
		{
			catalog.ThrowIfNull();
			children.ThrowIfNull();
			Contract.EndContractBlock();
			Debug.Assert(op != '\0'); // May have created a 'default' value for an operator upstream.
			return op switch
			{
				ADD => catalog.SumOf(children),
				MULTIPLY => catalog.ProductOf(children),

				_ => throw new ArgumentException($"Invalid operator: {op}", nameof(op)),
			};
		}

		public static IEvaluate<double> GetOperator(
			EvaluationCatalogSubmodule catalog,
			char op,
			IEnumerable<IEvaluate<double>> children)
			=> catalog is null
				? throw new ArgumentNullException(nameof(catalog))
				: GetOperator(catalog.Catalog, op, children);

		public static IEvaluate<double>? GetRandomOperator(
			ICatalog<IEvaluate<double>> catalog,
			IEnumerable<IEvaluate<double>> children)
		{
			catalog.ThrowIfNull();
			children.ThrowIfNull();
			Contract.EndContractBlock();

			return GetOperator(catalog, Operators.RandomSelectOne(), children);
		}

		public static IEvaluate<double>? GetRandomOperator(
			ICatalog<IEvaluate<double>> catalog,
			IEnumerable<IEvaluate<double>> children,
			char except,
			params char[] moreExcept)
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

		public static IEvaluate<double>? GetRandomOperator(
			EvaluationCatalogSubmodule catalog,
			IEnumerable<IEvaluate<double>> children)
			=> catalog is null
				? throw new ArgumentNullException(nameof(catalog))
				: GetRandomOperator(catalog.Catalog, children);

		public static IEvaluate<double>? GetRandomOperator(
			EvaluationCatalogSubmodule catalog,
			IEnumerable<IEvaluate<double>> children,
			char except,
			params char[] moreExcept)
			=> catalog is null
				? throw new ArgumentNullException(nameof(catalog))
				: GetRandomOperator(catalog.Catalog, children, except, moreExcept);

		public static IEvaluate<double> GetFunction(
			ICatalog<IEvaluate<double>> catalog,
			char op,
			IReadOnlyList<IEvaluate<double>> children)
		{
			catalog.ThrowIfNull();
			children.ThrowIfNull();

			Contract.EndContractBlock();

			if (children.Count == 1)
				return GetFunction(catalog, op, children[0]);

			switch (op)
			{
				case Exponent.SYMBOL:
					if (children.Count != 2) throw new ArgumentException("Must have 2 child params for an exponent.");
					return catalog.GetExponent(children[0], children[1]);
			}

			throw new ArgumentException("Invalid function.", nameof(op));
		}

		public static IEvaluate<double> GetFunction(
			ICatalog<IEvaluate<double>> catalog,
			char op,
			IEvaluate<double> child)
		{
			catalog.ThrowIfNull();
			child.ThrowIfNull();
			Contract.EndContractBlock();

			return op switch
			{
				SQUARE => catalog.GetExponent(child, 2),
				INVERT => catalog.GetExponent(child, -1),
				SQUARE_ROOT => catalog.GetExponent(child, 0.5),
				Exponent.SYMBOL => throw new ArgumentException("Must have 2 child params for an exponent."),
				_ => throw new ArgumentException("Invalid function.", nameof(op)),
			};
		}

		public static IEvaluate<double> GetFunction(
			EvaluationCatalogSubmodule catalog,
			char op,
			IReadOnlyList<IEvaluate<double>> children)
			=> catalog is null ? throw new ArgumentNullException(nameof(catalog))
			: GetFunction(catalog.Catalog, op, children);

		public static IEvaluate<double>? GetRandomFunction(
			ICatalog<IEvaluate<double>> catalog,
			IReadOnlyList<IEvaluate<double>> children)
		{
			catalog.ThrowIfNull();
			Contract.EndContractBlock();

			return GetFunction(catalog, Functions.RandomSelectOne(), children);
		}

		public static IEvaluate<double>? GetRandomFunction(
			ICatalog<IEvaluate<double>> catalog,
			IReadOnlyList<IEvaluate<double>> children,
			char except,
			params char[] moreExcept)
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

		public static IEvaluate<double>? GetRandomFunction(
			EvaluationCatalogSubmodule catalog,
			IReadOnlyList<IEvaluate<double>> children)
			=> catalog is null ? throw new ArgumentNullException(nameof(catalog))
			: GetRandomFunction(catalog.Catalog, children);

		public static IEvaluate<double>? GetRandomFunction(
			EvaluationCatalogSubmodule catalog,
			IReadOnlyList<IEvaluate<double>> children,
			char except,
			params char[] moreExcept)
			=> catalog is null ? throw new ArgumentNullException(nameof(catalog))
			: GetRandomFunction(catalog.Catalog, children, except, moreExcept);

		public static IEvaluate<double> GetRandomFunction(
			ICatalog<IEvaluate<double>> catalog,
			IEvaluate<double> child,
			params char[] except)
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

		public static IEvaluate<double> GetRandomFunction(
			EvaluationCatalogSubmodule catalog,
			IEvaluate<double> child,
			params char[] except)
			=> catalog is null ? throw new ArgumentNullException(nameof(catalog))
			: GetRandomFunction(catalog.Catalog, child, except);
	}

	[System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1716:Identifiers should not match keywords")]
	public static class Boolean
	{
		// Operators...
		public const char AND = And.SYMBOL;
		public const char OR = Or.SYMBOL;

		// Functions...
		public const char NOT = Not.SYMBOL;
		public const char CONDITIONAL = Conditional.SYMBOL;

		// Fuzzy...
		public const string AT_LEAST = AtLeast.PREFIX;
		public const string AT_MOST = AtMost.PREFIX;
		public const string EXACTLY = Exactly.PREFIX;

		public static readonly ImmutableArray<char> Operators
			= ImmutableArray.Create(AND, OR);
		public static readonly ImmutableArray<char> Functions
			= ImmutableArray.Create(NOT, CONDITIONAL);
		public static readonly ImmutableArray<string> Counting
			= ImmutableArray.Create(AT_LEAST, AT_MOST, EXACTLY);

		public static IEvaluate<bool> GetOperator(
			ICatalog<IEvaluate<bool>> catalog,
			char op,
			IEnumerable<IEvaluate<bool>> children)
		{
			catalog.ThrowIfNull();
			children.ThrowIfNull();
			Contract.EndContractBlock();
			Debug.Assert(op != '\0'); // May have created a 'default' value for an operator upstream.
			return op switch
			{
				AND => catalog.SumOf(children),
				OR => catalog.ProductOf(children),

				_ => throw new ArgumentException($"Invalid operator: {op}", nameof(op)),
			};
		}

		public static IEvaluate<bool>? GetRandomOperator(
			ICatalog<IEvaluate<bool>> catalog,
			IEnumerable<IEvaluate<bool>> children,
			params char[] except)
		{
			catalog.ThrowIfNull();
			children.ThrowIfNull();
			Contract.EndContractBlock();

			return except is null || except.Length == 0
				? GetOperator(catalog, Operators.RandomSelectOne(), children)
				: Operators.TryRandomSelectOne(out var op, new HashSet<char>(except))
				? GetOperator(catalog, op, children)
				: null;
		}
	}
}
