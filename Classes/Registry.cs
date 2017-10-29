using Open.Evaluation.ArithmeticOperators;
using Open.Evaluation.BooleanOperators;
using Open.Numeric;
using System.Collections.Generic;
using System.Linq;

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

			public static readonly IReadOnlyList<char> Operators = (new List<char> { ADD, MULTIPLY }).AsReadOnly();
			public static readonly IReadOnlyList<char> Functions = (new List<char> { EXPONENT }).AsReadOnly();

			public static char GetRandom(IEnumerable<char> excluded = null)
			{
				var ao = excluded == null
					? Operators
					: Operators.Where(o => !excluded.Contains(o)).ToArray();
				return ao.RandomSelectOne();
			}

			public static char GetRandom(char excluded)
			{
				var ao = Operators.Where(o => o != excluded).ToArray();
				return ao.RandomSelectOne();
			}

			public static char GetRandomFunction()
			{
				return Functions.RandomSelectOne();
			}

			public static char GetRandomFunction(char excluded)
			{
				var ao = Functions.Where(o => o != excluded).ToArray();
				return ao.RandomSelectOne();
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

			public static readonly IReadOnlyList<char> Operators = (new List<char> { AND, OR }).AsReadOnly();
			public static readonly IReadOnlyList<char> Functions = (new List<char> { NOT, CONDITIONAL }).AsReadOnly();

			public static char GetRandom(IEnumerable<char> excluded = null)
			{
				var ao = excluded == null
					? Operators
					: Operators.Where(o => !excluded.Contains(o)).ToArray();
				return ao.RandomSelectOne();
			}

			public static char GetRandom(char excluded)
			{
				var ao = Operators.Where(o => o != excluded).ToArray();
				return ao.RandomSelectOne();
			}

			public static char GetRandomFunction()
			{
				return Functions.RandomSelectOne();
			}

			public static char GetRandomFunction(char excluded)
			{
				var ao = Functions.Where(o => o != excluded).ToArray();
				return ao.RandomSelectOne();
			}


		}
	}
}
