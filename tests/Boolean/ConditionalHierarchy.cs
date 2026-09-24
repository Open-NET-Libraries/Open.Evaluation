using Open.Evaluation.Arithmetic;
using Open.Evaluation.Boolean;
using Open.Hierarchy;

namespace Open.Evaluation.Tests.Boolean;

// Conditional is a parent: without IParent it was invisible to descendant traversal, so a consumer
// discovering the parameters of "p1 ? p5 : p4" found none and could bind none -- and gene counts,
// node mapping and rebuilding all treated it as a leaf.
[TestClass]
public class ConditionalHierarchy
{
	[TestMethod]
	public void UntypedParent_ExposesAllThreeChildren_ForAnyResultType()
	{
		using var bools = new EvaluationCatalog<bool>();
		using var doubles = new EvaluationCatalog<double>();
		var cond = bools.GetParameter(0);

		var boolConditional = bools.Conditional((cond, bools.GetParameter(1), bools.GetParameter(2)));
		var numConditional = doubles.Conditional((cond, doubles.GetParameter(1), doubles.GetParameter(2)));

		((IParent)boolConditional).Children.Should().HaveCount(3);
		((IParent)numConditional).Children.Should().HaveCount(3);

		// Descendant traversal now reaches the condition's parameter as well as the branches'.
		((IParent)numConditional).GetDescendants().OfType<IParameter>().Select(p => p.Id)
			.Should().BeEquivalentTo(new ushort[] { 0, 1, 2 });
	}

	[TestMethod]
	public void TypedParent_HoldsSameTypedChildren_AllThreeForBool_BranchesOnlyOtherwise()
	{
		using var bools = new EvaluationCatalog<bool>();
		using var doubles = new EvaluationCatalog<double>();
		var cond = bools.GetParameter(0);

		var boolConditional = bools.Conditional((cond, bools.GetParameter(1), bools.GetParameter(2)));
		var numConditional = doubles.Conditional((cond, doubles.GetParameter(1), doubles.GetParameter(2)));

		((IParent<IEvaluate<bool>>)boolConditional).Children.Should().HaveCount(3);
		((IParent<IEvaluate<double>>)numConditional).Children.Should().HaveCount(2, "the bool condition cannot be a typed child of a double conditional");
	}

	[TestMethod]
	public void NodeMapAndFixHierarchy_RoundTripsABoolConditional_AndRebuildsAfterAnEdit()
	{
		using var catalog = new EvaluationCatalog<bool>();
		var p0 = catalog.GetParameter(0);
		var p1 = catalog.GetParameter(1);
		var p2 = catalog.GetParameter(2);
		var p3 = catalog.GetParameter(3);
		var mux2 = catalog.Conditional((p0, p2, p1));

		var tree = catalog.Factory.Map(mux2);
		tree.Children.Should().HaveCount(3, "a bool conditional maps all three children as nodes");

		// Unchanged tree rebuilds to the same interned instance.
		catalog.FixHierarchy(tree.Clone()).Recycle().Should().BeSameAs(mux2);

		// Replace the if-false branch (p1) with p3, rebuild: the condition and if-true survive.
		var edited = tree.Clone();
		var target = edited.Children.Cast<Node<IEvaluate<bool>>>().First(n => n.Value == p1);
		edited.Replace(target, edited.Source.Map(p3));
		var rebuilt = catalog.FixHierarchy(edited).Recycle();
		rebuilt.Description.Value.Should().Be("({0} ? {2} : {3})");
		tree.Recycle();
	}

	[TestMethod]
	public void FixHierarchy_NumericConditional_KeepsItsConditionThroughATypedRebuild()
	{
		using var bools = new EvaluationCatalog<bool>();
		using var doubles = new EvaluationCatalog<double>();
		var cond = bools.GetParameter(0);
		var a = doubles.GetParameter(1);
		var b = doubles.GetParameter(2);
		var c = doubles.GetParameter(3);
		var conditional = doubles.Conditional((cond, a, b));

		var tree = doubles.Factory.Map(conditional);
		tree.Children.Should().HaveCount(2, "only the branches are typed children of a double conditional");

		var edited = tree.Clone();
		var target = edited.Children.Cast<Node<IEvaluate<double>>>().First(n => n.Value == b);
		edited.Replace(target, edited.Source.Map(c));
		var rebuilt = doubles.FixHierarchy(edited).Recycle();
		rebuilt.Description.Value.Should().Be("({0} ? {1} : {3})", "the condition is recovered from the untyped view");
		tree.Recycle();
	}

	// Fixed-arity coverage: a conditional's typed children can never gain or lose a member.
	// FixHierarchy's conditional case (above) recovers the shape correctly when it is left alone;
	// these tests pin the library helpers that used to approve or perform edits FixHierarchy could
	// not then recover from -- one approved removing a branch, two appended a child outright, and
	// FixHierarchy itself inferred arity from a count instead of checking it (see file comment for
	// what that let through).

	[TestMethod]
	public void IsValidForRemoval_ConditionalBranchWithConstantSibling_ReturnsFalse()
	{
		using var bools = new EvaluationCatalog<bool>();
		using var doubles = new EvaluationCatalog<double>();
		var p = doubles.GetParameter(0);
		var q = doubles.GetParameter(1);
		var condition = bools.GreaterThan((p, q));
		var conditional = doubles.Conditional((condition, doubles.GetConstant(1d), doubles.GetConstant(2d)));

		var tree = doubles.Factory.Map(conditional);
		var ifTrueNode = tree.Children[0];

		// Both branches being constants is exactly the case the old "futility" check approved:
		// no dynamic sibling remained, and the conditional itself was the tree's root, so removal
		// looked worthwhile even though a conditional can never drop to one branch.
		ifTrueNode.IsValidForRemoval().Should().BeFalse(
			"a conditional's branches are fixed in number; the tree cannot lose one even when its sibling is a constant");
		tree.Recycle();
	}

	[TestMethod]
	public void AddParameter_ConditionalNode_ReturnsNull()
	{
		using var bools = new EvaluationCatalog<bool>();
		using var doubles = new EvaluationCatalog<double>();
		var p = doubles.GetParameter(0);
		var q = doubles.GetParameter(1);
		var condition = bools.GreaterThan((p, q));
		var conditional = doubles.Conditional((condition, doubles.GetParameter(2), doubles.GetParameter(3)));

		var node = doubles.Factory.Map(conditional);
		try
		{
			var result = doubles.Mutation.AddParameter(node);
			result.Should().BeNull("a conditional's typed children are fixed in number; appending a parameter would corrupt its arity");
		}
		finally
		{
			node.Recycle();
		}
	}

	[TestMethod]
	public void TryAddConstant_NumericConditionalNode_ReturnsNull()
	{
		using var bools = new EvaluationCatalog<bool>();
		using var doubles = new EvaluationCatalog<double>();
		var p = doubles.GetParameter(0);
		var q = doubles.GetParameter(1);
		var condition = bools.GreaterThan((p, q));
		var conditional = doubles.Conditional((condition, doubles.GetParameter(2), doubles.GetParameter(3)));

		var node = doubles.Factory.Map(conditional);
		try
		{
			var result = doubles.TryAddConstant(node, 5d);
			result.Should().BeNull("a numeric conditional's typed children are fixed at its two branches");
		}
		finally
		{
			node.Recycle();
		}
	}

	[TestMethod]
	public void TryAddConstant_BoolConditionalNode_ReturnsNull()
	{
		using var bools = new EvaluationCatalog<bool>();
		var cond = bools.GetParameter(0);
		var ifTrue = bools.GetParameter(1);
		var ifFalse = bools.GetParameter(2);
		var conditional = bools.Conditional((cond, ifTrue, ifFalse));

		var node = bools.Factory.Map(conditional);
		try
		{
			var result = bools.TryAddConstant(node, true);
			// Before this fix, a bool conditional's fourth (typed) child made FixHierarchy's
			// rebuilt-count check come out at 4 instead of 3, so it took the condition from the
			// untyped view but the LAST TWO typed children as branches -- silently reinterpreting
			// [cond, ifTrue, ifFalse, newConstant] as (cond, ifFalse, newConstant) and dropping
			// ifTrue with no throw at all.
			result.Should().BeNull("a bool conditional's typed children are fixed at three -- the condition and both branches");
		}
		finally
		{
			node.Recycle();
		}
	}

	[TestMethod]
	public void FixHierarchy_ConditionalWithWrongArity_Throws_ForDoubleAndBool()
	{
		using var bools = new EvaluationCatalog<bool>();
		using var doubles = new EvaluationCatalog<double>();

		// T = double: two typed children (branches only) is the whole contract; forcing a third
		// -- bypassing TryAddConstant, itself blocked above -- must throw rather than be
		// reinterpreted.
		var p = doubles.GetParameter(0);
		var q = doubles.GetParameter(1);
		var numCondition = bools.GreaterThan((p, q));
		var numConditional = doubles.Conditional((numCondition, doubles.GetParameter(2), doubles.GetParameter(3)));
		var numTree = doubles.Factory.Map(numConditional);
		var numEdited = numTree.Clone();
		numEdited.AddValue(doubles.GetConstant(5d));
		Action numAct = () => doubles.FixHierarchy(numEdited);
		numAct.Should().Throw<NotSupportedException>();
		numTree.Recycle();

		// T = bool: three typed children (condition + both branches) is the whole contract; a
		// fourth used to be silently reinterpreted as a replacement branch (see the test above).
		var cond = bools.GetParameter(0);
		var ifTrue = bools.GetParameter(1);
		var ifFalse = bools.GetParameter(2);
		var boolConditional = bools.Conditional((cond, ifTrue, ifFalse));
		var boolTree = bools.Factory.Map(boolConditional);
		var boolEdited = boolTree.Clone();
		boolEdited.AddValue(bools.GetConstant(true));
		Action boolAct = () => bools.FixHierarchy(boolEdited);
		boolAct.Should().Throw<NotSupportedException>();
		boolTree.Recycle();
	}
}
