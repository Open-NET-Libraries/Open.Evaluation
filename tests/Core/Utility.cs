namespace Open.Evaluation.Tests.Core;

[TestClass]
public class UtilityTests
{
	[TestMethod]
	public void SkipAt_RemovesElementAtIndex()
	{
		int[] source = [1, 2, 3, 4];
		Open.Evaluation.Utility.SkipAt(source, 1).Should().Equal(1, 3, 4);
	}

	[TestMethod]
	public void SkipAt_FirstIndex()
	{
		int[] source = [1, 2, 3];
		Open.Evaluation.Utility.SkipAt(source, 0).Should().Equal(2, 3);
	}

	[TestMethod]
	public void SkipAt_LastIndex()
	{
		int[] source = [1, 2, 3];
		Open.Evaluation.Utility.SkipAt(source, 2).Should().Equal(1, 2);
	}

	[TestMethod]
	public void SkipAt_NegativeIndex_Throws()
	{
		int[] source = [1, 2, 3];
		Action act = () => _ = Open.Evaluation.Utility.SkipAt(source, -1).ToArray();
		act.Should().Throw<Exception>();
	}

	[TestMethod]
	public void ReplaceAt_SubstitutesElementAtIndex()
	{
		int[] source = [1, 2, 3];
		Open.Evaluation.Utility.ReplaceAt(source, 1, 99).Should().Equal(1, 99, 3);
	}

	[TestMethod]
	public void InsertAt_SingleItem_InsertsBeforeIndex()
	{
		int[] source = [1, 2, 3];
		Open.Evaluation.Utility.InsertAt(source, 1, 99).Should().Equal(1, 99, 2, 3);
	}

	[TestMethod]
	public void InsertAt_SingleItem_AtStart()
	{
		int[] source = [1, 2, 3];
		Open.Evaluation.Utility.InsertAt(source, 0, 99).Should().Equal(99, 1, 2, 3);
	}

	[TestMethod]
	public void InsertAt_SingleItem_NegativeIndex_Throws()
	{
		int[] source = [1, 2, 3];
		Action act = () => _ = Open.Evaluation.Utility.InsertAt(source, -1, 99).ToArray();
		act.Should().Throw<ArgumentOutOfRangeException>();
	}

	[TestMethod]
	public void InsertAt_Collection_InsertsAllElementsBeforeIndex()
	{
		int[] source = [1, 2, 3];
		int[] injection = [97, 98, 99];
		Open.Evaluation.Utility.InsertAt(source, 1, injection).Should().Equal(1, 97, 98, 99, 2, 3);
	}

	[TestMethod]
	public void InsertAt_Collection_AtEnd_PastIndex_AppendsNothing()
	{
		// The current implementation only injects when count == index during enumeration;
		// an index at/past the source length means the injection never triggers.
		int[] source = [1, 2, 3];
		int[] injection = [99];
		Open.Evaluation.Utility.InsertAt(source, 3, injection).Should().Equal(1, 2, 3);
	}

	[TestMethod]
	public void Extract_MovesMatchingItemsOutOfSource()
	{
		List<int> source = [1, 2, 3, 4, 5];
		var extracted = Open.Evaluation.Utility.Extract(source, x => x % 2 == 0);

		extracted.Should().Equal(2, 4);
		source.Should().Equal(1, 3, 5);
	}

	[TestMethod]
	public void Extract_NoMatches_LeavesSourceUnchanged()
	{
		List<int> source = [1, 3, 5];
		var extracted = Open.Evaluation.Utility.Extract(source, x => x % 2 == 0);

		extracted.Should().BeEmpty();
		source.Should().Equal(1, 3, 5);
	}

	[TestMethod]
	public void ExtractType_MovesMatchingTypedItemsOutOfSource()
	{
		System.Collections.IList source = new List<object> { 1, "a", 2, "b", 3.5 };
		var extracted = Open.Evaluation.Utility.ExtractType<int>(source);

		extracted.Should().Equal(1, 2);
		source.Cast<object>().Should().Equal("a", "b", 3.5);
	}

	[TestMethod]
	public void Rent_Array_InvokesActionWithArrayOfRequestedMinLength()
	{
		var pool = System.Buffers.ArrayPool<int>.Shared;
		var observedLength = -1;
		pool.Rent(4, (int[] a) => observedLength = a.Length);
		(observedLength >= 4).Should().BeTrue();
	}

	[TestMethod]
	public void Rent_Func_ReturnsActionResult()
	{
		var pool = System.Buffers.ArrayPool<int>.Shared;
		var result = pool.Rent(4, (int[] a) => a.Length >= 4);
		result.Should().BeTrue();
	}

	[TestMethod]
	public void Rent_SmallMinLength_UsesExactPlainArray_NotRoundedPoolBucket()
	{
		// minLength at/below the pool's internal threshold (128) falls back to `new T[minLength]`
		// directly, giving an exact-length array - unlike ArrayPool.Rent, which commonly rounds
		// up to the next power-of-two bucket size.
		var pool = System.Buffers.ArrayPool<int>.Shared;
		var observedLength = -1;
		pool.Rent(50, (int[] a) => observedLength = a.Length);
		observedLength.Should().Be(50);
	}
}
