namespace Open.Evaluation.Core;

public class EvaluationCatalog<T> : Catalog<IEvaluate<T>>
	where T : notnull, IEquatable<T>, IComparable<T>
{
	private static EvaluationCatalog<T>? _instance;
	public static new EvaluationCatalog<T> Shared
		=> LazyInitializer.EnsureInitialized(ref _instance);

	//protected override TItem OnBeforeRegistration<TItem>(TItem item)
	//{
	//	Debug.Assert(item is not Exponent<T>);
	//	Debug.Assert(item is not Sum<T>);
	//	Debug.Assert(item is not Product<T>);
	//	Debug.Assert(item is not Constant<T>);

	//	return item;
	//}

	private MutationCatalog? _mutation;
	public MutationCatalog Mutation =>
		LazyInitializer.EnsureInitialized(ref _mutation, () => new MutationCatalog(this))!;

	public class MutationCatalog : SubmoduleBase<EvaluationCatalog<T>>
	{
		internal MutationCatalog(EvaluationCatalog<T> source) : base(source)
		{
		}
	}

	private VariationCatalog? _variation;
	public VariationCatalog Variation =>
		LazyInitializer.EnsureInitialized(ref _variation, () => new VariationCatalog(this))!;

	public class VariationCatalog : SubmoduleBase<EvaluationCatalog<T>>
	{
		internal VariationCatalog(EvaluationCatalog<T> source) : base(source)
		{
		}
	}
}