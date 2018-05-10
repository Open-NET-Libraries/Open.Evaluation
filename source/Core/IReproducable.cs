using Open.Hierarchy;
using System.Collections.Generic;
using System.Linq;

namespace Open.Evaluation.Core
{
	public interface IReproducable<TParam> : IEvaluate
	{
		IEvaluate NewUsing(ICatalog<IEvaluate> catalog, TParam param);
	}

	public static class ReproductionExtensions
	{
		public static IEvaluate NewUsing<T, TChild>(
			this T target,
			ICatalog<IEvaluate> catalog, TChild child, params TChild[] rest)
			where T : IReproducable<IEnumerable<TChild>>
		{
			return target.NewUsing(catalog, Enumerable.Repeat(child, 1).Concat(rest));
		}

		public static IEvaluate NewWithIndexRemoved<T, TChild>(
			this T target,
			ICatalog<IEvaluate> catalog, int index)
			where T : IReproducable<IEnumerable<TChild>>, IParent<TChild>
		{
			return target.NewUsing(catalog, target.Children.SkipAt(index));
		}

		public static IEvaluate NewWithIndexReplaced<T, TChild>(
			this T target,
			ICatalog<IEvaluate> catalog, int index, TChild repacement)
			where T : IReproducable<IEnumerable<TChild>>, IParent<TChild>
		{
			return target.NewUsing(catalog, target.Children.ReplaceAt(index, repacement));
		}

		public static IEvaluate NewWithAppended<T, TChild>(
			this T target,
			ICatalog<IEvaluate> catalog, IEnumerable<TChild> appended)
			where T : IReproducable<IEnumerable<TChild>>, IParent<TChild>
		{
			return target.NewUsing(catalog, target.Children.Concat(appended));
		}

		public static IEvaluate NewWithAppended<T, TChild>(
			this T target,
			ICatalog<IEvaluate> catalog, TChild child, params TChild[] rest)
			where T : IReproducable<IEnumerable<TChild>>, IParent<TChild>
		{
			return target.NewWithAppended(catalog, Enumerable.Repeat(child, 1).Concat(rest));
		}


	}
}
