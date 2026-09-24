using Open.Evaluation.Boolean;

namespace Open.Evaluation.Tests.Boolean;

[TestClass]
public class Conditional
{
	[TestMethod]
	public void ConditionTrue_ReturnsIfTrueBranch()
	{
		var catalog = new EvaluationCatalog<double>();
		var boolCatalog = new EvaluationCatalog<bool>();
		var cond = boolCatalog.GetParameter(0);
		var ifTrue = catalog.GetConstant(1d);
		var ifFalse = catalog.GetConstant(2d);

		var conditional = catalog.Conditional((cond, ifTrue, ifFalse));

		using var lease = Context.Rent();
		var context = lease.Item;
		context.AddParam(boolCatalog, 0, true);
		conditional.Evaluate(context).Result.Should().Be(1d);
	}

	[TestMethod]
	public void ConditionFalse_ReturnsIfFalseBranch()
	{
		var catalog = new EvaluationCatalog<double>();
		var boolCatalog = new EvaluationCatalog<bool>();
		var cond = boolCatalog.GetParameter(0);
		var ifTrue = catalog.GetConstant(1d);
		var ifFalse = catalog.GetConstant(2d);

		var conditional = catalog.Conditional((cond, ifTrue, ifFalse));

		using var lease = Context.Rent();
		var context = lease.Item;
		context.AddParam(boolCatalog, 0, false);
		conditional.Evaluate(context).Result.Should().Be(2d);
	}

	[TestMethod]
	public void Description_FormatsAsTernary()
	{
		var catalog = new EvaluationCatalog<double>();
		var boolCatalog = new EvaluationCatalog<bool>();
		var cond = boolCatalog.GetParameter(0);
		var ifTrue = catalog.GetParameter(0);
		var ifFalse = catalog.GetParameter(1);

		var conditional = catalog.Conditional((cond, ifTrue, ifFalse));
		conditional.Description.Value.Should().Be("({0} ? {0} : {1})", "parenthesized: a bare ternary is ambiguous under a prefix operator and collided in the catalog");
	}

	[TestMethod]
	public void NullCondition_Throws()
	{
		var catalog = new EvaluationCatalog<double>();
		Action act = () => catalog.Conditional((null!, catalog.GetConstant(1d), catalog.GetConstant(2d)));
		act.Should().Throw<ArgumentNullException>();
	}

	[TestMethod]
	public void NullIfTrue_Throws()
	{
		var catalog = new EvaluationCatalog<double>();
		var boolCatalog = new EvaluationCatalog<bool>();
		var cond = boolCatalog.GetParameter(0);
		Action act = () => catalog.Conditional((cond, null!, catalog.GetConstant(2d)));
		act.Should().Throw<ArgumentNullException>();
	}

	[TestMethod]
	public void NullIfFalse_Throws()
	{
		var catalog = new EvaluationCatalog<double>();
		var boolCatalog = new EvaluationCatalog<bool>();
		var cond = boolCatalog.GetParameter(0);
		Action act = () => catalog.Conditional((cond, catalog.GetConstant(1d), null!));
		act.Should().Throw<ArgumentNullException>();
	}

	[TestMethod]
	public void OnlyEvaluatesTakenBranch()
	{
		// The untaken branch's own evaluation shouldn't be forced/registered as part of
		// resolving the conditional's result - Condition ? IfTrue.Evaluate() : IfFalse.Evaluate()
		// short-circuits at the C# level.
		var catalog = new EvaluationCatalog<double>();
		var boolCatalog = new EvaluationCatalog<bool>();
		var cond = boolCatalog.GetParameter(0);
		var ifTrue = catalog.GetConstant(1d);
		var ifFalse = catalog.GetConstant(2d);
		var conditional = catalog.Conditional((cond, ifTrue, ifFalse));

		using var lease = Context.Rent();
		var context = lease.Item;
		context.AddParam(boolCatalog, 0, true);
		conditional.Evaluate(context).Result.Should().Be(1d);

		// The untaken branch (ifFalse) was never added to the context.
		context.TryGetResult<double>(ifFalse, out _).Should().BeFalse();
	}

	[TestMethod]
	public void Interning_StructurallyIdentical_ReturnsSameInstance()
	{
		var catalog = new EvaluationCatalog<double>();
		var boolCatalog = new EvaluationCatalog<bool>();
		var cond = boolCatalog.GetParameter(0);
		var ifTrue = catalog.GetParameter(0);
		var ifFalse = catalog.GetParameter(1);

		var a = catalog.Conditional((cond, ifTrue, ifFalse));
		var b = catalog.Conditional((boolCatalog.GetParameter(0), catalog.GetParameter(0), catalog.GetParameter(1)));

		ReferenceEquals(a, b).Should().BeTrue();
	}

	[TestMethod]
	public void NewUsing_ExplicitInterface_WithSecondCatalog_RegistersInThatCatalog()
	{
		// The explicit IReproducable<...>.NewUsing(catalog, param) overload used to ignore the
		// passed-in catalog and rebuild through the instance's own Catalog instead -- every other
		// operator (And, Or, Not, Exponent, GreaterThan, LessThan) forwards the passed catalog; this
		// pins the same contract for Conditional.
		using var catalog1 = new EvaluationCatalog<double>();
		using var catalog2 = new EvaluationCatalog<double>();
		using var boolCatalog = new EvaluationCatalog<bool>();
		var cond = boolCatalog.GetParameter(0);
		var ifTrue = catalog1.GetConstant(1d);
		var ifFalse = catalog1.GetConstant(2d);

		var original = (global::Open.Evaluation.Boolean.Conditional<double>)catalog1.Conditional((cond, ifTrue, ifFalse));
		IReproducable<(IEvaluate<bool>, IEvaluate<double>, IEvaluate<double>), IEvaluate<double>> reproducable = original;

		var rebuilt = (global::Open.Evaluation.Boolean.Conditional<double>)reproducable.NewUsing(catalog2, (cond, ifTrue, ifFalse));

		rebuilt.Catalog.Should().BeSameAs(catalog2,
			"the explicit interface overload must register the rebuilt node in the CALLER's catalog, not the original instance's own");
		ReferenceEquals(rebuilt, catalog2.Conditional((cond, ifTrue, ifFalse))).Should().BeTrue(
			"the rebuilt node should be interned in catalog2's own registry, not catalog1's");
	}
}
