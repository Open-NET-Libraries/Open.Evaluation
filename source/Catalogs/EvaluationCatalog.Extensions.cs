using Open.Evaluation.Arithmetic;
using Open.Evaluation.Core;
using Open.Hierarchy;
using System;
using System.Linq;

namespace Open.Evaluation.Catalogs
{
	public static class EvaluationCatalogExtensions
	{
		/// <summary>
		/// Applies a multiple to any node.
		/// </summary>
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

			if (multiple == 0 || double.IsNaN(multiple)) // Neustralized.
				return catalog.ApplyClone(sourceNode, newNode =>
				{
					newNode.Value = catalog.GetConstant(multiple);
				});

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
			else
			{
				return catalog.ApplyClone(sourceNode, newNode =>
				{
					var e = newNode.Value;
					newNode.Value = catalog.ProductOf(multiple, e);
				});
			}
		}


		/// <summary>
		/// Applies a multiple to any node.
		/// </summary>
		/// <param name="sourceNode">The node to multply by.</param>
		/// <param name="multiple">The value to multiply by.</param>
		/// <returns>The resultant root evaluation.</returns>
		public static IEvaluate<double> AdjustNodeMultiple(
			this EvaluationCatalog<double> catalog,
			Node<IEvaluate<double>> sourceNode, double delta)
		{
			if (sourceNode == null)
				throw new ArgumentNullException(nameof(sourceNode));

			if (delta == 0) // No change... 
				return sourceNode.Root.Value;

			if (sourceNode.Value is Product<double> p)
			{
				var multiple = catalog.ProductOfConstants(p.Children.OfType<IConstant<double>>());
				if(multiple.Value!=1)
				{
					return catalog.ApplyClone(sourceNode, newNode =>
					{
						var constantNodes = newNode.Children.Where(s=>s.Value is IConstant<double>).ToArray();
						constantNodes[0].Value = catalog.SumOfConstants(delta, multiple);

						for (var i = 1; i < constantNodes.Length; i++)
							newNode.Remove(constantNodes[i]);
					});
				}
			}
			

			return MultiplyNode(catalog, sourceNode, delta+1);

		}

	}
}
