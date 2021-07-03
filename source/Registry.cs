using Open.Evaluation.Arithmetic;
using Open.Evaluation.Boolean;
using Open.Evaluation.Boolean.Counting;
using Open.Evaluation.Catalogs;
using Open.Evaluation.Core;
using Open.RandomizationExtensions;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.Contracts;

namespace Open.Evaluation
{
	using EvaluationCatalogSubmodule = EvaluationCatalog<double>.SubmoduleBase;

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

			public static readonly IReadOnlyList<char> Operators
				= new char[] { ADD, MULTIPLY }.ToImmutableArray();
			public static readonly IReadOnlyList<char> Functions
				= new char[] { SQUARE, INVERT, SQUARE_ROOT }.ToImmutableArray();

			public static IEvaluate<double> GetOperator(
				ICatalog<IEvaluate<double>> catalog,
				char op,
				IEnumerable<IEvaluate<double>> children)
			{
				if (catalog is null) throw new ArgumentNullException(nameof(catalog));
				if (children is null) throw new ArgumentNullException(nameof(children));
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
			{
				if (catalog is null) throw new ArgumentNullException(nameof(catalog));

				return GetOperator(catalog.Catalog, op, children);
			}

			public static IEvaluate<double>? GetRandomOperator(
				ICatalog<IEvaluate<double>> catalog,
				IEnumerable<IEvaluate<double>> children,
				params char[] except)
			{
				if (catalog is null) throw new ArgumentNullException(nameof(catalog));
				if (children is null) throw new ArgumentNullException(nameof(children));
				Contract.EndContractBlock();

				if (except == null || except.Length == 0)
					return GetOperator(catalog, Operators.RandomSelectOne(), children);

				return Operators.TryRandomSelectOne(out var op, new HashSet<char>(except))
					? GetOperator(catalog, op, children)
					: null;
			}

			public static IEvaluate<double>? GetRandomOperator(
				EvaluationCatalogSubmodule catalog,
				IEnumerable<IEvaluate<double>> children,
				params char[] except)
			{
				if (catalog is null) throw new ArgumentNullException(nameof(catalog));

				return GetRandomOperator(catalog.Catalog, children, except);
			}

			public static IEvaluate<double> GetFunction(
				ICatalog<IEvaluate<double>> catalog,
				char op,
				IReadOnlyList<IEvaluate<double>> children)
			{
				if (catalog is null) throw new ArgumentNullException(nameof(catalog));
				if (children is null) throw new ArgumentNullException(nameof(children));

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
				if (catalog is null) throw new ArgumentNullException(nameof(catalog));
				if (child is null) throw new ArgumentNullException(nameof(child));
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
			{
				if (catalog is null) throw new ArgumentNullException(nameof(catalog));

				return GetFunction(catalog.Catalog, op, children);
			}

			public static IEvaluate<double>? GetRandomFunction(
				ICatalog<IEvaluate<double>> catalog,
				IReadOnlyList<IEvaluate<double>> children,
				params char[] except)
			{
				if (catalog is null) throw new ArgumentNullException(nameof(catalog));
				Contract.EndContractBlock();

				if (except == null || except.Length == 0)
					return GetFunction(catalog, Functions.RandomSelectOne(), children);

				return Functions.TryRandomSelectOne(out var op, new HashSet<char>(except))
					? GetFunction(catalog, op, children)
					: null;
			}

			public static IEvaluate<double>? GetRandomFunction(
				EvaluationCatalogSubmodule catalog,
				IReadOnlyList<IEvaluate<double>> children,
				params char[] except)
			{
				if (catalog is null) throw new ArgumentNullException(nameof(catalog));

				return GetRandomFunction(catalog.Catalog, children, except);
			}

			public static IEvaluate<double> GetRandomFunction(
				ICatalog<IEvaluate<double>> catalog,
				IEvaluate<double> child,
				params char[] except)
			{
				if (catalog is null) throw new ArgumentNullException(nameof(catalog));
				if (child is null) throw new ArgumentNullException(nameof(child));
				Contract.EndContractBlock();

				char op;
				if (except == null || except.Length == 0)
					op = Functions.RandomSelectOne();
				else
					Functions.TryRandomSelectOne(out op, new HashSet<char>(except));

				return GetFunction(catalog, op, child);

			}

			public static IEvaluate<double> GetRandomFunction(
				EvaluationCatalogSubmodule catalog,
				IEvaluate<double> child,
				params char[] except)
			{
				if (catalog is null) throw new ArgumentNullException(nameof(catalog));

				return GetRandomFunction(catalog.Catalog, child, except);
			}
		}

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

			public static readonly IReadOnlyList<char> Operators
				= new char[] { AND, OR }.ToImmutableArray();
			public static readonly IReadOnlyList<char> Functions
				= new char[] { NOT, CONDITIONAL }.ToImmutableArray();
			public static readonly IReadOnlyList<string> Counting
				= new string[] { AT_LEAST, AT_MOST, EXACTLY }.ToImmutableArray();

			public static IEvaluate<bool> GetOperator(
				ICatalog<IEvaluate<bool>> catalog,
				char op,
				IEnumerable<IEvaluate<bool>> children)
			{
				if (catalog is null) throw new ArgumentNullException(nameof(catalog));
				if (children is null) throw new ArgumentNullException(nameof(children));
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
				if (catalog is null) throw new ArgumentNullException(nameof(catalog));
				if (children is null) throw new ArgumentNullException(nameof(children));
				Contract.EndContractBlock();

				if (except == null || except.Length == 0)
					return GetOperator(catalog, Operators.RandomSelectOne(), children);

				return Operators.TryRandomSelectOne(out var op, new HashSet<char>(except))
					? GetOperator(catalog, op, children)
					: null;
			}

		}
	}
}
