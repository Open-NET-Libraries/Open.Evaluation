using Open.Evaluation.Boolean;

namespace Open.Evaluation.Tests.Registry;

[TestClass]
public class BooleanRegistry
{
	static (EvaluationCatalog<bool> Catalog, IEvaluate<bool>[] Ps) Setup()
	{
		var catalog = new EvaluationCatalog<bool>();
		return (catalog, [catalog.GetParameter(0), catalog.GetParameter(1)]);
	}

	/// <summary>
	/// Regression test for a historical defect in the 1.x line: the Registry's operator
	/// dispatch for '&amp;' and '|' was wired to the arithmetic Sum/Product operators
	/// instead of the boolean And/Or operators. This verifies '&amp;' actually performs
	/// logical AND (not arithmetic addition) on this (2.x) line.
	/// </summary>
	[TestMethod]
	public void GetOperator_And_ProducesLogicalAnd_NotArithmeticSum()
	{
		var (catalog, ps) = Setup();
		var op = Open.Evaluation.Boolean.Registry.GetOperator(catalog, Glyphs.And, ps);

		op.Should().BeOfType<And>();

		using var lease = Context.Rent();
		var context = lease.Item.Init(catalog, (ReadOnlySpan<bool>)[true, false]);
		op.Evaluate(context).Result.Should().BeFalse(); // true AND false = false (not true+false-ish truthy)
	}

	/// <summary>
	/// Regression test for a historical defect in the 1.x line: see
	/// <see cref="GetOperator_And_ProducesLogicalAnd_NotArithmeticSum"/>. This verifies
	/// '|' actually performs logical OR (not arithmetic multiplication) on this (2.x) line.
	/// </summary>
	[TestMethod]
	public void GetOperator_Or_ProducesLogicalOr_NotArithmeticProduct()
	{
		var (catalog, ps) = Setup();
		var op = Open.Evaluation.Boolean.Registry.GetOperator(catalog, Glyphs.Or, ps);

		op.Should().BeOfType<Or>();

		using var lease = Context.Rent();
		var context = lease.Item.Init(catalog, (ReadOnlySpan<bool>)[false, false]);
		op.Evaluate(context).Result.Should().BeFalse();

		var context2 = new Context();
		context2.Init(catalog, (ReadOnlySpan<bool>)[true, false]);
		op.Evaluate(context2).Result.Should().BeTrue();
	}

	[TestMethod]
	public void GetOperator_InvalidGlyph_Throws()
	{
		var (catalog, ps) = Setup();
		Action act = () => Open.Evaluation.Boolean.Registry.GetOperator(catalog, '%', ps);
		act.Should().Throw<ArgumentException>();
	}

	[TestMethod]
	public void Operators_ContainsAndOr_Only()
	{
		Open.Evaluation.Boolean.Registry.Operators.Should().BeEquivalentTo([Glyphs.And, Glyphs.Or]);
	}

	[TestMethod]
	public void Functions_ContainsNotAndConditional()
	{
		Open.Evaluation.Boolean.Registry.Functions.Should().BeEquivalentTo([Glyphs.Not, Glyphs.Conditional]);
	}

	[TestMethod]
	public void CountingFunctions_ContainsAllThreeCountingOperators()
	{
		Open.Evaluation.Boolean.Registry.CountingFunctions.Should()
			.BeEquivalentTo(["AtLeast", "AtMost", "Exactly"]);
	}

	[TestMethod]
	public void GetRandomOperator_AlwaysProducesRegisteredOperator()
	{
		var (catalog, ps) = Setup();
		for (var i = 0; i < 20; i++)
		{
			var op = Open.Evaluation.Boolean.Registry.GetRandomOperator(catalog, ps);
			op.Should().NotBeNull();
			(op is And or Or).Should().BeTrue();
		}
	}

	[TestMethod]
	public void GetRandomOperator_ExceptAnd_AlwaysProducesOr()
	{
		var (catalog, ps) = Setup();
		for (var i = 0; i < 20; i++)
		{
			var op = Open.Evaluation.Boolean.Registry.GetRandomOperator(catalog, ps, Glyphs.And);
			op.Should().BeOfType<Or>();
		}
	}

	[TestMethod]
	public void GetRandomOperator_ExceptOr_AlwaysProducesAnd()
	{
		var (catalog, ps) = Setup();
		for (var i = 0; i < 20; i++)
		{
			var op = Open.Evaluation.Boolean.Registry.GetRandomOperator(catalog, ps, Glyphs.Or);
			op.Should().BeOfType<And>();
		}
	}

	/// <summary>
	/// BUG (upstream, not in Open.Evaluation): excluding every operator via the
	/// <c>(char except, params char[] others)</c> overload should leave nothing to select and
	/// return null, but it still returns a live operator. Root-caused via an isolated repro
	/// against <c>Open.RandomizationExtensions.Randomizer.TryRandomSelectOneExcept</c> (v2.5.3)
	/// directly: <c>ImmutableArray&lt;char&gt; three = ['a','b','c'];
	/// three.TryRandomSelectOneExcept(out var v, 'a', ['b'])</c> still returns both 'a' and 'b'
	/// as possible results across repeated calls - the <c>others</c> array is silently ignored;
	/// only the single <c>excluding</c> argument is ever honored. This is NOT a defect in
	/// Open.Evaluation: <c>Open.Evaluation.Boolean.Registry.GetRandomOperator</c> (and the
	/// equivalent overloads in <c>Open.Evaluation.Arithmetic.Registry</c> /
	/// <c>GetRandomFunction</c>) call the dependency correctly; the dependency itself
	/// (Open.RandomizationExtensions, a separate package/repo not present in this worktree) is
	/// where the fix belongs. Note the *other* exclusion overload taking
	/// <c>IEnumerable&lt;char&gt; except</c> (see
	/// <see cref="GetRandomOperator_ExceptEnumerable_ExcludesGivenGlyphs"/>) uses a different,
	/// unaffected code path (TryRandomSelectOne with a HashSet) and works correctly.
	/// </summary>
	[Ignore("BUG (upstream, Open.RandomizationExtensions.Randomizer.TryRandomSelectOneExcept): the 'others' params array is silently ignored, so excluding more than one glyph via (except, params others) does not actually exclude the extras. See doc comment for an isolated repro.")]
	[TestMethod]
	public void GetRandomOperator_ExceptBoth_ReturnsNull()
	{
		var (catalog, ps) = Setup();
		var op = Open.Evaluation.Boolean.Registry.GetRandomOperator(catalog, ps, Glyphs.And, Glyphs.Or);
		op.Should().BeNull();
	}

	[TestMethod]
	public void GetRandomOperator_ExceptEnumerable_ExcludesGivenGlyphs()
	{
		var (catalog, ps) = Setup();
		for (var i = 0; i < 20; i++)
		{
			var op = Open.Evaluation.Boolean.Registry.GetRandomOperator(catalog, ps, (IEnumerable<char>)[Glyphs.And]);
			op.Should().BeOfType<Or>();
		}
	}

	[TestMethod]
	public void GetRandomOperator_NullExceptEnumerable_BehavesLikeUnfiltered()
	{
		var (catalog, ps) = Setup();
		var op = Open.Evaluation.Boolean.Registry.GetRandomOperator(catalog, ps, (IEnumerable<char>)null!);
		op.Should().NotBeNull();
		(op is And or Or).Should().BeTrue();
	}
}
