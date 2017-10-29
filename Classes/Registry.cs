using Open.Evaluation.ArithmeticOperators;
using Open.Evaluation.BooleanOperators;
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

	}
}
