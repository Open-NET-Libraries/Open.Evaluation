using Open.Disposable;
using Open.Evaluation.Arithmetic;
using Open.Evaluation.Core;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Open.Evaluation;

public static class CatalogExtensions
{
	static readonly Regex openParen = new("[(]", RegexOptions.Compiled);
	static readonly Regex closeParen = new("[)]", RegexOptions.Compiled);
	static readonly Regex unnecessaryParaenthesis = new(@"\(({\w+})\)", RegexOptions.Compiled);
	static readonly Regex paramOnly = new(@"^(?:{(\d+)})$", RegexOptions.Compiled);
	static readonly Regex registeredOnly = new(@"^(?:{(\w+)})$", RegexOptions.Compiled);

	static Regex GetOperatorRegex(string op) => new(
			string.Format(CultureInfo.InvariantCulture, @"\(\s*{0} (?:\s*{1}\s* {0} )+\s*\)", @"([-+]?\s*{\w+}|[-+]?\s*\d+(?:\.\d*)*)", op),
			RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

	static readonly Regex products = GetOperatorRegex(@"\*");
	static readonly Regex sums = GetOperatorRegex(@"\+");
	static readonly Regex exponents = GetOperatorRegex(@"\^");
	static readonly ImmutableArray<char> plusMinus = ImmutableArray.Create('+', '-');

	static IEnumerable<IEvaluate<double>> SubMatches(
		Catalog<IEvaluate<double>> catalog,
		Dictionary<string, IEvaluate<double>> registry,
		Match m) => m.Groups.
			Cast<Group>()
			.Skip(1)
			.SelectMany(g => g.Captures.Cast<Capture>())
			.Select(c => c.Value)
			.Select(v =>
		{
			if (double.TryParse(v, out var constant)) return catalog.GetConstant(constant);

			var span = v.AsSpan().Trim();
			var negative = !span.IsEmpty && span[0] == '-';
			span = span.Trim(plusMinus.AsSpan());

			var len = span.Length;
			if (len == 0 || span[0] != '{' || span[len - 1] != '}')
				throw new FormatException($"Unrecognized evaluation sequence: {span.ToString()}");

			v = span.TrimStart('{').TrimEnd('}').ToString();

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

			throw new FormatException($"Unrecognized evaluation sequence: {v}");
		});

	public static IEvaluate<double> Parse(this Catalog<IEvaluate<double>> catalog, string evaluation)
	{
		var original = evaluation ?? throw new ArgumentNullException(nameof(evaluation));
		if (string.IsNullOrWhiteSpace(evaluation))
			throw new ArgumentException("Must be more than just whitespace or empty.", nameof(evaluation));

		var oParenCount = openParen.Matches(evaluation).Count;
		var cParenCount = closeParen.Matches(evaluation).Count;
		if (oParenCount > cParenCount) throw new FormatException("Missing close parenthesis.");
		if (oParenCount < cParenCount) throw new FormatException("Missing open parenthesis.");

		var lease = DictionaryPool<string, IEvaluate<double>>.Rent();
		var registry = lease.Item;
		var count = 0;

		evaluation = evaluation.Trim();
		string last;
		do
		{
			last = evaluation;

			evaluation = unnecessaryParaenthesis.Replace(evaluation, "$1");

			if (double.TryParse(evaluation, out var constantOnly))
				return catalog.GetConstant(constantOnly);

			var checkParamOnly = paramOnly.Match(evaluation);
			if (checkParamOnly.Success) return catalog.GetParameter(ushort.Parse(checkParamOnly.Groups[1].Value, CultureInfo.InvariantCulture));

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
				if (sm.Length != 2) throw new FormatException($"Exponent with {sm.Length} elements defined.");
				registry.Add(key, catalog.GetExponent(sm[0], sm.Last()));
				return '{' + key + '}';
			});
		}
		while (last != evaluation);

		var checkRegisteredOnly = registeredOnly.Match(evaluation);
		return checkRegisteredOnly.Success
			? registry[checkRegisteredOnly.Groups[1].Value]
			: throw new FormatException($"Could not parse sequence: {original}");
	}
}
