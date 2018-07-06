using Open.Hierarchy;
using System.Collections.Generic;
using System.Linq;

namespace Open.Evaluation.Core
{
	public interface IReproducable<TParam> : IEvaluate
	{
		IEvaluate NewUsing(in ICatalog<IEvaluate> catalog, in TParam param);
	}

	public static class ReproductionExtensions
	{
		public static IEvaluate NewUsing<T, TChild>(
			this T target,
			in ICatalog<IEvaluate> catalog, in TChild child, params TChild[] rest)
			where T : IReproducable<IEnumerable<TChild>>
		{
			return target.NewUsing(in catalog, Enumerable.Repeat(child, 1).Concat(rest));
		}

		public static IEvaluate NewWithIndexRemoved<T, TChild>(
			this T target,
			in ICatalog<IEvaluate> catalog, in int index)
			where T : IReproducable<IEnumerable<TChild>>, IParent<TChild>
		{
			return target.NewUsing(in catalog, target.Children.SkipAt(index));
		}

		public static IEvaluate NewWithIndexReplaced<T, TChild>(
			this T target,
			in ICatalog<IEvaluate> catalog, in int index, in TChild repacement)
			where T : IReproducable<IEnumerable<TChild>>, IParent<TChild>
		{
			return target.NewUsing(in catalog, target.Children.ReplaceAt(index, repacement));
		}

		public static IEvaluate NewWithAppended<T, TChild>(
			this T target,
			in ICatalog<IEvaluate> catalog, in IEnumerable<TChild> appended)
			where T : IReproducable<IEnumerable<TChild>>, IParent<TChild>
		{
			return target.NewUsing(in catalog, target.Children.Concat(appended));
		}

		public static IEvaluate NewWithAppended<T, TChild>(
			this T target,
			in ICatalog<IEvaluate> catalog, in TChild child, params TChild[] rest)
			where T : IReproducable<IEnumerable<TChild>>, IParent<TChild>
		{
			return target.NewWithAppended(in catalog, Enumerable.Repeat(child, 1).Concat(rest));
		}


	}
}
