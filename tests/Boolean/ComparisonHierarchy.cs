using Open.Evaluation.Arithmetic;
using Open.Evaluation.Boolean;
using Open.Hierarchy;

namespace Open.Evaluation.Tests.Boolean;

// GreaterThan/LessThan are OperatorBase<IEvaluate<T>, bool>: the child type and the operator's own
// result type are independent, so -- unlike Conditional, which had to hand-roll IParent<IEvaluate<T>>
// -- the untyped and typed IParent views come for free from OperatorBase. This pins that a
// comparison is visible to untyped traversal but, correctly, never to a same-typed walk of the tree
// it lives in (its own children are a DIFFERENT T than that tree's).
[TestClass]
public class ComparisonHierarchy
{
	[TestMethod]
	public void UntypedParent_ExposesBothOperands()
	{
		using var doubles = new EvaluationCatalog<double>();
		using var bools = new EvaluationCatalog<bool>();
		var a = doubles.GetParameter(0);
		var b = doubles.GetParameter(1);
		var greaterThan = bools.GreaterThan((a, b));

		((IParent)greaterThan).Children.Should().HaveCount(2);
	}

	[TestMethod]
	public void TypedParent_HoldsBothOperands()
	{
		using var doubles = new EvaluationCatalog<double>();
		using var bools = new EvaluationCatalog<bool>();
		var a = doubles.GetParameter(0);
		var b = doubles.GetParameter(1);
		var greaterThan = bools.GreaterThan((a, b));

		((IParent<IEvaluate<double>>)greaterThan).Children.Should().Equal(a, b);
	}

	[TestMethod]
	public void MappedAsABoolTree_TheComparisonHasNoTypedChildren()
	{
		// A GreaterThan<double> implements IParent<IEvaluate<double>>, never
		// IParent<IEvaluate<bool>>, so Node<IEvaluate<bool>>.EnsureChildrenMapped -- which only
		// expands a value that is IParent<IEvaluate<bool>> -- treats it as a leaf here.
		using var doubles = new EvaluationCatalog<double>();
		using var bools = new EvaluationCatalog<bool>();
		var a = doubles.GetParameter(0);
		var b = doubles.GetParameter(1);
		var greaterThan = bools.GreaterThan((a, b));

		var node = bools.Factory.Map(greaterThan);
		node.Children.Should().BeEmpty();
	}

	[TestMethod]
	public void FixHierarchy_NumericConditional_KeepsAComparisonConditionThroughATypedRebuild()
	{
		using var doubles = new EvaluationCatalog<double>();
		using var bools = new EvaluationCatalog<bool>();
		var p = doubles.GetParameter(0);
		var q = doubles.GetParameter(1);
		var a = doubles.GetParameter(2);
		var b = doubles.GetParameter(3);
		var c = doubles.GetParameter(4);
		var condition = bools.GreaterThan((p, q));
		var conditional = doubles.Conditional((condition, a, b));

		var tree = doubles.Factory.Map(conditional);
		tree.Children.Should().HaveCount(2, "only the branches are typed children of a double conditional");

		var edited = tree.Clone();
		var target = edited.Children.Cast<Node<IEvaluate<double>>>().First(n => n.Value == b);
		edited.Replace(target, edited.Source.Map(c));
		var rebuilt = doubles.FixHierarchy(edited).Recycle();
		rebuilt.Description.Value.Should().Be("(({0} > {1}) ? {2} : {4})", "the comparison condition is recovered from the untyped view");
		tree.Recycle();
	}

	[TestMethod]
	public void FixHierarchy_BoolTree_RoundTripsAnAndOfAComparisonAndANot()
	{
		using var doubles = new EvaluationCatalog<double>();
		using var bools = new EvaluationCatalog<bool>();
		var a = doubles.GetParameter(0);
		var b = doubles.GetParameter(1);
		var c = bools.GetParameter(0);
		var expression = bools.And([bools.GreaterThan((a, b)), bools.Not(c)]);

		var tree = bools.Factory.Map(expression);

		// Unchanged tree rebuilds to the same interned instance -- the comparison, whose own
		// children are a DIFFERENT T (double, not bool), survives the round trip via the untyped
		// pass-through path (see MappedAsABoolTree_TheComparisonHasNoTypedChildren above).
		bools.FixHierarchy(tree.Clone()).Recycle().Should().BeSameAs(expression);
	}
}
