namespace Open.Evaluation.Tests.Core;

[TestClass]
public class EvaluationResultTests
{
	[TestMethod]
	public void Description_FromFactory_IsLazy_NotInvokedUntilAccessed()
	{
		var invoked = false;
		var result = new EvaluationResult<double>(5d, v =>
		{
			invoked = true;
			return v.ToString();
		});

		invoked.Should().BeFalse("the description factory must not run until .Description.Value is accessed");

		var text = result.Description.Value;

		invoked.Should().BeTrue();
		text.Should().Be("5");
	}

	[TestMethod]
	public void Description_DefaultConstructor_UsesResultToString()
	{
		var result = new EvaluationResult<double>(5d);
		result.Description.Value.Should().Be("5");
	}

	[TestMethod]
	public void Description_FromPlainString_IsUsedVerbatim()
	{
		var result = new EvaluationResult<double>(5d, "five");
		result.Description.Value.Should().Be("five");
	}

	[TestMethod]
	public void NullDescription_Throws()
	{
		Action act = () => new EvaluationResult<double>(5d, (string)null!);
		act.Should().Throw<ArgumentNullException>();
	}

	[TestMethod]
	public void ImplicitConversion_ToUnderlyingValue()
	{
		var result = EvaluationResult.Create(5d);
		double value = result;
		value.Should().Be(5d);
	}

	[TestMethod]
	public void ImplicitConversion_ToObjectResult_PreservesValueAndDescription()
	{
		EvaluationResult<double> result = EvaluationResult.Create(5d, "five");
		EvaluationResult<object> asObject = result;

		asObject.Result.Should().Be(5d);
		asObject.Description.Value.Should().Be("five");
	}

	[TestMethod]
	public void ExplicitConversion_FromObjectResult_WithMatchingType_Succeeds()
	{
		EvaluationResult<double> original = EvaluationResult.Create(5d, "five");
		EvaluationResult<object> asObject = original;

		var roundTripped = (EvaluationResult<double>)asObject;
		roundTripped.Result.Should().Be(5d);
	}

	[TestMethod]
	public void ExplicitConversion_FromObjectResult_WithMismatchedType_Throws()
	{
		EvaluationResult<double> original = EvaluationResult.Create(5d, "five");
		EvaluationResult<object> asObject = original;

		Action act = () => _ = (EvaluationResult<int>)asObject;
		act.Should().Throw<InvalidCastException>();
	}

	[TestMethod]
	public void Coerce_WithMatchingType_Succeeds()
	{
		IEvaluationResult boxed = EvaluationResult.Create(5d, "five");
		var coerced = EvaluationResult<double>.Coerce(boxed);
		coerced.Result.Should().Be(5d);
	}

	[TestMethod]
	public void Coerce_WithMismatchedType_Throws()
	{
		IEvaluationResult boxed = EvaluationResult.Create(5d, "five");
		Action act = () => EvaluationResult<int>.Coerce(boxed);
		act.Should().Throw<InvalidCastException>();
	}

	[TestMethod]
	public void IEvaluationResult_Result_BoxesUnderlyingValue()
	{
		IEvaluationResult boxed = EvaluationResult.Create(5d, "five");
		boxed.Result.Should().Be(5d);
	}

	/// <summary>
	/// Documents an actual semantics gotcha: <see cref="EvaluationResult{T}"/> is a record
	/// struct, so equality is member-wise across Result *and* Description. Description is a
	/// <see cref="Lazy{T}"/>, which does not itself have value equality, so two results
	/// carrying the identical numeric value but distinct Lazy&lt;string&gt; instances compare
	/// as unequal even though both would render the same description text.
	/// </summary>
	[TestMethod]
	public void RecordEquality_SameValueDifferentLazyInstances_AreNotEqual()
	{
		var a = EvaluationResult.Create(5d, "five");
		var b = EvaluationResult.Create(5d, "five");

		a.Result.Should().Be(b.Result);
		a.Description.Value.Should().Be(b.Description.Value);
		a.Equals(b).Should().BeFalse(
			"Description is a Lazy<string> without value equality, so record equality falls back to reference equality for it");
	}

	[TestMethod]
	public void RecordEquality_SameSharedLazyInstance_AreEqual()
	{
		var description = Lazy.FromValue("five");
		var a = new EvaluationResult<double>(5d, description);
		var b = new EvaluationResult<double>(5d, description);

		a.Equals(b).Should().BeTrue();
	}
}
