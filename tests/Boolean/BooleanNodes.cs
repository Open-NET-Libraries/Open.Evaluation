using Microsoft.VisualStudio.TestTools.UnitTesting;
using Open.Evaluation.Boolean;
using Open.Evaluation.Catalogs;
using Open.Evaluation.Core;
using System.Collections.Generic;

namespace Open.Evaluation.Tests.Boolean;

/// <summary>
/// Evaluation coverage for the boolean node types. Prior to 1.1.3 none of these had tests;
/// <c>Or</c> used a predicate-less <c>.Any()</c> (true for any non-empty child set).
/// </summary>
[TestClass]
public class BooleanNodes
{
	private static IEvaluate<bool>[] Params(EvaluationCatalog<bool> catalog, int count)
	{
		var result = new IEvaluate<bool>[count];
		for (ushort i = 0; i < count; i++) result[i] = catalog.GetParameter<bool>(i);
		return result;
	}

	[DataTestMethod]
	[DataRow(false, true)]
	[DataRow(true, false)]
	public void Not_Inverts(bool input, bool expected)
	{
		using var catalog = new EvaluationCatalog<bool>();
		var node = catalog.Not(catalog.GetParameter<bool>(0));
		Assert.AreEqual(expected, node.Evaluate(new[] { input }));
	}

	[DataTestMethod]
	[DataRow(false, false, false, false)]
	[DataRow(true, false, false, true)]
	[DataRow(false, true, false, true)]
	[DataRow(false, false, true, true)]
	[DataRow(true, true, true, true)]
	public void Or_ThreeChildren_IsTrueIffAnyTrue(bool a, bool b, bool c, bool expected)
	{
		using var catalog = new EvaluationCatalog<bool>();
		var node = catalog.Or(Params(catalog, 3));
		// Regression: pre-1.1.3 Or used .Any() with no predicate, so (false,false,false) => true.
		Assert.AreEqual(expected, node.Evaluate(new[] { a, b, c }));
	}

	[DataTestMethod]
	[DataRow(false, false, false, false)]
	[DataRow(true, true, false, false)]
	[DataRow(true, true, true, true)]
	public void And_ThreeChildren_IsTrueIffAllTrue(bool a, bool b, bool c, bool expected)
	{
		using var catalog = new EvaluationCatalog<bool>();
		var node = catalog.And(Params(catalog, 3));
		Assert.AreEqual(expected, node.Evaluate(new[] { a, b, c }));
	}

	// ---- Counting ----

	[TestMethod]
	public void AtLeast_ZeroCount_IsRejectedAtConstruction()
	{
		// AtLeast tightens CountingBase's ">= 0" rule to ">= 1": "at least zero" is a
		// degenerate, always-true predicate, so the node refuses to exist rather than
		// carrying an evaluation branch for it. (AtMost/Exactly legitimately accept 0.)
		using var catalog = new EvaluationCatalog<bool>();
		Assert.ThrowsException<System.ArgumentOutOfRangeException>(
			() => catalog.CountAtLeast((0, Params(catalog, 3))));
	}

	[DataTestMethod]
	[DataRow(1, false, false, false, false)]
	[DataRow(1, false, true, false, true)]
	[DataRow(2, true, false, false, false)]
	[DataRow(2, true, false, true, true)]
	[DataRow(3, true, true, false, false)]
	[DataRow(3, true, true, true, true)]
	[DataRow(4, true, true, true, false)]    // more than the child count: never satisfiable
	public void AtLeast_CountsTrueChildren(int count, bool a, bool b, bool c, bool expected)
	{
		using var catalog = new EvaluationCatalog<bool>();
		var node = catalog.CountAtLeast((count, Params(catalog, 3)));
		Assert.AreEqual(expected, node.Evaluate(new[] { a, b, c }));
	}

	[DataTestMethod]
	[DataRow(0, false, false, false, true)]
	[DataRow(0, true, false, false, false)]
	[DataRow(1, true, false, false, true)]
	[DataRow(1, true, true, false, false)]
	[DataRow(2, true, true, false, true)]
	[DataRow(2, true, true, true, false)]
	[DataRow(3, true, true, true, true)]
	public void AtMost_CountsTrueChildren(int count, bool a, bool b, bool c, bool expected)
	{
		using var catalog = new EvaluationCatalog<bool>();
		var node = catalog.CountAtMost((count, Params(catalog, 3)));
		Assert.AreEqual(expected, node.Evaluate(new[] { a, b, c }));
	}

	[DataTestMethod]
	[DataRow(0, false, false, false, true)]
	[DataRow(0, true, false, false, false)]
	[DataRow(1, true, false, false, true)]
	[DataRow(1, true, true, false, false)]
	[DataRow(2, true, true, false, true)]
	[DataRow(2, true, true, true, false)]
	[DataRow(3, true, true, true, true)]
	public void Exactly_CountsTrueChildren(int count, bool a, bool b, bool c, bool expected)
	{
		using var catalog = new EvaluationCatalog<bool>();
		var node = catalog.CountExactly((count, Params(catalog, 3)));
		Assert.AreEqual(expected, node.Evaluate(new[] { a, b, c }));
	}

	// ---- String rendering (doubles as identity/hash for genetic-programming consumers) ----

	[TestMethod]
	public void Not_RendersWithItsSymbol()
	{
		using var catalog = new EvaluationCatalog<bool>();
		var node = catalog.Not(catalog.GetParameter<bool>(0));
		// Regression: pre-1.1.3 the single-child Not rendered as "({0})" -- no '!' at all --
		// because OperatorBase only emits the symbol between children.
		Assert.AreEqual("(!{0})", node.ToStringRepresentation());
	}

	[TestMethod]
	public void Not_OfOperator_IsDistinguishableFromBareOperator()
	{
		using var catalog = new EvaluationCatalog<bool>();
		var and = catalog.And(Params(catalog, 2));
		var notAnd = catalog.Not(and);
		Assert.AreEqual("({0} & {1})", and.ToStringRepresentation());
		Assert.AreEqual("(!({0} & {1}))", notAnd.ToStringRepresentation());
		Assert.AreNotEqual(and.ToStringRepresentation(), notAnd.ToStringRepresentation());
	}
}
