using Open.Evaluation.Arithmetic;

namespace Open.Evaluation.Tests;

public static class Product
{
	[TestClass]
	public class Default : ParseTestBase
	{
		const string FORMAT = "(({0} + {1} + 2) * ({0} + {1}) * ({2} + {3}))";
		public Default() : base(FORMAT) { }

		protected override double Expected
		{
			get
			{
				var x1 = PV[0] + PV[1];
				var x2 = PV[2] + PV[3];
				var x3 = x1 + 2;
				return x1 * x2 * x3;
			}
		}
	}

	[TestClass]
	public class ConstantCollapse : ParseTestBase
	{
		const string FORMAT = "(({0} * {1}) + (7 * 5 * 2 * {0}) + ({2} * {3}))";
		const string REP = "(({0} * {1}) + ({2} * {3}) + (70 * {0}))";
		public ConstantCollapse() : base(FORMAT, REP) { }

		protected override double Expected
		{
			get
			{
				var x1 = PV[0] * PV[1];
				var x2 = 7 * 5 * 2 * PV[0];
				var x3 = PV[2] * PV[3];
				return x1 + x2 + x3;
			}
		}
	}

	[TestClass]
	public class ProductCollapse : ParseTestBase
	{
		const string FORMAT = "(({0} + {1}) * ({0} + {1}) * {2} * {2} * {3} * 2 * 1)";
		const string REP = "(2 * ({0} + {1}) * ({0} + {1}) * {2} * {2} * {3})";
		const string RED = "(2 * (({0} + {1})²) * ({2}²) * {3})";
		public ProductCollapse() : base(FORMAT, REP, RED) { }

		protected override double Expected
			=> Math.Pow(PV[0] + PV[1], 2)
			  * Math.Pow(PV[2], 2)
			  * PV[3]
			  * 2;
	}

	[TestClass]
	public class ZeroCollapse : ParseTestBase
	{
		const string FORMAT = "(({0} + {1}) * 0 * {2} * {2} * {3} * 2 * 1)";
		const string REP = "(0 * ({0} + {1}) * {2} * {2} * {3})";
		const string RED = "0";
		public ZeroCollapse() : base(FORMAT, REP, RED) { }

		protected override double Expected => 0;
	}

	[TestClass]
	public class ProductOfSums
	{
		[TestMethod]
		public void TwoSumTest()
		{
			using var catalog = new EvaluationCatalog<double>();

			var a = (Sum<double>)catalog.Parse("({0} + {1})");
			var b = (Sum<double>)catalog.Parse("({2} + {3})");

			Validate(catalog, catalog.ProductOfSums(a, b));
			Validate(catalog, catalog.ProductOfSums(new Sum<double>[] { a, b }));

			{
				var sample = catalog.Parse("(({0} + {1}) * ({2} + {3}))");
				var reduced = catalog.Variation.FlattenProductofSums(sample);
				Validate(catalog, reduced);
			}

			{
				var sample = catalog.Parse("(({0} + {1}) * ({2} + (({0} + {1}) * ({2} + {3}))))");
				Context.Use(context =>
				{
					ReadOnlySpan<double> p = [1, 2, 3, 4];
					context.Init(catalog, p);
					var v = sample.Evaluate(context);
					var vResult = v.Result;
					var vDesc = v.Description.Value;
					var reduced = catalog.Variation.FlattenProductofSums(sample);
					reduced.Description.Value
						.Should().Be("((({0}²) * {2}) + (({0}²) * {3}) + (({1}²) * {2}) + (({1}²) * {3}) + (2 * {0} * {1} * {2}) + (2 * {0} * {1} * {3}) + ({0} * {2}) + ({1} * {2}))");

					Context.Use(c2 =>
					{
						ReadOnlySpan<double> p = [1, 2, 3, 4];
						c2.Init(catalog, p);
						var e = reduced.Evaluate(c2);
						var eResult = e.Result;
						var eDesc = e.Description.Value;
						eResult
							.Should().Be(vResult, eDesc);
					});
				});

				// Verify that the context is not needed after evaluation.
				EvaluationResult<double> evaluated = default;
				using(var context = new Context())
				{
					ReadOnlySpan<double> p = [1, 2, 3, 4];
					context.Init(catalog, p);
					evaluated = sample.Evaluate(context);
				}

				evaluated.Result.Should().Be(72);
				evaluated.Description.Value.Should().Be("((((1 + 2) * (3 + 4)) + 3) * (1 + 2))");

			}
		}

		[TestMethod]
		public void VariationTest()
		{
		}

		static void Validate(ICatalog<IEvaluate<double>> catalog, IEvaluate<double> e)
		=> Context.Use(context =>
		{
			ReadOnlySpan<double> p = [1, 2, 3, 4];
			context.Init(catalog, p);

			var v = e.Evaluate(context);

			e.Description.Value
				.Should().Be("(({0} * {2}) + ({0} * {3}) + ({1} * {2}) + ({1} * {3}))");

			v.Result
				.Should().Be(21);
		});
	}
}
