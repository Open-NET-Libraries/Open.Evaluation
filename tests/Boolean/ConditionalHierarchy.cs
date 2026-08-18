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
		rebuilt.Description.Value.Should().Be("{0} ? {2} : {3}");
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
		rebuilt.Description.Value.Should().Be("{0} ? {1} : {3}", "the condition is recovered from the untyped view");
		tree.Recycle();
	}
}
