using System.Collections.Generic;

namespace Open.Evaluation
{
	public interface IReproducable<TParam> : IEvaluate
    {
		IEvaluate NewUsing(TParam param);
	}
}
