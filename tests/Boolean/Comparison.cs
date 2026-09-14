using Open.Evaluation.Arithmetic;
using Open.Evaluation.Boolean;

namespace Open.Evaluation.Tests.Boolean;

[TestClass]
public class Comparison
{
	[TestMethod]
	public void GreaterThan_EvaluatesTrue_WhenLeftIsGreater()
	{
		var doubles = new EvaluationCatalog<double>();
		var bools = new EvaluationCatalog<bool>();
		var greaterThan = bools.GreaterThan((doubles.GetParameter(0), doubles.GetParameter(1)));

		using var lease = Context.Rent();
		var context = lease.Item;
		context.AddParam(doubles, 0, 2d);
		context.AddParam(doubles, 1, 1d);
		greaterThan.Evaluate(context).Result.Should().BeTrue("2 is greater than 1");
	}

	[TestMethod]
	public void GreaterThan_EvaluatesFalse_WhenLeftIsLess()
	{
		var doubles = new EvaluationCatalog<double>();
		var bools = new EvaluationCatalog<bool>();
		var greaterThan = bools.GreaterThan((doubles.GetParameter(0), doubles.GetParameter(1)));

		using var lease = Context.Rent();
		var context = lease.Item;
		context.AddParam(doubles, 0, 1d);
		context.AddParam(doubles, 1, 2d);
		greaterThan.Evaluate(context).Result.Should().BeFalse("1 is not greater than 2");
	}

	[TestMethod]
	public void GreaterThan_EvaluatesFalse_WhenEqual()
	{
		var doubles = new EvaluationCatalog<double>();
		var bools = new EvaluationCatalog<bool>();
		var greaterThan = bools.GreaterThan((doubles.GetParameter(0), doubles.GetParameter(1)));

		using var lease = Context.Rent();
		var context = lease.Item;
		context.AddParam(doubles, 0, 1d);
		context.AddParam(doubles, 1, 1d);
		greaterThan.Evaluate(context).Result.Should().BeFalse("strictly greater excludes equality");
	}

	[TestMethod]
	public void LessThan_EvaluatesTrue_WhenLeftIsLess()
	{
		var doubles = new EvaluationCatalog<double>();
		var bools = new EvaluationCatalog<bool>();
		var lessThan = bools.LessThan((doubles.GetParameter(0), doubles.GetParameter(1)));

		using var lease = Context.Rent();
		var context = lease.Item;
		context.AddParam(doubles, 0, 1d);
		context.AddParam(doubles, 1, 2d);
		lessThan.Evaluate(context).Result.Should().BeTrue("1 is less than 2");
	}

	[TestMethod]
	public void LessThan_EvaluatesFalse_WhenLeftIsGreater()
	{
		var doubles = new EvaluationCatalog<double>();
		var bools = new EvaluationCatalog<bool>();
		var lessThan = bools.LessThan((doubles.GetParameter(0), doubles.GetParameter(1)));

		using var lease = Context.Rent();
		var context = lease.Item;
		context.AddParam(doubles, 0, 2d);
		context.AddParam(doubles, 1, 1d);
		lessThan.Evaluate(context).Result.Should().BeFalse("2 is not less than 1");
	}

	[TestMethod]
	public void LessThan_EvaluatesFalse_WhenEqual()
	{
		var doubles = new EvaluationCatalog<double>();
		var bools = new EvaluationCatalog<bool>();
		var lessThan = bools.LessThan((doubles.GetParameter(0), doubles.GetParameter(1)));

		using var lease = Context.Rent();
		var context = lease.Item;
		context.AddParam(doubles, 0, 1d);
		context.AddParam(doubles, 1, 1d);
		lessThan.Evaluate(context).Result.Should().BeFalse("strictly less excludes equality");
	}

	[TestMethod]
	public void GreaterThan_WorksForAnIntegerOperandType()
	{
		var ints = new EvaluationCatalog<int>();
		var bools = new EvaluationCatalog<bool>();
		var greaterThan = bools.GreaterThan((ints.GetParameter(0), ints.GetParameter(1)));

		using var lease = Context.Rent();
		var context = lease.Item;
		context.AddParam(ints, 0, 5);
		context.AddParam(ints, 1, 3);
		greaterThan.Evaluate(context).Result.Should().BeTrue();
	}

	[TestMethod]
	public void GreaterThan_IsFalse_WhenEitherOperandIsNaN()
	{
		var doubles = new EvaluationCatalog<double>();
		var bools = new EvaluationCatalog<bool>();
		var greaterThan = bools.GreaterThan((doubles.GetParameter(0), doubles.GetParameter(1)));

		using (var lease = Context.Rent())
		{
			var context = lease.Item;
			context.AddParam(doubles, 0, double.NaN);
			context.AddParam(doubles, 1, 1d);
			greaterThan.Evaluate(context).Result.Should().BeFalse("IEEE: any comparison with NaN is false");
		}

		using (var lease = Context.Rent())
		{
			var context = lease.Item;
			context.AddParam(doubles, 0, 1d);
			context.AddParam(doubles, 1, double.NaN);
			greaterThan.Evaluate(context).Result.Should().BeFalse("IEEE: any comparison with NaN is false");
		}
	}

	[TestMethod]
	public void LessThan_IsFalse_WhenEitherOperandIsNaN()
	{
		var doubles = new EvaluationCatalog<double>();
		var bools = new EvaluationCatalog<bool>();
		var lessThan = bools.LessThan((doubles.GetParameter(0), doubles.GetParameter(1)));

		using (var lease = Context.Rent())
		{
			var context = lease.Item;
			context.AddParam(doubles, 0, double.NaN);
			context.AddParam(doubles, 1, 1d);
			lessThan.Evaluate(context).Result.Should().BeFalse("IEEE: any comparison with NaN is false");
		}

		using (var lease = Context.Rent())
		{
			var context = lease.Item;
			context.AddParam(doubles, 0, 1d);
			context.AddParam(doubles, 1, double.NaN);
			lessThan.Evaluate(context).Result.Should().BeFalse("IEEE: any comparison with NaN is false");
		}
	}

	[TestMethod]
	public void Description_FormatsAsInfixComparison()
	{
		var doubles = new EvaluationCatalog<double>();
		var bools = new EvaluationCatalog<bool>();
		var left = doubles.GetParameter(0);
		var right = doubles.GetParameter(1);

		bools.GreaterThan((left, right)).Description.Value.Should().Be("({0} > {1})");
		bools.LessThan((left, right)).Description.Value.Should().Be("({0} < {1})");
	}

	[TestMethod]
	public void Description_DoesNotCollideWithOtherTwoChildRenderings()
	{
		var doubles = new EvaluationCatalog<double>();
		var bools = new EvaluationCatalog<bool>();
		var dLeft = doubles.GetParameter(0);
		var dRight = doubles.GetParameter(1);
		var bLeft = bools.GetParameter(0);
		var bRight = bools.GetParameter(1);

		// All four end up interned in the SAME bool catalog: a genuine collision (two different
		// node types rendering to the same key) would throw from Catalog.Register, not just
		// compare equal below.
		var greaterThan = bools.GreaterThan((dLeft, dRight));
		var lessThan = bools.LessThan((dLeft, dRight));
		var and = bools.And([bLeft, bRight]);
		var or = bools.Or([bLeft, bRight]);

		new[]
		{
			greaterThan.Description.Value,
			lessThan.Description.Value,
			and.Description.Value,
			or.Description.Value,
		}.Should().OnlyHaveUniqueItems("no two-child rendering of the same-labeled operands may collide in the catalog");
	}

	[TestMethod]
	public void Interning_StructurallyIdentical_ReturnsSameInstance()
	{
		var doubles = new EvaluationCatalog<double>();
		var bools = new EvaluationCatalog<bool>();

		var a = bools.GreaterThan((doubles.GetParameter(0), doubles.GetParameter(1)));
		var b = bools.GreaterThan((doubles.GetParameter(0), doubles.GetParameter(1)));

		ReferenceEquals(a, b).Should().BeTrue();
	}

	[TestMethod]
	public void Interning_OperandOrderMatters_DistinctInstances()
	{
		var doubles = new EvaluationCatalog<double>();
		var bools = new EvaluationCatalog<bool>();
		var p0 = doubles.GetParameter(0);
		var p1 = doubles.GetParameter(1);

		var ab = bools.GreaterThan((p0, p1));
		var ba = bools.GreaterThan((p1, p0));

		ReferenceEquals(ab, ba).Should().BeFalse("operand order is load-bearing: a > b is not b > a");
	}

	[TestMethod]
	public void MotivatingCase_ConditionalOnComparison_SelectsTheLargerSquare()
	{
		// Conditional<double>((a > b), a*a, b*b): the max(a, b)^2 shape that a comparison makes
		// expressible for the first time -- there was previously no way to route a numeric
		// relationship into a boolean condition.
		var doubles = new EvaluationCatalog<double>();
		var bools = new EvaluationCatalog<bool>();
		var a = doubles.GetParameter(0);
		var b = doubles.GetParameter(1);
		var expression = doubles.Conditional((
			bools.GreaterThan((a, b)),
			doubles.ProductOf(a, a),
			doubles.ProductOf(b, b)));

		using (var lease = Context.Rent())
		{
			var context = lease.Item;
			context.AddParam(doubles, 0, 3d);
			context.AddParam(doubles, 1, 5d);
			expression.Evaluate(context).Result.Should().Be(25d, "b is larger, so b*b wins");
		}

		using (var lease = Context.Rent())
		{
			var context = lease.Item;
			context.AddParam(doubles, 0, 5d);
			context.AddParam(doubles, 1, 3d);
			expression.Evaluate(context).Result.Should().Be(25d, "a is larger, so a*a wins");
		}
	}
}
