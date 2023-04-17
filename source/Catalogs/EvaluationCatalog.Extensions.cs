using Open.Evaluation.Arithmetic;
using Open.Evaluation.Core;
using Open.Hierarchy;

namespace Open.Evaluation.Catalogs;

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
	public static IEvaluate<double> MultiplyNode(
		this EvaluationCatalog<double> catalog,
		Node<IEvaluate<double>> sourceNode, double multiple)
	{
		catalog.ThrowIfNull();
		sourceNode.ThrowIfNull();
		Contract.EndContractBlock();

		if (multiple == 1) // No change...
		{
			return sourceNode.Root.Value ?? throw new NotSupportedException(CannotOperateNewNodeNullValue);
		}

		if (multiple == 0 || double.IsNaN(multiple)) // Neutralized.
		{
			return catalog.ApplyClone(sourceNode, _ =>
				catalog.GetConstant(multiple));
		}

#pragma warning disable IDE0046 // Convert to conditional expression
		if (sourceNode.Value is not Product<double> p)
		{
			return catalog.ApplyClone(sourceNode,
				newNode => catalog.ProductOf(multiple, newNode.Value ?? throw new NotSupportedException(CannotOperateNewNodeNullValue)));
		}
#pragma warning restore IDE0046 // Convert to conditional expression

		return p.Children.OfType<IConstant<double>>().Any()
			? catalog.ApplyClone(sourceNode, newNode =>
			{
				var n = newNode.Children.First(s => s.Value is IConstant<double>);
				var c = (IConstant<double>)(n.Value ?? throw new NotSupportedException(CannotOperateNewNodeNullValue));
				n.Value = catalog.ProductOfConstants(multiple, c);
			})
			: catalog.TryAddConstant(sourceNode, multiple)!;
	}

	public static IEvaluate<double> MultiplyNodeDescendant(
		this EvaluationCatalog<double> catalog,
		Node<IEvaluate<double>> sourceNode, int descendantIndex, double multiple)
		=> catalog.MultiplyNode(sourceNode.GetDescendantsOfType().ElementAt(descendantIndex), multiple);

	public static Constant<double> GetMultiple<TParent>(this EvaluationCatalog<double> catalog, TParent n)
		where TParent : IParent<IEvaluate<double>>
		=> catalog.ProductOfConstants(n.Children.OfType<IConstant<double>>());

	public static Constant<double> GetMultiple(this EvaluationCatalog<double> catalog, IEvaluate<double>? node)
		=> node is IParent<IEvaluate<double>> n ? catalog.GetMultiple(n) : catalog.GetConstant(1);

	/// <summary>
	/// Applies a multiple to any node.
	/// </summary>
	/// <param name="catalog">The catalog to use.</param>
	/// <param name="sourceNode">The node to multiply by.</param>
	/// <param name="delta">The value to multiply by.</param>
	/// <returns>The resultant root evaluation.</returns>
	public static IEvaluate<double> AdjustNodeMultiple(
		this EvaluationCatalog<double> catalog,
		Node<IEvaluate<double>> sourceNode, double delta)
	{
		catalog.ThrowIfNull();
		sourceNode.ThrowIfNull();
		Contract.EndContractBlock();

		if (delta == 0) // No change... 
			return sourceNode.Root.Value ?? throw new NotSupportedException(CannotOperateNewNodeNullValue);

		if (sourceNode.Value is not Product<double> p)
			return MultiplyNode(catalog, sourceNode, delta + 1);

		var multiple = catalog.GetMultiple(p);
		return multiple.Value switch
		{
			1 => catalog.MultiplyNode(sourceNode, delta + 1),
			_ => catalog.ApplyClone(sourceNode, newNode =>
			{
				var constantNodes = newNode.Children.Where(s => s.Value is IConstant<double>).ToArray();
				constantNodes[0].Value = catalog.SumOfConstants(delta, multiple);

				for (var i = 1; i < constantNodes.Length; i++)
					newNode.Remove(constantNodes[i]);
			})
		};
	}

	public static IEvaluate<double> AdjustNodeMultipleOfDescendant(
		this EvaluationCatalog<double> catalog,
		Node<IEvaluate<double>> sourceNode, int descendantIndex, double delta)
		=> catalog.AdjustNodeMultiple(sourceNode.GetDescendantsOfType().ElementAt(descendantIndex), delta);
}
