using Open.Evaluation.Core;
using Open.Hierarchy;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Numerics;
using Throw;

namespace Open.Evaluation.Arithmetic;

[SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
public static partial class EvaluationCatalogExtensions
{
	const string CannotOperateNewNodeNullValue = "Cannot operate when the newNode.Value is null.";

	/// <summary>
	/// Applies a multiple to any node.
	/// </summary>
	/// <param name="catalog">The catalog to use.</param>
	/// <param name="sourceNode">The node to multiply by.</param>
	/// <param name="multiple">The value to multiply by.</param>
	/// <returns>The resultant root evaluation.</returns>
	[SuppressMessage("Style", "IDE0046:Convert to conditional expression", Justification = "Preferred verbosity")]
	public static IEvaluate<T> MultiplyNode<T>(
		this EvaluationCatalog<T> catalog,
		Node<IEvaluate<T>> sourceNode, T multiple)
		where T : notnull, INumber<T>
	{
		catalog.ThrowIfNull();
		sourceNode.ThrowIfNull();
		Contract.EndContractBlock();

		if (multiple == T.MultiplicativeIdentity) // No change...
		{
			return sourceNode.Root.Value ?? throw new NotSupportedException(CannotOperateNewNodeNullValue);
		}

		if (T.IsZero(multiple) || T.IsNaN(multiple)) // Neutralized.
		{
			return catalog.ApplyClone(sourceNode, _ =>
				catalog.GetConstant(multiple));
		}

		if (sourceNode.Value is not Product<T> p)
		{
			return catalog.ApplyClone(sourceNode,
				newNode => catalog.ProductOf(multiple,
					newNode.Value ?? throw new NotSupportedException(CannotOperateNewNodeNullValue)));
		}

		if (p.Children.OfType<IConstant<T>>().Any())
		{
			return catalog.ApplyClone(sourceNode, newNode =>
			{
				var n = newNode.Children.First(s => s.Value is IConstant<T>);
				var c = (IConstant<T>)(n.Value ?? throw new NotSupportedException(CannotOperateNewNodeNullValue));
				n.Value = catalog.ProductOfConstants(multiple, c);
			});
		}

		return catalog.TryAddConstant(sourceNode, multiple)!;
	}

	public static IEvaluate<T> MultiplyNodeDescendant<T>(
		this EvaluationCatalog<T> catalog,
		Node<IEvaluate<T>> sourceNode, int descendantIndex, T multiple)
		where T : notnull, INumber<T>
		=> catalog.MultiplyNode(sourceNode.GetDescendantsOfType().ElementAt(descendantIndex), multiple);

	public static Constant<T> GetMultiple<T, TParent>(
		this EvaluationCatalog<T> catalog,
		[DisallowNull] TParent n)
		where T : notnull, INumber<T>
		where TParent : IParent<IEvaluate<T>>
		=> catalog.ProductOfConstants(n.Children.OfType<IConstant<T>>());

	public static Constant<T> GetMultiple<T>(
		this EvaluationCatalog<T> catalog,
		IEvaluate<T>? node)
		where T : notnull, INumber<T>
		=> node is IParent<IEvaluate<T>> n ? catalog.GetMultiple(n) : catalog.GetConstant(T.MultiplicativeIdentity);

	/// <summary>
	/// Applies a multiple to any node.
	/// </summary>
	/// <param name="catalog">The catalog to use.</param>
	/// <param name="sourceNode">The node to multiply by.</param>
	/// <param name="delta">The value to multiply by.</param>
	/// <returns>The resultant root evaluation.</returns>
	public static IEvaluate<T> AdjustNodeMultiple<T>(
		this EvaluationCatalog<T> catalog,
		Node<IEvaluate<T>> sourceNode, T delta)
		where T : notnull, INumber<T>
	{
		catalog.ThrowIfNull();
		sourceNode.ThrowIfNull();
		Contract.EndContractBlock();

		if (T.IsZero(delta)) // No change... 
			return sourceNode.Root.Value ?? throw new NotSupportedException(CannotOperateNewNodeNullValue);

		if (sourceNode.Value is not Product<T> p)
			return MultiplyNode(catalog, sourceNode, delta + T.One);

		var multiple = catalog.GetMultiple(p);
		return multiple.Value switch
		{
			1 => catalog.MultiplyNode(sourceNode, delta + T.One),
			_ => catalog.ApplyClone(sourceNode, newNode =>
			{
				var constantNodes = newNode.Children.Where(s => s.Value is IConstant<T>).ToArray();
				constantNodes[0].Value = catalog.SumOfConstants(delta, multiple);

				for (var i = 1; i < constantNodes.Length; i++)
					newNode.Remove(constantNodes[i]);
			})
		};
	}

	public static IEvaluate<T> AdjustNodeMultipleOfDescendant<T>(
		this EvaluationCatalog<T> catalog,
		Node<IEvaluate<T>> sourceNode, int descendantIndex, T delta)
		where T : notnull, INumber<T>
		=> catalog.AdjustNodeMultiple(sourceNode.GetDescendantsOfType().ElementAt(descendantIndex), delta);
}
