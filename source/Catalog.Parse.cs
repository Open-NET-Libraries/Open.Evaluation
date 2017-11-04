using Open.Evaluation.Arithmetic;
using Open.Evaluation.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Open.Evaluation  
{
	public static partial class CatalogExtensions
	{
		static readonly Regex unnecessaryParaenthesis = new Regex(@"\(({\w+})\)");
		static readonly Regex paramOnly = new Regex(@"^(?:{(\d+)})$");
		static readonly Regex registeredOnly = new Regex(@"^(?:{(\w+)})$");

		static Regex GetOperatorRegex(char op)
		{
			return new Regex(
				string.Format(@"{0} (?:\s*\{1}\s* {0} )+", @"({\w+}|\d+(?:\.\d*)*)", op),
				RegexOptions.IgnorePatternWhitespace);
		}

		static readonly Regex products = GetOperatorRegex('*');
		static readonly Regex sums = GetOperatorRegex('+');
		static readonly Regex exponents = GetOperatorRegex('^');

		static IEnumerable<IEvaluate<double>> SubMatches(Catalog<IEvaluate<double>> catalog, Dictionary<string, IEvaluate<double>> registry, Match m)
		{
			return m.Groups.Skip(1).SelectMany(g=>g.Captures).Select(c => c.Value).Select(v =>
			{
				if (double.TryParse(v, out double constant)) return catalog.GetConstant(constant);
				if (v.StartsWith('{') && v.EndsWith('}'))
				{
					v = v.TrimStart('{').TrimEnd('}');
					if (registry.TryGetValue(v, out IEvaluate<double> result)) return result;
					if (ushort.TryParse(v, out ushort p)) return catalog.GetParameter(p);
				}
				throw new InvalidOperationException(string.Format("Unrecognized evaluation sequence: {0}", v));
			});
		}

		public static IEvaluate<double> Parse(this Catalog<IEvaluate<double>> catalog, string evaluation)
		{
			var original = evaluation;
			if (String.IsNullOrWhiteSpace(evaluation))
				return null;

			evaluation = evaluation.Trim();

			var count = 0;
			var registry = new Dictionary<string, IEvaluate<double>>();

			string last;
			do
			{
				last = evaluation;

				evaluation = unnecessaryParaenthesis.Replace(evaluation, "$1");

				if (double.TryParse(evaluation, out double constantOnly))
					return catalog.GetConstant(constantOnly);

				var checkParamOnly = paramOnly.Match(evaluation);
				if (checkParamOnly.Success) return catalog.GetParameter(ushort.Parse(checkParamOnly.Groups[1].Value));

				evaluation = products.Replace(evaluation, m =>
				{
					var key = string.Format("X{0}", ++count);
					registry.Add(key, catalog.ProductOf(SubMatches(catalog, registry, m)));
					return '{' + key + '}';
				});

				evaluation = sums.Replace(evaluation, m =>
				{
					var key = string.Format("X{0}", ++count);
					registry.Add(key, catalog.SumOf(SubMatches(catalog, registry, m)));
					return '{' + key + '}';
				});

				evaluation = exponents.Replace(evaluation, m =>
				{
					var key = string.Format("X{0}", ++count);
					var sm = SubMatches(catalog, registry, m).ToArray();
					if (sm.Length != 2) throw new InvalidOperationException(string.Format("Exponent with {0} elements defined.", sm.Length));
					registry.Add(key, catalog.GetExponent(sm.First(), sm.Last()));
					return '{' + key + '}';
				});

			}
			while (last != evaluation);

			var checkRegisteredOnly = registeredOnly.Match(evaluation);
			if (checkRegisteredOnly.Success) return registry[checkRegisteredOnly.Groups[1].Value];

			throw new InvalidOperationException(string.Format("Could not parse sequence: {0}", original));
		}
	}
}
