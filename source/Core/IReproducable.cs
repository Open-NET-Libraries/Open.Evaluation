using Open.Hierarchy;
using System.Collections.Generic;
using System.Linq;

namespace Open.Evaluation.Core
{
	public interface IReproducable<in TParam, TEval> : IEvaluate
		where TEval : IEvaluate
	{
		TEval NewUsing(ICatalog<TEval> catalog, TParam param);
	}

	public static class ReproductionExtensions
	{
		public static TEval NewUsing<T, TChild, TEval>(
			this T target,
			ICatalog<TEval> catalog,
			TChild child,
			params TChild[] rest)
			where T : IReproducable<IEnumerable<TChild>, TEval>
			where TEval : IEvaluate
			=> target.NewUsing(catalog, Enumerable.Repeat(child, 1).Concat(rest));

		public static TEval NewWithIndexRemoved<T, TChild, TEval>(
			this T target,
			ICatalog<TEval> catalog, in int index)
			where T : IReproducable<IEnumerable<TChild>, TEval>, IParent<TChild>
			where TEval : IEvaluate
			=> target.NewUsing(catalog, target.Children.SkipAt(index));

		public static TEval NewWithIndexReplaced<T, TChild, TEval>(
			this T target,
			ICatalog<TEval> catalog, in int index, in TChild repacement)
			where T : IReproducable<IEnumerable<TChild>, TEval>, IParent<TChild>
			where TEval : IEvaluate
			=> target.NewUsing(catalog, target.Children.ReplaceAt(index, repacement));

		public static TEval NewWithAppended<T, TChild, TEval>(
			this T target,
			ICatalog<TEval> catalog, IEnumerable<TChild> appended)
			where T : IReproducable<IEnumerable<TChild>, TEval>, IParent<TChild>
			where TEval : IEvaluate
			=> target.NewUsing(catalog, target.Children.Concat(appended));

		public static TEval NewWithAppended<T, TChild, TEval>(
			this T target,
			ICatalog<TEval> catalog, TChild child, params TChild[] rest)
			where T : IReproducable<IEnumerable<TChild>, TEval>, IParent<TChild>
			where TEval : IEvaluate
			=> target.NewWithAppended(catalog, Enumerable.Repeat(child, 1).Concat(rest));


	}
}
