using System.Diagnostics.CodeAnalysis;
using Open.Evaluation.Arithmetic;
using Open.Hierarchy;

namespace Open.Evaluation.Tests.Arithmetic;

/// <summary>
/// Covers the deterministic node-multiple helpers in
/// Open.Evaluation.Arithmetic.CatalogExtensions (Multiply.cs) used by the mutation/variation
/// machinery: MultiplyNode, GetMultiple, and AdjustNodeMultiple.
/// </summary>
[TestClass]
public class Multiply
{
	[TestMethod]
	public void MultiplyNode_WithIdentityMultiple_ReturnsUnchanged()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		var node = catalog.Factory.Map(p0);
		try
		{
			var result = catalog.MultiplyNode(node, 1d);
			ReferenceEquals(result, p0).Should().BeTrue();
		}
		finally
		{
			node.Recycle();
		}
	}

	[TestMethod]
	public void MultiplyNode_WithZeroMultiple_ReturnsZeroConstant()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		var node = catalog.Factory.Map(p0);
		try
		{
			var result = catalog.MultiplyNode(node, 0d);
			result.Should().BeOfType<Constant<double>>();
			((Constant<double>)result).Value.Should().Be(0d);
		}
		finally
		{
			node.Recycle();
		}
	}

	[TestMethod]
	public void MultiplyNode_OnNonProduct_WrapsInProductWithMultiple()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		var node = catalog.Factory.Map(p0);
		try
		{
			var result = catalog.MultiplyNode(node, 3d);
			result.Description.Value.Should().Be("(3 * {0})");
		}
		finally
		{
			node.Recycle();
		}
	}

	[TestMethod]
	public void MultiplyNode_OnProductWithExistingConstant_MultipliesThatConstant()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		var product = catalog.ProductOf(catalog.GetConstant(2d), p0);
		product.Description.Value.Should().Be("(2 * {0})");

		var node = catalog.Factory.Map(product);
		try
		{
			var result = catalog.MultiplyNode(node, 3d);
			result.Description.Value.Should().Be("(6 * {0})");
		}
		finally
		{
			node.Recycle();
		}
	}

	[TestMethod]
	public void MultiplyNode_OnProductWithoutConstant_AddsOneAsNewConstantChild()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		var p1 = catalog.GetParameter(1);
		var product = catalog.ProductOf(p0, p1);
		product.Description.Value.Should().Be("({0} * {1})");

		var node = catalog.Factory.Map(product);
		try
		{
			var result = catalog.MultiplyNode(node, 5d);
			// Product sorts constants to the front (ConstantPriority = -1).
			result.Description.Value.Should().Be("(5 * {0} * {1})");
		}
		finally
		{
			node.Recycle();
		}
	}

	[TestMethod]
	public void GetMultiple_OnProductWithConstant_ReturnsThatConstant()
	{
		using var catalog = new EvaluationCatalog<double>();
		var product = catalog.ProductOf(catalog.GetConstant(4d), catalog.GetParameter(0));

		var multiple = catalog.GetMultiple(product);
		multiple.Value.Should().Be(4d);
	}

	[TestMethod]
	public void GetMultiple_OnNonParentNode_ReturnsMultiplicativeIdentity()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);

		var multiple = catalog.GetMultiple(p0);
		multiple.Value.Should().Be(1d);
	}

	[TestMethod]
	public void GetMultiple_OnProductWithoutConstant_ReturnsMultiplicativeIdentity()
	{
		using var catalog = new EvaluationCatalog<double>();
		var product = catalog.ProductOf(catalog.GetParameter(0), catalog.GetParameter(1));

		var multiple = catalog.GetMultiple(product);
		multiple.Value.Should().Be(1d);
	}

	[TestMethod]
	public void AdjustNodeMultiple_WithZeroDelta_ReturnsUnchanged()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		var node = catalog.Factory.Map(p0);
		try
		{
			var result = catalog.AdjustNodeMultiple(node, 0d);
			ReferenceEquals(result, p0).Should().BeTrue();
		}
		finally
		{
			node.Recycle();
		}
	}

	[TestMethod]
	public void AdjustNodeMultiple_OnProductWithExistingMultiple_AddsDeltaToConstant()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		var product = catalog.ProductOf(catalog.GetConstant(4d), p0);

		var node = catalog.Factory.Map(product);
		try
		{
			var result = catalog.AdjustNodeMultiple(node, 3d);
			result.Description.Value.Should().Be("(7 * {0})");
		}
		finally
		{
			node.Recycle();
		}
	}

	[TestMethod]
	public void AdjustNodeMultiple_OnNonProduct_BehavesLikeMultiplyByDeltaPlusOne()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p0 = catalog.GetParameter(0);
		var node = catalog.Factory.Map(p0);
		try
		{
			// delta=2 on a bare parameter (not a Product) => MultiplyNode(delta + 1 = 3).
			var result = catalog.AdjustNodeMultiple(node, 2d);
			result.Description.Value.Should().Be("(3 * {0})");
		}
		finally
		{
			node.Recycle();
		}
	}

	// AdjustNodeMultiple used `multiple.Value switch { 1 => ..., _ => ... }`, a
	// constant pattern that the compiler lowers to boxed object.Equals against a boxed
	// System.Int32(1) -- which never equals a boxed System.Double(1.0) (or any other T). So for
	// T=double the switch always took the "_" branch, even when the combined constant multiple
	// genuinely equalled the multiplicative identity -- most commonly a Product with zero
	// constant children at all (e.g. a duplicate-parameter product like {1}*{1}). That branch
	// then indexed constantNodes[0] with an empty array, throwing IndexOutOfRangeException.
	// Fixed by comparing against T.MultiplicativeIdentity directly, mirroring MultiplyNode's own
	// idiom earlier in this file.
	[TestMethod]
	public void AdjustNodeMultiple_OnProductWithNoConstantChildren_DoesNotThrow()
	{
		using var catalog = new EvaluationCatalog<double>();
		var p1 = catalog.GetParameter(1);
		var dup = catalog.ProductOf(p1, p1);

		var node = catalog.Factory.Map(dup);
		try
		{
			IEvaluate<double>? result = null;
			Action act = () => result = catalog.AdjustNodeMultiple(node, 1d);

			act.Should().NotThrow();
			result!.Description.Value.Should().Be("(2 * {1} * {1})");
		}
		finally
		{
			node.Recycle();
		}
	}

	// Bounded port of the plan's stress re-run (same seed/shape generator that found 323
	// crashes pre-fix with 2000 trials): builds random Sum/Product trees over a few parameters
	// and constants, then repeatedly calls AdjustNodeMultiple on a random descendant. Trimmed to
	// 300 trials to stay cheap while still exercising the no-constant-children path many times.
	[TestMethod]
	[SuppressMessage("Design", "CA1031:Do not catch general exception types",
		Justification = "Deliberately catching any exception to count crashes across a randomized stress run.")]
	public void AdjustNodeMultiple_RandomTreesRepeatedAdjustment_NeverThrows()
	{
		var rnd = new Random(999);
		int crashes = 0;

		for (int trial = 0; trial < 300; trial++)
		{
			using var catalog = new EvaluationCatalog<double>();
			int nParams = rnd.Next(1, 4);
			var parts = Enumerable.Range(0, nParams).Select(i => (IEvaluate<double>)catalog.GetParameter(i)).ToArray();

			IEvaluate<double> BuildRandom(int depth)
			{
				if (depth <= 0 || rnd.Next(3) == 0)
					return rnd.Next(2) == 0
						? parts[rnd.Next(parts.Length)]
						: catalog.GetConstant(rnd.Next(1, 6));

				int nChildren = rnd.Next(2, 4);
				var children = Enumerable.Range(0, nChildren).Select(_ => BuildRandom(depth - 1)).ToArray();
				return rnd.Next(2) == 0
					? catalog.SumOf(children)
					: catalog.ProductOf(children);
			}

			IEvaluate<double> root = BuildRandom(3);

			for (int gen = 0; gen < 10; gen++)
			{
				var tree = catalog.Factory.Map(root);
				var descendants = tree.GetDescendantsOfType().ToArray();
				if (descendants.Length == 0) { tree.Recycle(); break; }
				var target = descendants[rnd.Next(descendants.Length)];
				double delta = rnd.Next(2) == 0 ? -1d : 1d;
				try
				{
					root = catalog.AdjustNodeMultiple(target, delta);
				}
				catch (Exception)
				{
					crashes++;
					tree.Recycle();
					goto nextTrial;
				}
				tree.Recycle();
			}
			nextTrial: ;
		}

		crashes.Should().Be(0);
	}
}
