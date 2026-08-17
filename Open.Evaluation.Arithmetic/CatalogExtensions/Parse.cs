using System.Globalization;

namespace Open.Evaluation.Arithmetic;

public static partial class CatalogExtensions
{
	static readonly Regex OpenParenPattern = GetOpenParenPattern();
	static readonly Regex CloseParenPattern = GetCloseParenPattern();
	static readonly Regex UnnecessaryParenPattern = GetUnnecessaryParenPattern();
	static readonly Regex ParamOnlyPatern = GetParamOnlyPattern();
	static readonly Regex RegisteredOnlyPattern = GetRegisteredOnlyPattern();

	static Regex GetOperatorRegex(string op) => new(
			string.Format(CultureInfo.InvariantCulture, @"\(\s*{0} (?:\s*{1}\s* {0} )+\s*\)", @"([-+]?\s*{\w+}|[-+]?\s*\d+(?:\.\d*)*)", op),
			RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

	static readonly Regex ProductsPattern = GetOperatorRegex(@"\*");
	static readonly Regex SumsPattern = GetOperatorRegex(@"\+");
	static readonly Regex ExponentsPattern = GetOperatorRegex(@"\^");
	static readonly ImmutableArray<char> PlusMinus = ['+', '-'];

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
			if (double.TryParse(v, out double constant)) return catalog.GetConstant(constant);

            ReadOnlySpan<char> span = v.AsSpan().Trim();
            bool negative = !span.IsEmpty && span[0] == '-';
			span = span.Trim(PlusMinus.AsSpan());

            int len = span.Length;
			if (len == 0 || span[0] != '{' || span[len - 1] != '}')
				throw new FormatException($"Unrecognized evaluation sequence: {span.ToString()}");

			v = span.TrimStart('{').TrimEnd('}').ToString();

			if (negative)
			{
				if (registry.TryGetValue(v, out IEvaluate<double>? result)) return catalog.ProductOf(-1, result);
				if (ushort.TryParse(v, out ushort p)) return catalog.ProductOf(-1, catalog.GetParameter(p));
			}
			else
			{
				if (registry.TryGetValue(v, out IEvaluate<double>? result)) return result;
				if (ushort.TryParse(v, out ushort p)) return catalog.GetParameter(p);
			}

			throw new FormatException($"Unrecognized evaluation sequence: {v}");
		});

	public static IEvaluate<double> Parse(this Catalog<IEvaluate<double>> catalog, string evaluation)
	{
        string original = evaluation ?? throw new ArgumentNullException(nameof(evaluation));
		if (string.IsNullOrWhiteSpace(evaluation))
			throw new ArgumentException("Must be more than just whitespace or empty.", nameof(evaluation));

        int oParenCount = OpenParenPattern.Count(evaluation);
        int cParenCount = CloseParenPattern.Count(evaluation);
		if (oParenCount > cParenCount) throw new FormatException("Missing close parenthesis.");
		if (oParenCount < cParenCount) throw new FormatException("Missing open parenthesis.");

        RecycleHelper<Dictionary<string, IEvaluate<double>>> lease = DictionaryPool<string, IEvaluate<double>>.Rent();
        Dictionary<string, IEvaluate<double>> registry = lease.Item;
        int count = 0;

		evaluation = evaluation.Trim();
		string last;
		do
		{
			last = evaluation;

			evaluation = UnnecessaryParenPattern.Replace(evaluation, "$1");

			if (double.TryParse(evaluation, out double constantOnly))
				return catalog.GetConstant(constantOnly);

            Match checkParamOnly = ParamOnlyPatern.Match(evaluation);
			if (checkParamOnly.Success) return catalog.GetParameter(ushort.Parse(checkParamOnly.Groups[1].Value, CultureInfo.InvariantCulture));

			evaluation = ProductsPattern.Replace(evaluation, m =>
			{
                string key = $"X{++count}";
				registry.Add(key, catalog.ProductOf(SubMatches(catalog, registry, m)));
				return $"{{{key}}}";
			});

			evaluation = SumsPattern.Replace(evaluation, m =>
			{
                string key = $"X{++count}";
				registry.Add(key, catalog.SumOf(SubMatches(catalog, registry, m)));
				return $"{{{key}}}";
			});

			evaluation = ExponentsPattern.Replace(evaluation, m =>
			{
                string key = $"X{++count}";
                IEvaluate<double>[] sm = SubMatches(catalog, registry, m).ToArray();
				if (sm.Length != 2) throw new FormatException($"Exponent with {sm.Length} elements defined.");
				registry.Add(key, catalog.GetExponent(sm[0], sm[^1]));
				return $"{{{key}}}";
			});
		}
		while (last != evaluation);

        Match checkRegisteredOnly = RegisteredOnlyPattern.Match(evaluation);
		return checkRegisteredOnly.Success
			? registry[checkRegisteredOnly.Groups[1].Value]
			: throw new FormatException($"Could not parse sequence: {original}");
	}

	[GeneratedRegex("[(]", RegexOptions.Compiled)]
	private static partial Regex GetOpenParenPattern();
	[GeneratedRegex("[)]", RegexOptions.Compiled)]
	private static partial Regex GetCloseParenPattern();
	[GeneratedRegex("\\(({\\w+})\\)", RegexOptions.Compiled)]
	private static partial Regex GetUnnecessaryParenPattern();
	[GeneratedRegex("^(?:{(\\d+)})$", RegexOptions.Compiled)]
	private static partial Regex GetParamOnlyPattern();
	[GeneratedRegex("^(?:{(\\w+)})$", RegexOptions.Compiled)]
	private static partial Regex GetRegisteredOnlyPattern();
}
