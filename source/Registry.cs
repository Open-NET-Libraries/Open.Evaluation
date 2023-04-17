using Open.Disposable;
using Open.Evaluation.Arithmetic;
using Open.Evaluation.Boolean;
using Open.Evaluation.Boolean.Counting;
using Open.Evaluation.Core;
using Open.RandomizationExtensions;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using Throw;

using EvaluationCatalogSubmodule = Open.Evaluation.Catalogs.EvaluationCatalog<double>.SubmoduleBase;

namespace Open.Evaluation;
public static class Registry
{
	public static class Boolean
	{
		// Operators...
		public const char AND = Evaluation.Boolean.Symbols.Characters.And;
		public const char OR = Evaluation.Boolean.Symbols.Characters.Or;

		// Functions...
		public const char NOT = Evaluation.Boolean.Symbols.Characters.Not;
		public const char CONDITIONAL = Evaluation.Boolean.Symbols.Characters.Conditional;

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
			Debug.Assert(op != '\0'); // May have created a 'default' value for an operator upstream.
			Contract.EndContractBlock();

			return op switch
			{
				AND => catalog.And(children),
				OR => catalog.Or(children),

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
