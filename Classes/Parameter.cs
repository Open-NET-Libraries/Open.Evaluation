using System;
using System.Collections.Generic;

namespace EvaluationFramework
{
	public class Parameter<TContext, TResult>
		: EvaluationBase<TContext, TResult>, IParameter<TContext, TResult>, IClonable<Parameter<TContext, TResult>>
	{

		public Parameter(ushort id, Func<TContext, ushort, TResult> evaluator) : base()
		{
			if (evaluator == null)
				throw new ArgumentNullException("evaluator");
			_evaluator = evaluator;
		}

		Func<TContext, ushort, TResult> _evaluator;

		public ushort ID
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		protected override string ToStringRepresentationInternal()
		{
			return "{" + ID + "}";
		}

		public Parameter<TContext, TResult> Clone()
		{
			return new Parameter<TContext, TResult>(ID, _evaluator);
		}

		public override TResult Evaluate(TContext context)
		{
			return _evaluator(context, ID);
		}

		public override string ToString(TContext context)
		{
			return string.Empty + Evaluate(context);
		}

	}

	public class Parameter<TResult> : Parameter<IReadOnlyList<TResult>, TResult>
	{
		public Parameter(ushort id) : base(id, GetParamValueFrom)
		{
		}

		static TResult GetParamValueFrom(IReadOnlyList<TResult> source, ushort id)
		{
			return source[id];
		}
	}
}