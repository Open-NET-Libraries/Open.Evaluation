using System.Diagnostics.CodeAnalysis;

namespace Open.Evaluation.Core;

/// <summary>
/// Represents any evaluation that can be reproduced.
/// </summary>
public interface IReproducable<in TParam, TEval> : IEvaluate
	where TEval : IEvaluate
{
	/// <summary>
	/// Gets an evaluation based upon the provided param.
	/// </summary>
	/// <param name="catalog">The catalog to pull from.</param>
	/// <param name="param">The param to use.</param>
	/// <returns>The expected new evaluation.</returns>
	[return: NotNull]
	TEval NewUsing(ICatalog<TEval> catalog, TParam param);

	/// <inheritdoc cref="NewUsing(ICatalog{TEval}, TParam)"/>/>
	[return: NotNull]
	TEval NewUsing(TParam param);
}

/// <summary>
/// Extensions for creating the same kind of evaluation but modified by parameters.
/// </summary>
public static class ReproductionExtensions
{
	/// <summary>
	/// Gets a version of the existing using the parameters provided.
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

	/// <summary>
	/// Gets an item with one of the children removed.
	/// </summary>
	[return: NotNull]
	public static TEval NewWithIndexRemoved<T, TChild, TEval>(
		this T target,
		ICatalog<TEval> catalog, in int index)
		where T : IReproducable<IEnumerable<TChild>, TEval>, IParent<TChild>
		where TEval : IEvaluate
		=> target.NewUsing(catalog, target.Children.SkipAt(index));

	/// <summary>
	/// Gets an item with one of the children replaced.
	/// </summary>
	[return: NotNull]
	public static TEval NewWithIndexReplaced<T, TChild, TEval>(
		this T target,
		ICatalog<TEval> catalog, in int index, in TChild repacement)
		where T : IReproducable<IEnumerable<TChild>, TEval>, IParent<TChild>
		where TEval : IEvaluate
		=> target.NewUsing(catalog, target.Children.ReplaceAt(index, repacement));

	/// <summary>
	/// Gets an item with an additional child at the end.
	/// </summary>
	[return: NotNull]
	public static TEval NewWithAppended<T, TChild, TEval>(
		this T target,
		ICatalog<TEval> catalog, IEnumerable<TChild> appended)
		where T : IReproducable<IEnumerable<TChild>, TEval>, IParent<TChild>
		where TEval : IEvaluate
		=> target.NewUsing(catalog, target.Children.Concat(appended));

	/// <inheritdoc cref="NewWithAppended{T, TChild, TEval}(T, ICatalog{TEval}, IEnumerable{TChild})"/>
	[return: NotNull]
	public static TEval NewWithAppended<T, TChild, TEval>(
		this T target,
		ICatalog<TEval> catalog,
		TChild child,
		params TChild[] rest)
		where T : IReproducable<IEnumerable<TChild>, TEval>, IParent<TChild>
		where TEval : IEvaluate
		=> target.NewWithAppended(catalog, rest.Prepend(child));

	/// <inheritdoc cref="NewUsing{T, TChild, TEval}(T, ICatalog{TEval}, TChild, TChild[])"/>
	[return: NotNull]
	public static TEval NewUsing<T, TChild, TEval>(
		this T target,
		TChild child,
		params TChild[] rest)
		where T : IReproducable<IEnumerable<TChild>, TEval>
		where TEval : IEvaluate
		=> target.NewUsing(rest.Prepend(child));

	/// <inheritdoc cref="NewWithIndexRemoved{T, TChild, TEval}(T, ICatalog{TEval}, in int)" />
	[return: NotNull]
	public static TEval NewWithIndexRemoved<T, TChild, TEval>(
		this T target, in int index)
		where T : IReproducable<IEnumerable<TChild>, TEval>, IParent<TChild>
		where TEval : IEvaluate
		=> target.NewUsing(target.Children.SkipAt(index));

	/// <inheritdoc cref="NewWithIndexReplaced{T, TChild, TEval}(T, ICatalog{TEval}, in int, in TChild)">
	[return: NotNull]
	public static TEval NewWithIndexReplaced<T, TChild, TEval>(
		this T target, in int index, in TChild repacement)
		where T : IReproducable<IEnumerable<TChild>, TEval>, IParent<TChild>
		where TEval : IEvaluate
		=> target.NewUsing(target.Children.ReplaceAt(index, repacement));

	/// <inheritdoc cref="NewWithAppended{T, TChild, TEval}(T, ICatalog{TEval}, IEnumerable{TChild})"/>/>
	[return: NotNull]
	public static TEval NewWithAppended<T, TChild, TEval>(
		this T target, IEnumerable<TChild> appended)
		where T : IReproducable<IEnumerable<TChild>, TEval>, IParent<TChild>
		where TEval : IEvaluate
		=> target.NewUsing(target.Children.Concat(appended));

	/// <inheritdoc cref="NewWithAppended{T, TChild, TEval}(T, ICatalog{TEval}, IEnumerable{TChild})"/>
	[return: NotNull]
	public static TEval NewWithAppended<T, TChild, TEval>(
		this T target,
		TChild child,
		params TChild[] rest)
		where T : IReproducable<IEnumerable<TChild>, TEval>, IParent<TChild>
		where TEval : IEvaluate
		=> target.NewWithAppended<T, TChild, TEval>(rest.Prepend(child));
}
