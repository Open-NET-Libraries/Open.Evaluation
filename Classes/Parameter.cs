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

        protected override string ToStringRepresentationInternal()
        {
            return "{" + ID + "}";
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
            var list = source as IReadOnlyList<double>;
            if (list != null) return list[id];
            var array = source as double[];
            if (array != null) return list[id];
            throw new ArgumentException("Unknown context type.");
        }
    }

}