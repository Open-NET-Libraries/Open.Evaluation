/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using System;
using System.Collections.Generic;

namespace Open.Evaluation.Core
{
    public class Parameter<TValue>
        : EvaluationBase<TValue>, IParameter<TValue>, IReproducable<ushort>
        where TValue : IComparable
    {

        protected Parameter(ushort id, Func<object, ushort, TValue> evaluator = null) : base()
        {
			_evaluator = evaluator ?? GetParamValueFrom;
			ID = id;
        }

        Func<object, ushort, TValue> _evaluator;

		static TValue GetParamValueFrom(object source, ushort id)
		{
			if (source is IReadOnlyList<TValue> list) return list[id];
			if (source is IDictionary<ushort, TValue> d) return d[id];
			throw new ArgumentException("Unknown context type.");
		}


		public ushort ID
        {
            get;
            private set;
        }

		public static string ToStringRepresentation(ushort id)
		{
			return "{" + id + "}";
		}

        protected override string ToStringRepresentationInternal()
        {
            return ToStringRepresentation(ID);
        }

        protected override TValue EvaluateInternal(object context)
        {
            return _evaluator(context is ParameterContext p ? p.Context : context, ID);
        }

        protected override string ToStringInternal(object context)
        {
            return string.Empty + Evaluate(context);
        }

		internal static Parameter<TValue> Create(ICatalog<IEvaluate<TValue>> catalog, ushort id)
		{
			return catalog.Register(ToStringRepresentation(id), k => new Parameter<TValue>(id));
		}

		public virtual IEvaluate NewUsing(ICatalog<IEvaluate> catalog, ushort id)
		{
			return catalog.Register(ToStringRepresentation(id), k => new Parameter<TValue>(id));
		}
	}

	public static partial class ParameterExtensions
	{
		public static Parameter<TValue> GetParameter<TValue>(
			this ICatalog<IEvaluate<TValue>> catalog, ushort id)
			where TValue : IComparable
		{
			return Parameter<TValue>.Create(catalog, id);
		}
	}

}