using Open.Disposable;
using Open.Evaluation.Core;
using Open.RandomizationExtensions;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using Throw;

using EvaluationCatalogSubmodule = Open.Evaluation.Catalogs.EvaluationCatalog<double>.SubmoduleBase;

namespace Open.Evaluation.Arithmetic;
public static class Registry
{
		public static IEvaluate<double> GetOperator(
			ICatalog<IEvaluate<double>> catalog,
			char op,
			IEnumerable<IEvaluate<double>> children)
		{
			catalog.ThrowIfNull();
			children.ThrowIfNull();
			Debug.Assert(op != '\0'); // May have created a 'default' value for an operator upstream.
			Contract.EndContractBlock();

			return op switch
			{
				Glyphs.Sum => catalog.SumOf(children),
				Glyphs.Product => catalog.ProductOf(children),
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

			return GetOperator(catalog, Glyphs.Operators.RandomSelectOne(), children);
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
			return Glyphs.Operators.TryRandomSelectOne(out var op, hs)
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
				case Glyphs.Exponent:
					if (children.Count != 2) throw new ArgumentException("Must have 2 child params for an exponent.",nameof(children));
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
				Glyphs.Square => catalog.GetExponent(child, 2),
				Glyphs.Invert => catalog.GetExponent(child, -1),
				Glyphs.SquareRoot => catalog.GetExponent(child, 0.5),
				Glyphs.Exponent => throw new ArgumentException("Must have 2 child params for an exponent."),
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

			return GetFunction(catalog, Glyphs.Functions.RandomSelectOne(), children);
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
			return Glyphs.Functions.TryRandomSelectOne(out var op, hs)
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
				op = Glyphs.Functions.RandomSelectOne();
			else
			Glyphs.Functions.TryRandomSelectOne(out op, new HashSet<char>(except));

			return GetFunction(catalog, op, child);
		}

		public static IEvaluate<double> GetRandomFunction(
			EvaluationCatalogSubmodule catalog,
			IEvaluate<double> child,
			params char[] except)
			=> catalog is null ? throw new ArgumentNullException(nameof(catalog))
			: GetRandomFunction(catalog.Catalog, child, except);
	}
}
