/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using System;
using System.Collections.Generic;

namespace Open.Evaluation
{
    public class Parameter<TResult>
        : EvaluationBase<TResult>, IParameter<TResult>
        where TResult : IComparable
    {

        public Parameter(ushort id, Func<object, ushort, TResult> evaluator) : base()
        {
            if (evaluator == null)
                throw new ArgumentNullException("evaluator");
            ID = id;
            _evaluator = evaluator;
        }

        Func<object, ushort, TResult> _evaluator;

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

        protected override TResult EvaluateInternal(object context)
        {
            return _evaluator(context, ID);
        }

        protected override string ToStringInternal(object context)
        {
            return string.Empty + Evaluate(context);
        }
    }

    public class Parameter : Parameter<double>
    {
        public Parameter(ushort id) : base(id, GetParamValueFrom)
        {
        }

        static double GetParamValueFrom(object source, ushort id)
        {
			if (source is double[] array) return array[id];
			if (source is IReadOnlyList<double> list) return list[id];
			throw new ArgumentException("Unknown context type.");
        }
    }

	public static class ParameterExtensions
	{
		public static T GetParameter<T>(this Catalog catalog, ushort id, Func<ushort, T> factory)
			where T : IParameter
		{
			return catalog.Register(Parameter.ToStringRepresentation(id), k => factory(id));
		}

		public static Parameter GetParameter(this Catalog catalog, ushort id, Func<ushort, Parameter> factory)
		{
			return GetParameter<Parameter>(catalog, id, factory);
		}

		public static Parameter GetParameter(this Catalog catalog, ushort id)
		{
			return GetParameter(catalog, id, i => new Parameter(id));
		}
	}

}