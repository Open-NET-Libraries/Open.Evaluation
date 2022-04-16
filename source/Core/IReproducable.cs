using Open.Hierarchy;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Open.Evaluation.Core;

/// <summary>
/// Represents any evaluation that can be reproduced.
/// </summary>
public interface IReproducable<in TParam, TEval> : IEvaluate
	where TEval : IEvaluate
{
	/// <summary>
	/// Produces a new evaluation based upon the provided one.
	/// </summary>
	/// <param name="catalog">The catalog to pull from.</param>
	/// <param name="param">The param to use.</param>
	/// <returns>The expected new evaluation.</returns>
	[return: NotNull]
	TEval NewUsing(ICatalog<TEval> catalog, TParam param);
}

/// <summary>
/// Extensions for creating the same kind of evaluation but modified by parameters.
/// </summary>
public static class ReproductionExtensions
{
	/// <summary>
	/// Creates an new version of the existing using the parameters provided.
	/// </summary>
	[return: NotNull]
	public static TEval NewUsing<T, TChild, TEval>(
		this T target,
		ICatalog<TEval> catalog,
		TChild child,
		params TChild[] rest)
		where T : IReproducable<IEnumerable<TChild>, TEval>
		where TEval : IEvaluate
		=> target.NewUsing(catalog, rest.Prepend(child));

	[return: NotNull]
	public static TEval NewWithIndexRemoved<T, TChild, TEval>(
		this T target,
		ICatalog<TEval> catalog, in int index)
		where T : IReproducable<IEnumerable<TChild>, TEval>, IParent<TChild>
		where TEval : IEvaluate
		=> target.NewUsing(catalog, target.Children.SkipAt(index));

	[return: NotNull]
	public static TEval NewWithIndexReplaced<T, TChild, TEval>(
		this T target,
		ICatalog<TEval> catalog, in int index, in TChild repacement)
		where T : IReproducable<IEnumerable<TChild>, TEval>, IParent<TChild>
		where TEval : IEvaluate
		=> target.NewUsing(catalog, target.Children.ReplaceAt(index, repacement));

	[return: NotNull]
	public static TEval NewWithAppended<T, TChild, TEval>(
		this T target,
		ICatalog<TEval> catalog, IEnumerable<TChild> appended)
		where T : IReproducable<IEnumerable<TChild>, TEval>, IParent<TChild>
		where TEval : IEvaluate
		=> target.NewUsing(catalog, target.Children.Concat(appended));

	[return: NotNull]
	public static TEval NewWithAppended<T, TChild, TEval>(
		this T target,
		ICatalog<TEval> catalog,
		TChild child,
		params TChild[] rest)
		where T : IReproducable<IEnumerable<TChild>, TEval>, IParent<TChild>
		where TEval : IEvaluate
		=> target.NewWithAppended(catalog, rest.Prepend(child));
}
