using Open.Evaluation.Arithmetic;
using Open.Evaluation.Core;
using Open.Hierarchy;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Open.Evaluation.Catalogs
{
	[SuppressMessage("ReSharper", "CompareOfFloatsByEqualityOperator")]
	public static partial class EvaluationCatalogExtensions
	{
		/// <summary>
		/// Applies a multiple to any node.
		/// </summary>
		/// <param name="catalog">The catalog to use.</param>
		/// <param name="sourceNode">The node to multply by.</param>
		/// <param name="multiple">The value to multiply by.</param>
		/// <returns>The resultant root evaluation.</returns>
		public static IEvaluate<double> MultiplyNode(
			this EvaluationCatalog<double> catalog,
			Node<IEvaluate<double>> sourceNode, double multiple)
		{
			if (sourceNode == null)
				throw new ArgumentNullException(nameof(sourceNode));

			if (multiple == 1) // No change...
				return sourceNode.Root.Value;

			if (multiple == 0 || double.IsNaN(multiple)) // Neutralized.
			{
				return catalog.ApplyClone(sourceNode, newNode =>
					catalog.GetConstant(multiple));
			}

			if (sourceNode.Value is Product<double> p)
			{
				return p.Children.OfType<IConstant<double>>().Any()
					? catalog.ApplyClone(sourceNode, newNode =>
					{
						var n = newNode.Children.First(s => s.Value is IConstant<double>);
						var c = (IConstant<double>)n.Value;
						n.Value = catalog.ProductOfConstants(multiple, c);
					})
					: catalog.AddConstant(sourceNode, multiple);
			}

			return catalog.ApplyClone(sourceNode,
				newNode => catalog.ProductOf(multiple, newNode.Value));
		}

		public static IEvaluate<double> MultiplyNodeDescendant(
			this EvaluationCatalog<double> catalog,
			Node<IEvaluate<double>> sourceNode, int descendantIndex, double multiple)
			=> catalog.MultiplyNode(sourceNode.GetDescendantsOfType().ElementAt(descendantIndex), multiple);

		public static Constant<double> GetMultiple<TParent>(this EvaluationCatalog<double> catalog, TParent n)
			where TParent : IParent<IEvaluate<double>>
			=> catalog.ProductOfConstants(n.Children.OfType<IConstant<double>>());

		public static Constant<double> GetMultiple(this EvaluationCatalog<double> catalog, IEvaluate<double> node)
			=> node is IParent<IEvaluate<double>> n ? catalog.GetMultiple(n) : catalog.GetConstant(1);

		/// <summary>
		/// Applies a multiple to any node.
		/// </summary>
		/// <param name="catalog">The catalog to use.</param>
		/// <param name="sourceNode">The node to multply by.</param>
		/// <param name="delta">The value to multiply by.</param>
		/// <returns>The resultant root evaluation.</returns>
		public static IEvaluate<double> AdjustNodeMultiple(
			this EvaluationCatalog<double> catalog,
			Node<IEvaluate<double>> sourceNode, double delta)
		{
			if (sourceNode == null)
				throw new ArgumentNullException(nameof(sourceNode));

			if (delta == 0) // No change... 
				return sourceNode.Root.Value;

			if (!(sourceNode.Value is Product<double> p))
				return MultiplyNode(catalog, sourceNode, delta + 1);

			var multiple = catalog.GetMultiple(p);
			if (multiple.Value == 1)
				return MultiplyNode(catalog, sourceNode, delta + 1);

			return catalog.ApplyClone(sourceNode, newNode =>
			{
				var constantNodes = newNode.Children.Where(s => s.Value is IConstant<double>).ToArray();
				constantNodes[0].Value = catalog.SumOfConstants(delta, multiple);

				for (var i = 1; i < constantNodes.Length; i++)
					newNode.Remove(constantNodes[i]);
			});

		}


		public static IEvaluate<double> AdjustNodeMultipleOfDescendant(
			this EvaluationCatalog<double> catalog,
			Node<IEvaluate<double>> sourceNode, int descendantIndex, double delta)
			=> catalog.AdjustNodeMultiple(sourceNode.GetDescendantsOfType().ElementAt(descendantIndex), delta);
	}
}
