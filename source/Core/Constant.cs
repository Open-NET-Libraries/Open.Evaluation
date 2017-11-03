/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Open.Evaluation.Core
{
	[DebuggerDisplay("Value = {Value}")]
	public class Constant<TValue>
		: EvaluationBase<TValue>, IConstant<TValue>, IReproducable<TValue>
		where TValue : IComparable
	{

		protected Constant(TValue value) : base()
		{
			Value = value;
		}

		public TValue Value
		{
			get;
			private set;
		}

		IComparable IConstant.Value => Value;

		protected override string ToStringRepresentationInternal()
		{
			return string.Empty + Value;
		}

		protected override TValue EvaluateInternal(object context)
		{
			return Value;
		}

		protected override string ToStringInternal(object context)
		{
			return ToStringRepresentation();
		}

		internal static Constant<TValue> Create(ICatalog<IEvaluate<TValue>> catalog, TValue value)
		{
			return catalog.Register(value.ToString(), k => new Constant<TValue>(value));
		}

		public virtual IEvaluate NewUsing(ICatalog<IEvaluate> catalog, TValue value)
		{
			return catalog.Register(value.ToString(), k => new Constant<TValue>(value));
		}
	}

	public static partial class ConstantExtensions
	{
		public static Constant<TValue> GetConstant<TValue>(
			this ICatalog<IEvaluate<TValue>> catalog,
			TValue value)
			where TValue : IComparable
		{
			return Constant<TValue>.Create(catalog, value);
		}

		public static Constant<TValue> SumOfConstants<TValue>(
			this ICatalog<IEvaluate<TValue>> catalog,
			IEnumerable<IConstant<TValue>> constants)
			where TValue : struct, IComparable
		{
			dynamic result = 0;
			foreach (var c in constants)
			{
				result += c.Value;
			}
			return GetConstant<TValue>(catalog, result);
		}

		public static Constant<TValue> SumOfConstants<TValue>(
			this ICatalog<IEvaluate<TValue>> catalog,
			TValue c1, params IConstant<TValue>[] rest)
			where TValue : struct, IComparable
		{
			dynamic result = c1;
			foreach (var c in rest)
			{
				result += c.Value;
			}
			return GetConstant<TValue>(catalog, result);
		}

		public static Constant<TValue> SumOfConstants<TValue>(
			this ICatalog<IEvaluate<TValue>> catalog,
			IConstant<TValue> c1, params IConstant<TValue>[] rest)
			where TValue : struct, IComparable
		{
			return SumOfConstants(catalog, rest.Concat(c1));
		}

		public static Constant<TValue> ProductOfConstants<TValue>(
			this ICatalog<IEvaluate<TValue>> catalog,
			IEnumerable<IConstant<TValue>> constants)
			where TValue : struct, IComparable
		{
			dynamic result = 0;
			foreach (var c in constants)
			{
				result *= c.Value;
			}
			return GetConstant<TValue>(catalog, result);
		}

		public static Constant<TValue> ProductOfConstants<TValue>(
			this ICatalog<IEvaluate<TValue>> catalog,
			IConstant<TValue> c1, params IConstant<TValue>[] rest)
			where TValue : struct, IComparable
		{
			return ProductOfConstants(catalog, rest.Concat(c1));
		}

		public static Constant<TValue> ProductOfConstants<TValue>(
			this ICatalog<IEvaluate<TValue>> catalog,
			TValue c1, params IConstant<TValue>[] rest)
			where TValue : struct, IComparable
		{
			dynamic result = c1;
			foreach (var c in rest)
			{
				result *= c.Value;
			}
			return GetConstant<TValue>(catalog, result);
		}

	}

}