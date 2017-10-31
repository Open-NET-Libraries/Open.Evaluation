namespace Open.Evaluation
{
	public interface IReproducable<TParam> : IEvaluate
    {
		IEvaluate NewUsing(ICatalog<IEvaluate> catalog, TParam param);
	}
}
