using Open.Evaluation.Boolean;

namespace Open.Evaluation.Tests.Boolean;

[TestClass]
public class Not
{
	[DataTestMethod]
	[DataRow(true, false)]
	[DataRow(false, true)]
	public void Negates(bool value, bool expected)
	{
		var catalog = new EvaluationCatalog<bool>();
		var p0 = catalog.GetParameter(0);
		var not = catalog.Not(p0);

		using var lease = Context.Rent();
		var context = lease.Item.Init(catalog, (ReadOnlySpan<bool>)[value]);
		not.Evaluate(context).Result.Should().Be(expected);
	}

	[TestMethod]
	public void DoubleNegation_RestoresOriginal()
	{
		var catalog = new EvaluationCatalog<bool>();
		var p0 = catalog.GetParameter(0);
		var notNot = catalog.Not(catalog.Not(p0));

		using var lease = Context.Rent();
		var context = lease.Item.Init(catalog, (ReadOnlySpan<bool>)[true]);
		notNot.Evaluate(context).Result.Should().BeTrue();
	}

	/// <summary>
	/// BUG (fixed in Open.Evaluation.Boolean/Not.cs as part of this coverage pass): the same
	/// defect reported against the 1.x line - "Not rendered without its !" - was ALSO present
	/// on this 2.x line. <c>Not</c> never overrode <c>OperatorBase.Describe(children)</c>, and
	/// the inherited default only injects the operator's Symbol.Text *between* multiple
	/// children; with exactly one child (Not always has exactly one) that join logic never
	/// runs, so the static/unparameterized description - and therefore ToString() and the
	/// catalog interning key - rendered as "({0})" instead of "!{0}", silently losing the
	/// negation. Fixed by adding an explicit override that prefixes the symbol. This test
	/// verifies the fix.
	/// </summary>
	[TestMethod]
	public void Description_IncludesExclamationMark()
	{
		var catalog = new EvaluationCatalog<bool>();
		var p0 = catalog.GetParameter(0);
		var not = catalog.Not(p0);

		not.Description.Value.Should().Contain("!");
		not.Description.Value.Should().Be("!{0}");
	}

	/// <summary>
	/// Companion to <see cref="Description_IncludesExclamationMark"/>: the evaluated
	/// (parameter-resolved) description must also include "!" and must reflect the CHILD's
	/// resolved value (not the post-negation result, which the old
	/// <c>v =&gt; $"!{v}"</c> implementation accidentally bound to - producing the misleading
	/// "!True" when the actual negated result was true).
	/// </summary>
	[TestMethod]
	public void EvaluatedDescription_ShowsNegatedChildValue()
	{
		var catalog = new EvaluationCatalog<bool>();
		var p0 = catalog.GetParameter(0);
		var not = catalog.Not(p0);

		using var lease = Context.Rent();
		var context = lease.Item.Init(catalog, (ReadOnlySpan<bool>)[false]);

		var result = not.Evaluate(context);
		result.Result.Should().BeTrue(); // !false == true
		result.Description.Value.Should().Be("!False", "it should negate the child's own resolved text (False), not the operation's own result");
	}

	[TestMethod]
	public void NullChild_Throws()
	{
		var catalog = new EvaluationCatalog<bool>();
		Action act = () => catalog.Not(null!);
		act.Should().Throw<ArgumentNullException>();
	}

	[TestMethod]
	public void Interning_StructurallyIdentical_ReturnsSameInstance()
	{
		var catalog = new EvaluationCatalog<bool>();
		var a = catalog.Not(catalog.GetParameter(0));
		var b = catalog.Not(catalog.GetParameter(0));

		ReferenceEquals(a, b).Should().BeTrue();
	}
}
