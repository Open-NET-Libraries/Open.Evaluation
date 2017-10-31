/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Cloneable;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Open.Evaluation
{
	public class Constant<TResult>
		: EvaluationBase<TResult>, IConstant<TResult>, ICloneable
		where TResult : IComparable
	{

		internal Constant(TResult value) : base()
		{
			Value = value;
		}

		public TResult Value
		{
			get;
			private set;
		}

		IComparable IConstant.Value => Value;

		protected override string ToStringRepresentationInternal()
		{
			return string.Empty + Value;
		}

		public Constant<TResult> Clone()
		{
			return new Constant<TResult>(Value);
		}


		object ICloneable.Clone()
		{
			return Clone();
		}

		protected override TResult EvaluateInternal(object context)
		{
			return Value;
		}

		protected override string ToStringInternal(object context)
		{
			return ToStringRepresentation();
		}

	}

	public sealed class Constant : Constant<double>
	{
		internal Constant(double value) : base(value)
		{
		}
	}

	public static class ConstantExtensions
	{
		public static T GetConstant<T, TValue>(this ICatalog<IEvaluate<TValue>> catalog, TValue value, Func<TValue, T> factory)
			where TValue : IComparable
			where T : IConstant<TValue>
		{
			return catalog.Register(value.ToString(), k => factory(value));
		}

		public static Constant<TValue> GetConstant<TValue>(this ICatalog<IEvaluate<TValue>> catalog, TValue value)
			where TValue : IComparable
		{
			return GetConstant(catalog, value, v=> new Constant<TValue>(v));
		}

		public static Constant<TValue> SumConstants<TValue>(this ICatalog<IEvaluate<TValue>> catalog, IEnumerable<IConstant<TValue>> constants)
			where TValue : struct, IComparable
		{
			dynamic result = 0;
			foreach (var c in constants)
			{
				result += c.Value;
			}
			return GetConstant<TValue>(catalog, result);
		}

		public static Constant SumConstants(this ICatalog<IEvaluate<double>> catalog, IEnumerable<IConstant<double>> constants)
		{
			return GetConstant(catalog, constants.Select(c=>c.Value).Sum() );
		}

		public static Constant GetConstant(this ICatalog<IEvaluate<double>> catalog, double value, Func<double, Constant> factory)
		{
			return GetConstant<Constant, double>(catalog, value, factory);
		}

		public static Constant GetConstant(this ICatalog<IEvaluate<double>> catalog, double value)
		{
			return GetConstant(catalog, value, i => new Constant(value));
		}
	}

}