using Open.Evaluation.Boolean;
using Open.Evaluation.Boolean.Counting;

namespace Open.Evaluation.Tests.Boolean;

[TestClass]
public class Counting
{
	static (EvaluationCatalog<bool> Catalog, IEvaluate<bool>[] Ps) Setup(int count)
	{
		var catalog = new EvaluationCatalog<bool>();
		var ps = Enumerable.Range(0, count).Select(i => (IEvaluate<bool>)catalog.GetParameter(i)).ToArray();
		return (catalog, ps);
	}

	static void Run(EvaluationCatalog<bool> catalog, IEvaluate<bool> e, bool[] values, bool expected)
	{
		using var lease = Context.Rent();
		var context = lease.Item.Init(catalog, (ReadOnlySpan<bool>)values);
		e.Evaluate(context).Result.Should().Be(expected);
	}

	[TestClass]
	public class AtLeastTests
	{
		[DataTestMethod]
		[DataRow(new[] { true, true, false }, 2, true)]
		[DataRow(new[] { true, false, false }, 2, false)]
		[DataRow(new[] { true, true, true }, 2, true)]
		[DataRow(new[] { false, false, false }, 1, false)]
		public void EvaluatesThreshold(bool[] values, int count, bool expected)
		{
			var (catalog, ps) = Setup(values.Length);
			var atLeast = catalog.CountAtLeast((count, ps));
			Run(catalog, atLeast, values, expected);
		}

		[TestMethod]
		public void CountZero_Throws()
		{
			var (catalog, ps) = Setup(2);
			Action act = () => catalog.CountAtLeast((0, ps));
			act.Should().Throw<ArgumentOutOfRangeException>();
		}

		[TestMethod]
		public void NegativeCount_Throws()
		{
			var (catalog, ps) = Setup(2);
			Action act = () => catalog.CountAtLeast((-1, ps));
			act.Should().Throw<Exception>();
		}

		[TestMethod]
		public void Description_IncludesPrefixAndCount()
		{
			var (catalog, ps) = Setup(2);
			var atLeast = catalog.CountAtLeast((1, ps));
			atLeast.Description.Value.Should().Be("AtLeast(1 from ({0}, {1}))");
		}
	}

	[TestClass]
	public class AtMostTests
	{
		[DataTestMethod]
		[DataRow(new[] { true, true, false }, 2, true)]
		[DataRow(new[] { true, true, true }, 2, false)]
		[DataRow(new[] { false, false, false }, 0, true)]
		[DataRow(new[] { true, false, false }, 0, false)]
		public void EvaluatesThreshold(bool[] values, int count, bool expected)
		{
			var (catalog, ps) = Setup(values.Length);
			var atMost = catalog.CountAtMost((count, ps));
			Run(catalog, atMost, values, expected);
		}

		[TestMethod]
		public void CountZero_IsAllowed()
		{
			var (catalog, ps) = Setup(2);
			var atMost = catalog.CountAtMost((0, ps));
			Run(catalog, atMost, [false, false], true);
		}

		[TestMethod]
		public void Description_IncludesPrefixAndCount()
		{
			var (catalog, ps) = Setup(2);
			var atMost = catalog.CountAtMost((1, ps));
			atMost.Description.Value.Should().Be("AtMost(1 from ({0}, {1}))");
		}
	}

	[TestClass]
	public class ExactlyTests
	{
		[DataTestMethod]
		[DataRow(new[] { true, true, false }, 2, true)]
		[DataRow(new[] { true, false, false }, 2, false)]
		[DataRow(new[] { true, true, true }, 2, false)]
		[DataRow(new[] { false, false, false }, 0, true)]
		public void EvaluatesThreshold(bool[] values, int count, bool expected)
		{
			var (catalog, ps) = Setup(values.Length);
			var exactly = catalog.CountExactly((count, ps));
			Run(catalog, exactly, values, expected);
		}

		[TestMethod]
		public void Description_IncludesPrefixAndCount()
		{
			var (catalog, ps) = Setup(2);
			var exactly = catalog.CountExactly((1, ps));
			exactly.Description.Value.Should().Be("Exactly(1 from ({0}, {1}))");
		}

		[TestMethod]
		public void ActsAsXor_ForTwoInputs()
		{
			// Exactly(1, a, b) is one way of expressing XOR.
			var (catalog, ps) = Setup(2);
			var xor = catalog.CountExactly((1, ps));

			Run(catalog, xor, [true, false], true);
			Run(catalog, xor, [false, true], true);
			Run(catalog, xor, [true, true], false);
			Run(catalog, xor, [false, false], false);
		}
	}
}
