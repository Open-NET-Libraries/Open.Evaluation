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
		: EvaluationBase<TValue>, IConstant<TValue>, IReproducable<TValue, IEvaluate<TValue>>
		where TValue : IComparable
	{

		protected Constant(TValue value)
		{
			Value = value;
		}

		public TValue Value
		{
			get;
		}

		IComparable IConstant.Value => Value;

		public static string ToStringRepresentation(TValue value)
			=> string.Empty + value;

		protected override string ToStringRepresentationInternal()
			=> ToStringRepresentation(Value);

		protected override TValue EvaluateInternal(object context) => Value;

		protected override string ToStringInternal(object context) => ToStringRepresentation();

		internal static Constant<TValue> Create(ICatalog<IEvaluate<TValue>> catalog, TValue value)
			=> catalog.Register(ToStringRepresentation(value), k => new Constant<TValue>(value));

		public virtual IEvaluate<TValue> NewUsing(ICatalog<IEvaluate<TValue>> catalog, TValue value)
			=> catalog.Register(ToStringRepresentation(value), k => new Constant<TValue>(value));

		public static implicit operator TValue(Constant<TValue> c)
			=> c.Value;
	}

	public static partial class ConstantExtensions
	{
		public static Constant<TValue> GetConstant<TValue>(
			this ICatalog<IEvaluate<TValue>> catalog,
			TValue value)
			where TValue : IComparable
		{
			// ReSharper disable once SuspiciousTypeConversion.Global
			if (catalog is ICatalog<IEvaluate<double>> dCat && value is double d)
				return (dynamic)Constant.Create(dCat, d);

			return Constant<TValue>.Create(catalog, value);
		}

		public static Constant<TValue> SumOfConstants<TValue>(
			this ICatalog<IEvaluate<TValue>> catalog,
			TValue c1, IEnumerable<IConstant<TValue>> constants)
			where TValue : struct, IComparable
		{
			if (typeof(TValue) == typeof(float))
			{
				// ReSharper disable once PossibleMultipleEnumeration
				if (float.IsNaN((float)(dynamic)c1) || constants.Any(c => c is IConstant<float> d && float.IsNaN(d.Value)))
					return catalog.GetConstant((TValue)(dynamic)float.NaN);
			}

			if (typeof(TValue) == typeof(double))
			{
				// ReSharper disable once PossibleMultipleEnumeration
				if (double.IsNaN((double)(dynamic)c1) || constants.Any(c => c is IConstant<double> d && double.IsNaN(d.Value)))
					return catalog.GetConstant((TValue)(dynamic)double.NaN);
			}

			dynamic result = c1;
			// ReSharper disable once PossibleMultipleEnumeration
			// ReSharper disable once LoopCanBeConvertedToQuery
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
			=> SumOfConstants(catalog, (TValue)(dynamic)0, constants);

		public static Constant<TValue> SumOfConstants<TValue>(
			this ICatalog<IEvaluate<TValue>> catalog,
			TValue c1, params IConstant<TValue>[] rest)
			where TValue : struct, IComparable
			=> SumOfConstants(catalog, c1, (IEnumerable<IConstant<TValue>>)rest);

		public static Constant<TValue> SumOfConstants<TValue>(
			this ICatalog<IEvaluate<TValue>> catalog,
			IConstant<TValue> c1, params IConstant<TValue>[] rest)
			where TValue : struct, IComparable
			=> SumOfConstants(catalog, c1.Value, rest);

		public static Constant<TValue> ProductOfConstants<TValue>(
			this ICatalog<IEvaluate<TValue>> catalog,
			TValue c1, IEnumerable<IConstant<TValue>> constants)
			where TValue : struct, IComparable
		{
			if (typeof(TValue) == typeof(float))
			{
				// ReSharper disable once PossibleMultipleEnumeration
				if (float.IsNaN((float)(dynamic)c1) || constants.Any(c => c is IConstant<float> d && float.IsNaN(d.Value)))
					return catalog.GetConstant((TValue)(dynamic)float.NaN);
			}

			if (typeof(TValue) == typeof(double))
			{
				// ReSharper disable once PossibleMultipleEnumeration
				if (double.IsNaN((double)(dynamic)c1) || constants.Any(c => c is IConstant<double> d && double.IsNaN(d.Value)))
					return catalog.GetConstant((TValue)(dynamic)double.NaN);
			}

			dynamic zero = (TValue)(dynamic)0;
			dynamic result = c1;
			// ReSharper disable once PossibleMultipleEnumeration
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
			=> ProductOfConstants(catalog, (TValue)(dynamic)1, constants);

		public static Constant<TValue> ProductOfConstants<TValue>(
			this ICatalog<IEvaluate<TValue>> catalog,
			IConstant<TValue> c1, params IConstant<TValue>[] rest)
			where TValue : struct, IComparable
			=> ProductOfConstants(catalog, c1.Value, (IEnumerable<IConstant<TValue>>)rest);

		public static Constant<TValue> ProductOfConstants<TValue>(
			this ICatalog<IEvaluate<TValue>> catalog,
			TValue c1, params IConstant<TValue>[] rest)
			where TValue : struct, IComparable
			=> ProductOfConstants(catalog, c1, (IEnumerable<IConstant<TValue>>)rest);

	}

}
