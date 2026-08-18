using System.Collections.Immutable;
using Open.Evaluation.Arithmetic;
using Open.Evaluation.Boolean;

namespace Open.Evaluation.Tests.Core;

/// <summary>
/// Pins the public immutability surface: <see cref="OperatorBase{TChild, T}.Children"/> is a true
/// value-type <see cref="ImmutableArray{T}"/> (no in-place mutation path), and reproduction-style
/// APIs that look "mutating" by name always return a NEW node while leaving the original - and its
/// evaluation result - completely untouched.
/// </summary>
[TestClass]
public class ImmutabilityTests
{
	[TestMethod]
	public void Children_IsImmutableArray_MutatingMethodsReturnNewCollection_OriginalUnaffected()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		var p1 = catalog.GetParameter(1);
		var sum = (Sum<double>)catalog.SumOf(p0, p1);

		// Compile-time pin: Children's static type IS System.Collections.Immutable.ImmutableArray<T>.
		ImmutableArray<IEvaluate<double>> children = sum.Children;
		var originalLength = children.Length;

		var extra = catalog.GetParameter(2);
		var appended = children.Add(extra); // ImmutableArray<T>.Add returns a NEW array by value.

		children.Length.Should().Be(originalLength,
			"ImmutableArray<T> has value semantics: .Add must not mutate the source array");
		sum.Children.Length.Should().Be(originalLength,
			"the operator's own Children must remain unaffected by operations on a copy");
		appended.Length.Should().Be(originalLength + 1);
	}

	[TestMethod]
	public void NewWithIndexReplaced_OnSum_ReturnsNewExpression_OriginalStillEvaluatesUnchanged()
	{
		// Regression repro: OperatorBase<T>.ConditionalTransform built its replacement
		// children via a growable ImmutableArray.Builder and then called MoveToImmutable(), which
		// requires Count==Capacity exactly and so threw for any 2+-child transform. Fixed by
		// switching to DrainToImmutable() - see OperatorBase.cs.
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		var p1 = catalog.GetParameter(1);
		var sum = (Sum<double>)catalog.SumOf(p0, p1);
		var replacement = catalog.GetConstant(100d);

		var modified = sum.NewWithIndexReplaced<Sum<double>, IEvaluate<double>, IEvaluate<double>>(catalog, 1, replacement);

		using var lease = Context.Rent();
		var context = lease.Item.Init(catalog, (ReadOnlySpan<double>)[2d, 3d]);

		sum.Evaluate(context).Result.Should().Be(5d, "the original node must evaluate exactly as before");
		modified.Evaluate(context).Result.Should().Be(102d, "the new node reflects the replacement");
		ReferenceEquals(modified, sum).Should().BeFalse();
		((Sum<double>)modified).Children.Length.Should().Be(2);
		sum.Children.Length.Should().Be(2, "the original's own Children must be untouched");
	}

	[TestMethod]
	public void NewWithAppended_OnSum_ReturnsNewExpression_OriginalStillEvaluatesUnchanged()
	{
		// Same repro shape as NewWithIndexReplaced above (ConditionalTransform via
		// DrainToImmutable), but for the append path: a 3-child Sum<double> growing to 4 children.
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		var p1 = catalog.GetParameter(1);
		var p2 = catalog.GetParameter(2);
		var sum = (Sum<double>)catalog.SumOf(p0, p1, p2);
		var extra = catalog.GetConstant(100d);

		var modified = sum.NewWithAppended<Sum<double>, IEvaluate<double>, IEvaluate<double>>(catalog, extra);

		using var lease = Context.Rent();
		var context = lease.Item.Init(catalog, (ReadOnlySpan<double>)[2d, 3d, 4d]);

		sum.Evaluate(context).Result.Should().Be(9d, "the original node must evaluate exactly as before");
		modified.Evaluate(context).Result.Should().Be(109d, "the new node includes the appended child");
		ReferenceEquals(modified, sum).Should().BeFalse();
		((Sum<double>)modified).Children.Length.Should().Be(4);
		sum.Children.Length.Should().Be(3, "the original's own Children must be untouched");
	}

	[TestMethod]
	public void NewWithIndexReplaced_OnAnd_ReturnsNewExpression_OriginalStillEvaluatesUnchanged()
	{
		// Uses And (Boolean) rather than Sum<double>/Product<double>: And.NewUsing constructs
		// directly instead of delegating to OperatorBase<T>.ConditionalTransform, so this proves the
		// SAME ReproductionExtensions contract (new node, original untouched) independently of the
		// ConditionalTransform path exercised by the Sum<double> tests above.
		using var catalog = new EvaluationCatalog<bool>();
		var p0 = catalog.GetParameter(0);
		var p1 = catalog.GetParameter(1);
		var and = catalog.And([p0, p1]);
		var replacement = catalog.GetParameter(9);

		var modified = and.NewWithIndexReplaced<And, IEvaluate<bool>, IEvaluate<bool>>(catalog, 1, replacement);

		using var lease = Context.Rent();
		var context = lease.Item;
		context.AddParam(catalog, 0, true);
		context.AddParam(catalog, 1, false);
		context.AddParam(catalog, 9, true); // the replacement parameter's value.

		and.Evaluate(context).Result.Should().BeFalse("original: true AND false");
		modified.Evaluate(context).Result.Should().BeTrue("modified: true AND (replacement=true)");
		ReferenceEquals(modified, and).Should().BeFalse();
		((And)modified).Children.Length.Should().Be(2);
		and.Children.Length.Should().Be(2, "the original's own Children must be untouched");
	}

	[TestMethod]
	public void TryAddConstant_ReturnsNewExpression_OriginalStillEvaluatesUnchanged()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		var p1 = catalog.GetParameter(1);
		var sum = catalog.SumOf(p0, p1);

		var node = catalog.Factory.Map(sum);
		try
		{
			var withConstant = catalog.TryAddConstant(node, 100d);
			withConstant.Should().NotBeNull();

			using var lease = Context.Rent();
			var context = lease.Item.Init(catalog, (ReadOnlySpan<double>)[2d, 3d]);

			sum.Evaluate(context).Result.Should().Be(5d, "the original node must evaluate exactly as before");
			withConstant!.Evaluate(context).Result.Should().Be(105d, "the new node includes the added constant");
			ReferenceEquals(withConstant, sum).Should().BeFalse();
			((Sum<double>)sum).Children.Length.Should().Be(2, "the original's own Children must be untouched");
		}
		finally
		{
			node.Recycle();
		}
	}

	[TestMethod]
	public void RemoveNode_ReturnsNewTree_OriginalExpressionInstanceStillEvaluatesUnchanged()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		var p1 = catalog.GetParameter(1);
		var p2 = catalog.GetParameter(2);
		var sum = catalog.SumOf(p0, p1, p2);

		var node = catalog.Factory.Map(sum);
		try
		{
			var middleChild = node.Children[1];
			var fixedRoot = catalog.RemoveNode(middleChild);
			try
			{
				using var lease = Context.Rent();
				var context = lease.Item.Init(catalog, (ReadOnlySpan<double>)[2d, 3d, 4d]);

				// The ORIGINAL `sum` instance (obtained before the Node-tree surgery) must still
				// evaluate using all three of its original children.
				sum.Evaluate(context).Result.Should().Be(9d, "2 + 3 + 4");
				fixedRoot.Value!.Evaluate(context).Result.Should().Be(6d, "2 + 4, with the middle child removed");
			}
			finally
			{
				if (fixedRoot != node) fixedRoot.Recycle();
			}
		}
		finally
		{
			node.Recycle();
		}
	}
}
