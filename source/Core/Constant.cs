/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Open.Evaluation.Core
{
	[DebuggerDisplay("Value = {Value}")]
	public class Constant<TValue>
		: EvaluationBase<TValue>, IConstant<TValue>, IReproducable<TValue>
		where TValue : IComparable
	{

		protected Constant(in TValue value) : base()
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

		internal static Constant<TValue> Create(ICatalog<IEvaluate<TValue>> catalog, in TValue value)
		{
			var v = value;
			return catalog.Register(value.ToString(), k => new Constant<TValue>(v));
		}

		public virtual IEvaluate NewUsing(ICatalog<IEvaluate> catalog, in TValue value)
		{
			var v = value;
			return catalog.Register(value.ToString(), k => new Constant<TValue>(v));
		}
	}

	public static partial class ConstantExtensions
	{
		public static Constant<TValue> GetConstant<TValue>(
			this ICatalog<IEvaluate<TValue>> catalog,
			in TValue value)
			where TValue : IComparable
		{
			return Constant<TValue>.Create(catalog, in value);
		}

		public static Constant<TValue> SumOfConstants<TValue>(
			this ICatalog<IEvaluate<TValue>> catalog,
			in TValue c1, IEnumerable<IConstant<TValue>> constants)
			where TValue : struct, IComparable
		{
			if (typeof(TValue) == typeof(float))
			{
				if (float.IsNaN((float)(dynamic)c1) || constants.Any(c => c is IConstant<float> d && float.IsNaN(d.Value)))
					return catalog.GetConstant((TValue)(dynamic)float.NaN);
			}

			if (typeof(TValue) == typeof(double))
			{
				if (double.IsNaN((double)(dynamic)c1) || constants.Any(c => c is IConstant<double> d && double.IsNaN(d.Value)))
					return catalog.GetConstant((TValue)(dynamic)double.NaN);
			}

			dynamic result = c1;
			foreach (var c in constants)
			{
				result += c.Value;
			}
			return GetConstant<TValue>(catalog, result);
		}

		public static Constant<TValue> SumOfConstants<TValue>(
			this ICatalog<IEvaluate<TValue>> catalog,
			IEnumerable<IConstant<TValue>> constants)
			where TValue : struct, IComparable
		{
			return SumOfConstants(catalog, (TValue)(dynamic)0, constants);
		}

		public static Constant<TValue> SumOfConstants<TValue>(
			this ICatalog<IEvaluate<TValue>> catalog,
			in TValue c1, params IConstant<TValue>[] rest)
			where TValue : struct, IComparable
		{
			return SumOfConstants(catalog, in c1, (IEnumerable<IConstant<TValue>>)rest);
		}

		public static Constant<TValue> SumOfConstants<TValue>(
			this ICatalog<IEvaluate<TValue>> catalog,
			IConstant<TValue> c1, params IConstant<TValue>[] rest)
			where TValue : struct, IComparable
		{
			return SumOfConstants(catalog, c1.Value, rest);
		}

		public static Constant<TValue> ProductOfConstants<TValue>(
			this ICatalog<IEvaluate<TValue>> catalog,
			in TValue c1, IEnumerable<IConstant<TValue>> constants)
			where TValue : struct, IComparable
		{
			if (typeof(TValue) == typeof(float))
			{
				if (float.IsNaN((float)(dynamic)c1) || constants.Any(c => c is IConstant<float> d && float.IsNaN(d.Value)))
					return catalog.GetConstant((TValue)(dynamic)float.NaN);
			}

			if (typeof(TValue) == typeof(double))
			{
				if (double.IsNaN((double)(dynamic)c1) || constants.Any(c => c is IConstant<double> d && double.IsNaN(d.Value)))
					return catalog.GetConstant((TValue)(dynamic)double.NaN);
			}

			dynamic zero = (TValue)(dynamic)0;
			dynamic result = c1;
			foreach (var c in constants)
			{
				var val = c.Value;
				if (val == zero) return GetConstant<TValue>(catalog, zero);
				result *= val;
			}
			return GetConstant<TValue>(catalog, result);
		}

		public static Constant<TValue> ProductOfConstants<TValue>(
			this ICatalog<IEvaluate<TValue>> catalog,
			IEnumerable<IConstant<TValue>> constants)
			where TValue : struct, IComparable
		{
			return ProductOfConstants(catalog, (TValue)(dynamic)1, constants);
		}

		public static Constant<TValue> ProductOfConstants<TValue>(
			this ICatalog<IEvaluate<TValue>> catalog,
			in IConstant<TValue> c1, params IConstant<TValue>[] rest)
			where TValue : struct, IComparable
		{
			return ProductOfConstants(catalog, (TValue)(dynamic)1, (IEnumerable<IConstant<TValue>>)rest);
		}

		public static Constant<TValue> ProductOfConstants<TValue>(
			this ICatalog<IEvaluate<TValue>> catalog,
			in TValue c1, params IConstant<TValue>[] rest)
			where TValue : struct, IComparable
		{
			return ProductOfConstants(catalog, c1, (IEnumerable<IConstant<TValue>>)rest);
		}

	}

}
