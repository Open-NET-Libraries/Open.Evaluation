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
}
