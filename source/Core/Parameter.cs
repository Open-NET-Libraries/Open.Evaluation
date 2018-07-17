/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using System;
using System.Collections.Generic;

namespace Open.Evaluation.Core
{
	public class Parameter<TValue>
		: EvaluationBase<TValue>, IParameter<TValue>, IReproducable<ushort, IEvaluate<TValue>>
		where TValue : IComparable
	{

		protected Parameter(ushort id, Func<object, ushort, TValue> evaluator = null)
		{
			_evaluator = evaluator ?? GetParamValueFrom;
			ID = id;
		}

		readonly Func<object, ushort, TValue> _evaluator;

		static TValue GetParamValueFrom(object source, ushort id)
		{
			switch (source)
			{
				case IReadOnlyList<TValue> list:
					return list[id];
				case IDictionary<ushort, TValue> d:
					return d[id];
				case TValue v:
					return v;
			}

			throw new ArgumentException("Unknown type.", nameof(source));
		}


		public ushort ID
		{
			get;
		}

		public static string ToStringRepresentation(ushort id) => "{" + id + "}";

		protected override string ToStringRepresentationInternal()
			=> ToStringRepresentation(ID);

		protected override TValue EvaluateInternal(object context)
			=> _evaluator(context is ParameterContext p ? p.Context : context, ID);

		protected override string ToStringInternal(object context)
			=> string.Empty + Evaluate(context);

		internal static Parameter<TValue> Create(ICatalog<IEvaluate<TValue>> catalog, ushort id)
			=> catalog.Register(ToStringRepresentation(id), k => new Parameter<TValue>(id));

		public virtual IEvaluate<TValue> NewUsing(ICatalog<IEvaluate<TValue>> catalog, ushort id)
			=> catalog.Register(ToStringRepresentation(id), k => new Parameter<TValue>(id));
	}

	public static partial class ParameterExtensions
	{
		public static Parameter<TValue> GetParameter<TValue>(
			this ICatalog<IEvaluate<TValue>> catalog, ushort id)
			where TValue : IComparable
			=> Parameter<TValue>.Create(catalog, id);

	}
}
