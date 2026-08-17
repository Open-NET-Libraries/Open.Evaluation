using Open.Evaluation.Arithmetic;

namespace Open.Evaluation.Tests.Core;

/// <summary>
/// Pins interning-as-identity laws: structurally-equal construction (regardless of construction
/// order, where the library canonicalizes ordering) must yield the exact same registered instance,
/// and a parsed round-trip of a tree's own description must land back on that same instance.
/// </summary>
[TestClass]
public class InterningIdentityTests
{
	[TestMethod]
	public void SumOf_DifferentConstructionOrder_CanonicallyReordered_Interns()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		var p1 = catalog.GetParameter(1);

		// Sum reorders children canonically (OperatorBase's reorderChildren=true), so both
		// construction orders should collapse to the SAME registered instance.
		var sumAB = catalog.SumOf(p0, p1);
		var sumBA = catalog.SumOf(p1, p0);

		ReferenceEquals(sumAB, sumBA).Should().BeTrue(
			"Sum canonically reorders parameters by Id, so construction order must not matter");
	}

	[TestMethod]
	public void ProductOf_DifferentConstructionOrder_CanonicallyReordered_Interns()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		var p1 = catalog.GetParameter(1);

		var productAB = catalog.ProductOf(p0, p1);
		var productBA = catalog.ProductOf(p1, p0);

		ReferenceEquals(productAB, productBA).Should().BeTrue(
			"Product canonically reorders parameters by Id, so construction order must not matter");
	}

	[TestMethod]
	public void GetConstant_SameValue_DifferentCalls_Interns()
	{
		using var catalog = new EvaluationCatalog<double>();
		ReferenceEquals(catalog.GetConstant(5d), catalog.GetConstant(5d)).Should().BeTrue();
	}

	[TestMethod]
	public void GetParameter_SameId_DifferentCalls_Interns()
	{
		using var catalog = new EvaluationCatalog<double>();
		ReferenceEquals(catalog.GetParameter(3), catalog.GetParameter(3)).Should().BeTrue();
	}

	[TestMethod]
	public void ParseRoundTrip_RepresentativeTrees_ReturnsSameInternedInstance()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		var p1 = catalog.GetParameter(1);

		// NOTE: deliberately avoids constructing Exponent(base, constant-1) - per issue #6, that
		// shape's Description collapses to the bare base's own Description (the superscript "¹"
		// case), which causes a Catalog.Register id collision. Powers other than 1 are safe.
		//
		// NOTE: also deliberately excludes any Exponent whose rendered form uses Unicode
		// superscript digits (e.g. "({0}³)") - see the QUESTION-tagged test below documenting that
		// Parse() cannot read its own superscript output back.
		IEvaluate<double>[] trees =
		[
			catalog.SumOf(p0, p1),
			catalog.ProductOf(p0, p1),
			catalog.SumOf(catalog.ProductOf(p0, catalog.GetConstant(2d)), p1),
		];

		foreach (var tree in trees)
		{
			var representation = tree.Description.Value;
			var roundTripped = catalog.Parse(representation);

			ReferenceEquals(roundTripped, tree).Should().BeTrue(
				$"parsing '{representation}' back should yield the SAME interned instance");
		}
	}

	[TestMethod]
	[Ignore("QUESTION FOR AUTHOR: Exponent<T>'s Description renders integer powers using Unicode " +
		"superscript digits (e.g. Exponent(p0, 3) -> \"({0}³)\", per Exponent.ConvertToSuperScript), " +
		"but CatalogExtensions.Parse's ExponentsPattern/SubMatches only recognize the \"(base^power)\" " +
		"caret form as INPUT. Observed directly: catalog.Parse(catalog.GetExponent(p0, " +
		"catalog.GetConstant(3d)).Description.Value) throws System.FormatException: 'Could not parse " +
		"sequence: ({0}³)'. So Parse(x.Description.Value) is not a round-trip for any tree containing " +
		"an Exponent whose power renders as a superscript (i.e. any small positive integer power other " +
		"than the elided 1). Is Parse meant to accept its own pretty-printed superscript output, or is " +
		"round-tripping only guaranteed for the caret-form input grammar (in which case is that worth " +
		"documenting on Parse/Description)?")]
	public void ParseRoundTrip_ExponentWithSuperscriptPower_DoesNotRoundTrip()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		var exponent = catalog.GetExponent(p0, catalog.GetConstant(3d));

		var roundTripped = catalog.Parse(exponent.Description.Value);
		ReferenceEquals(roundTripped, exponent).Should().BeTrue();
	}
}
