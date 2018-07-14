using Open.Evaluation.Arithmetic;
using Open.Evaluation.Boolean;
using Open.Evaluation.Core;
using System;
using System.Collections.Generic;

namespace Open.Evaluation
{
	public static class Registry
	{
		public static class Arithmetic
		{
			// Operators...
			public const char ADD = Sum.SYMBOL;
			public const char MULTIPLY = Product.SYMBOL;

			// Functions. Division is simply an 'inversion'.
			public const char EXPONENT = Exponent.SYMBOL;

			public static readonly IReadOnlyList<char> Operators
				= (new List<char> { ADD, MULTIPLY });
			public static readonly IReadOnlyList<char> Functions
				= (new List<char> { EXPONENT });
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


		public static IEvaluate<double> GetOperator(
			this ICatalog<IEvaluate<double>> catalog, char op, IEnumerable<IEvaluate<double>> children)
		{
			switch (op)
			{
				case Sum.SYMBOL:
					return catalog.SumOf(children);
				case Product.SYMBOL:
					return catalog.ProductOf(children);
			}

			throw new ArgumentException("Invalid operator.", nameof(op));
		}

		public static IEvaluate<double> GetFunction(
			this ICatalog<IEvaluate<double>> catalog, char op, params IEvaluate<double>[] children)
		{
			switch (op)
			{
				case Exponent.SYMBOL:
					if (children.Length != 2) throw new ArgumentException("Must have 2 child params for an exponent.");
					return catalog.GetExponent(children[0], children[1]);
			}

			throw new ArgumentException("Invalid function.", nameof(op));
		}
	}
}
