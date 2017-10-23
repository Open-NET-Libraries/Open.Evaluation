using System;
using System.Collections.Generic;
using System.Text;

namespace Open.Evaluation
{
    public interface IReproducable : IEvaluate, IParent<IEvaluate>
    {
		object ReproductionParam { get; }

		IEvaluate CreateNewFrom(object param, IEnumerable<IEvaluate> children);

		IEvaluate CreateNewFrom(IEnumerable<IEvaluate> children);

		IEvaluate CreateNewFrom(object param, IEvaluate child);

		IEvaluate CreateNewFrom(IEvaluate child);
	}
}
