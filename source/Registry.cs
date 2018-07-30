using Open.Evaluation.Arithmetic;
using Open.Evaluation.Boolean;
using Open.Evaluation.Catalogs;
using Open.Evaluation.Core;
using Open.Numeric;
using System;
using System.Collections.Generic;
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
				= (new List<char> { ADD, MULTIPLY }).AsReadOnly();
			public static readonly IReadOnlyList<char> Functions
				= (new List<char> { SQUARE, INVERT, SQUARE_ROOT }).AsReadOnly();

			public static IEvaluate<double> GetOperator(
				ICatalog<IEvaluate<double>> catalog,
				char op,
				IEnumerable<IEvaluate<double>> children)
			{
				if (catalog == null) throw new ArgumentNullException(nameof(catalog));
				if (children == null) throw new ArgumentNullException(nameof(children));
				Contract.EndContractBlock();
				Debug.Assert(op != '\0'); // May have created a 'default' value for an operator upstream.
				switch (op)
				{
					case Sum.SYMBOL:
						return catalog.SumOf(children);
					case Product.SYMBOL:
						return catalog.ProductOf(children);
				}

				throw new ArgumentException($"Invalid operator: {op}", nameof(op));
			}

			public static IEvaluate<double> GetOperator(
				EvaluationCatalogSubmodule catalog,
				char op,
				IEnumerable<IEvaluate<double>> children)
				=> GetOperator(catalog?.Catalog, op, children);

			public static IEvaluate<double> GetRandomOperator(
				ICatalog<IEvaluate<double>> catalog,
				IEnumerable<IEvaluate<double>> children,
				params char[] except)
			{
				if (catalog == null) throw new ArgumentNullException(nameof(catalog));
				if (children == null) throw new ArgumentNullException(nameof(children));
				Contract.EndContractBlock();

				if (except == null || except.Length == 0)
					return GetOperator(catalog, Operators.RandomSelectOne(), children);

				return Operators.TryRandomSelectOne(out var op, new HashSet<char>(except))
					? GetOperator(catalog, op, children)
					: null;
			}

			public static IEvaluate<double> GetRandomOperator(
				EvaluationCatalogSubmodule catalog,
				IEnumerable<IEvaluate<double>> children,
				params char[] except)
				=> GetRandomOperator(catalog?.Catalog, children, except);

			public static IEvaluate<double> GetFunction(
				ICatalog<IEvaluate<double>> catalog,
				char op,
				in ReadOnlySpan<IEvaluate<double>> children)
			{
				if (catalog == null) throw new ArgumentNullException(nameof(catalog));
				Contract.EndContractBlock();

				if (children.Length == 1)
					return GetFunction(catalog, op, children[0]);

				switch (op)
				{
					case Exponent.SYMBOL:
						if (children.Length != 2) throw new ArgumentException("Must have 2 child params for an exponent.");
						return catalog.GetExponent(children[0], children[1]);
				}

				throw new ArgumentException("Invalid function.", nameof(op));
			}

			public static IEvaluate<double> GetFunction(
				ICatalog<IEvaluate<double>> catalog,
				char op,
				IEvaluate<double> child)
			{
				if (catalog == null) throw new ArgumentNullException(nameof(catalog));
				if (child == null) throw new ArgumentNullException(nameof(child));
				Contract.EndContractBlock();

				switch (op)
				{
					case SQUARE:
						return catalog.GetExponent(child, 2);
					case INVERT:
						return catalog.GetExponent(child, -1);
					case SQUARE_ROOT:
						return catalog.GetExponent(child, 0.5);
					case Exponent.SYMBOL:
						throw new ArgumentException("Must have 2 child params for an exponent.");
					default:
						throw new ArgumentException("Invalid function.", nameof(op));
				}
			}

			public static IEvaluate<double> GetFunction(
				EvaluationCatalogSubmodule catalog,
				char op,
				in ReadOnlySpan<IEvaluate<double>> children)
				=> GetFunction(catalog?.Catalog, op, children);

			public static IEvaluate<double> GetRandomFunction(
				ICatalog<IEvaluate<double>> catalog,
				in ReadOnlySpan<IEvaluate<double>> children,
				params char[] except)
			{
				if (catalog == null) throw new ArgumentNullException(nameof(catalog));
				Contract.EndContractBlock();

				if (except == null || except.Length == 0)
					return GetFunction(catalog, Functions.RandomSelectOne(), children);

				return Functions.TryRandomSelectOne(out var op, new HashSet<char>(except))
					? GetFunction(catalog, op, children)
					: null;
			}

			public static IEvaluate<double> GetRandomFunction(
				EvaluationCatalogSubmodule catalog,
				in ReadOnlySpan<IEvaluate<double>> children,
				params char[] except)
				=> GetRandomFunction(catalog?.Catalog, children, except);

			public static IEvaluate<double> GetRandomFunction(
				ICatalog<IEvaluate<double>> catalog,
				IEvaluate<double> child,
				params char[] except)
			{
				if (catalog == null) throw new ArgumentNullException(nameof(catalog));
				if (child == null) throw new ArgumentNullException(nameof(child));
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
				=> GetRandomFunction(catalog?.Catalog, child, except);
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
				= (new List<char> { AND, OR });
			public static readonly IReadOnlyList<char> Functions
				= (new List<char> { NOT, CONDITIONAL });
			public static readonly IReadOnlyList<string> Counting
				= (new List<string> { AT_LEAST, AT_MOST, EXACTLY });

		}




	}
}
