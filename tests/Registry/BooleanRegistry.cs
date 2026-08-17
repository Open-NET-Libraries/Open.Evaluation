using Microsoft.VisualStudio.TestTools.UnitTesting;
using Open.Evaluation.Boolean;
using Open.Evaluation.Catalogs;
using Open.Evaluation.Core;
using System;
using System.Linq;

namespace Open.Evaluation.Tests.Registry;

/// <summary>
/// Regression coverage for <see cref="Open.Evaluation.Registry.Boolean.GetOperator"/>.
/// Prior to 1.1.3 the AND/OR cases were wired to the arithmetic <c>SumOf</c>/<c>ProductOf</c>
/// catalog extensions, producing <c>Sum&lt;bool&gt;</c>/<c>Product&lt;bool&gt;</c> nodes whose
/// <c>dynamic</c> arithmetic throws at evaluation time for any multi-parameter boolean
/// expression. These tests build operators through the registry (the path genetic-programming
/// consumers use) and evaluate them, which is exactly what used to throw.
/// </summary>
[TestClass]
public class BooleanRegistry
{
	private static IEvaluate<bool> Build(EvaluationCatalog<bool> catalog, char op)
		=> Open.Evaluation.Registry.Boolean.GetOperator(
			catalog, op, [catalog.GetParameter<bool>(0), catalog.GetParameter<bool>(1)]);

	[TestMethod]
	public void GetOperator_And_ProducesAndNode()
	{
		using var catalog = new EvaluationCatalog<bool>();
		var node = Build(catalog, Open.Evaluation.Registry.Boolean.AND);
		Assert.IsInstanceOfType(node, typeof(And));
	}

	[TestMethod]
	public void GetOperator_Or_ProducesOrNode()
	{
		using var catalog = new EvaluationCatalog<bool>();
		var node = Build(catalog, Open.Evaluation.Registry.Boolean.OR);
		Assert.IsInstanceOfType(node, typeof(Or));
	}

	[DataTestMethod]
	[DataRow(false, false, false)]
	[DataRow(false, true, false)]
	[DataRow(true, false, false)]
	[DataRow(true, true, true)]
	public void GetOperator_And_EvaluatesTruthTable(bool a, bool b, bool expected)
	{
		using var catalog = new EvaluationCatalog<bool>();
		var node = Build(catalog, Open.Evaluation.Registry.Boolean.AND);
		// Evaluating a two-parameter boolean operator built via the registry: this threw
		// RuntimeBinderException before 1.1.3.
		Assert.AreEqual(expected, node.Evaluate(new[] { a, b }));
	}

	[DataTestMethod]
	[DataRow(false, false, false)]
	[DataRow(false, true, true)]
	[DataRow(true, false, true)]
	[DataRow(true, true, true)]
	public void GetOperator_Or_EvaluatesTruthTable(bool a, bool b, bool expected)
	{
		using var catalog = new EvaluationCatalog<bool>();
		var node = Build(catalog, Open.Evaluation.Registry.Boolean.OR);
		Assert.AreEqual(expected, node.Evaluate(new[] { a, b }));
	}

	[TestMethod]
	public void GetOperator_UnknownSymbol_Throws()
	{
		using var catalog = new EvaluationCatalog<bool>();
		Assert.ThrowsException<ArgumentException>(() => Build(catalog, '+'));
	}

	[TestMethod]
	public void GetRandomOperator_AlwaysYieldsEvaluableNode()
	{
		using var catalog = new EvaluationCatalog<bool>();
		var children = new IEvaluate<bool>[] { catalog.GetParameter<bool>(0), catalog.GetParameter<bool>(1) };
		for (int i = 0; i < 50; i++)
		{
			var node = Open.Evaluation.Registry.Boolean.GetRandomOperator(catalog, children);
			Assert.IsNotNull(node);
			// Must not throw for either operator.
			_ = node.Evaluate(new[] { true, false });
		}
	}

	[TestMethod]
	public void Operators_RegistryListsExactlyAndOr()
	{
		CollectionAssert.AreEquivalent(
			new[] { And.SYMBOL, Or.SYMBOL },
			Open.Evaluation.Registry.Boolean.Operators.ToArray());
	}
}
