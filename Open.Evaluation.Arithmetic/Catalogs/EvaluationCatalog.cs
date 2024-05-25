namespace Open.Evaluation.Arithmetic;

public partial class EvaluationCatalog<T> : Catalog<IEvaluate<T>>
	where T : notnull, INumber<T>
{
	protected override TItem OnBeforeRegistration<TItem>(TItem item)
	{
		Debug.Assert(item is not Exponent<T>);
		Debug.Assert(item is not Sum<T>);
		Debug.Assert(item is not Product<T>);
		Debug.Assert(item is not Constant<T>);

		return item;
	}
}
