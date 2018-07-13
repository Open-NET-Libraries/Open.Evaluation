using Open.Evaluation.Arithmetic;
using Open.Evaluation.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Open.Evaluation
{
	public static partial class CatalogExtensions
	{
		static readonly Regex openParen = new Regex(@"[(]");
		static readonly Regex closeParen = new Regex(@"[)]");
		static readonly Regex unnecessaryParaenthesis = new Regex(@"\(({\w+})\)");
		static readonly Regex paramOnly = new Regex(@"^(?:{(\d+)})$");
		static readonly Regex registeredOnly = new Regex(@"^(?:{(\w+)})$");

		static Regex GetOperatorRegex(string op)
		{
			return new Regex(
				string.Format(@"\(\s*{0} (?:\s*{1}\s* {0} )+\s*\)", @"([-+]?\s*{\w+}|[-+]?\s*\d+(?:\.\d*)*)", op),
				RegexOptions.IgnorePatternWhitespace);
		}

		static readonly Regex products = GetOperatorRegex(@"\*");
		static readonly Regex sums = GetOperatorRegex(@"\+");
		static readonly Regex exponents = GetOperatorRegex(@"\^");

		static IEnumerable<IEvaluate<double>> SubMatches(Catalog<IEvaluate<double>> catalog, Dictionary<string, IEvaluate<double>> registry, Match m)
		{
			return m.Groups.
				Cast<Group>()
				.Skip(1)
				.SelectMany(g => g.Captures.Cast<Capture>())
				.Select(c => c.Value)
				.Select(v =>
			{
				if (double.TryParse(v, out var constant)) return catalog.GetConstant(constant);

				v = v.Trim();
				var negative = v.Length != 0 && v[0] == '-';
				v = v.Trim('+', '-');

				var len = v.Length;
				if (len != 0 && v[0] == '{' && v[len - 1] == '}')
				{
					v = v.TrimStart('{').TrimEnd('}');

					if (negative)
					{
						if (registry.TryGetValue(v, out var result)) return catalog.ProductOf(-1, result);
						if (ushort.TryParse(v, out var p)) return catalog.ProductOf(-1, catalog.GetParameter(p));
					}
					else
					{
						if (registry.TryGetValue(v, out var result)) return result;
						if (ushort.TryParse(v, out var p)) return catalog.GetParameter(p);
					}
				}
				throw new InvalidOperationException($"Unrecognized evaluation sequence: {v}");
			});
		}

		public static IEvaluate<double> Parse(this Catalog<IEvaluate<double>> catalog, string evaluation)
		{
			var original = evaluation;
			if (string.IsNullOrWhiteSpace(evaluation))
				return null;

			evaluation = evaluation.Trim();

			var oParenCount = openParen.Matches(evaluation).Count;
			var cParenCount = closeParen.Matches(evaluation).Count;
			if (oParenCount > cParenCount) throw new FormatException("Missing close parenthesis.");
			if (oParenCount < cParenCount) throw new FormatException("Missing open parenthesis.");

			var count = 0;
			var registry = new Dictionary<string, IEvaluate<double>>();

			string last;
			do
			{
				last = evaluation;

				evaluation = unnecessaryParaenthesis.Replace(evaluation, "$1");

				if (double.TryParse(evaluation, out var constantOnly))
					return catalog.GetConstant(constantOnly);

				var checkParamOnly = paramOnly.Match(evaluation);
				if (checkParamOnly.Success) return catalog.GetParameter(ushort.Parse(checkParamOnly.Groups[1].Value));

				evaluation = products.Replace(evaluation, m =>
				{
					var key = $"X{++count}";
					registry.Add(key, catalog.ProductOf(SubMatches(catalog, registry, m)));
					return '{' + key + '}';
				});

				evaluation = sums.Replace(evaluation, m =>
				{
					var key = $"X{++count}";
					registry.Add(key, catalog.SumOf(SubMatches(catalog, registry, m)));
					return '{' + key + '}';
				});

				evaluation = exponents.Replace(evaluation, m =>
				{
					var key = $"X{++count}";
					var sm = SubMatches(catalog, registry, m).ToArray();
					if (sm.Length != 2) throw new FormatException(string.Format("Exponent with {0} elements defined.", sm.Length));
					registry.Add(key, catalog.GetExponent(sm.First(), sm.Last()));
					return '{' + key + '}';
				});

			}
			while (last != evaluation);

			var checkRegisteredOnly = registeredOnly.Match(evaluation);
			if (checkRegisteredOnly.Success) return registry[checkRegisteredOnly.Groups[1].Value];

			throw new FormatException(string.Format("Could not parse sequence: {0}", original));
		}
	}
}
