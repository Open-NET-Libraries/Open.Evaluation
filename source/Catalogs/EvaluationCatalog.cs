using Open.Evaluation.Core;
using Open.Hierarchy;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Open.Evaluation.Catalogs
{
	public class EvaluationCatalog<T> : Catalog<IEvaluate<T>>
		where T : IComparable
	{
		public EvaluationCatalog()
		{
		}

		/// <summary>
		/// For any evaluation node, correct the hierarchy to match.
		/// 
		/// Uses .NewUsing methods to reconstruct the tree.
		/// </summary>
		/// <param name="target">The node tree to correct.</param>
		/// <returns>The updated tree.</returns>
		public Node<IEvaluate<T>> FixHierarchy(
			Node<IEvaluate<T>> target,
			bool operateDirectly = false)
		{
			if (!operateDirectly)
				target = Factory.Clone(target);

			var value = target.Value;
			// Does this node's value contain children?
			if (value is IParent<IEnumerable<IEvaluate<T>>> p)
			{
				var fixedChildren = target
					.Select(n =>
					{
						var f = FixHierarchy(n, true);
						var v = f.Value;
						if (f != n) Factory.Recycle(f); // Only the owner of the target node should do the recycling.
						return Register(v);
					}).ToArray();

				Node<IEvaluate<T>> node;

				if (value is IReproducable<IEnumerable<IEvaluate<T>>> r)
				{
					// This recursion technique will operate on the leaves of the tree first.
					node = Factory.Map(
						(IEvaluate<T>)r.NewUsing(
							(ICatalog<IEvaluate>)this,
							// Using new children... Rebuild using new structure and check for registration.
							fixedChildren
						)
					);
				}
				else if (value is IReproducable<(IEvaluate<T>, IEvaluate<T>)> e) // Functions, exponent, etc...
				{
					Debug.Assert(target.Children.Count == 2);
					node = Factory.Map(
						(IEvaluate<T>)e.NewUsing(
							(ICatalog<IEvaluate>)this,
							(fixedChildren[0], fixedChildren[1])
						)
					);

				}
				else
				{
					throw new Exception("Unknown IParent / IReproducable.");
				}

				target.Parent?.Replace(target, node);
				return node;

			}
			// No children? Then clear any child notes.
			else
			{
				target.Clear();

				var old = target.Value;
				var registered = Register(target.Value);
				if (old != registered)
					target.Value = registered;

				return target;
			}
		}

		/// <summary>
		/// Provides a cloned node (as part of a cloned tree) for the handler to operate on.
		/// </summary>
		/// <param name="sourceNode">The node to clone.</param>
		/// <param name="handler">the handler to pass the cloned node to.</param>
		/// <returns>The resultant value corrected by .FixHierarchy()</returns>
		public IEvaluate<T> ApplyClone(
			Node<IEvaluate<T>> sourceNode,
			Action<Node<IEvaluate<T>>> handler)
		{
			var newGene = Factory.CloneTree(sourceNode); // * new 1
			handler(newGene);
			var newRoot = FixHierarchy(newGene.Root); // * new 2
			var value = newRoot.Value;
			Factory.Recycle(newGene.Root); // * 1
			Factory.Recycle(newRoot); // * 2
			return value;
		}

		/// <summary>
		/// Removes a node from its parent.
		/// </summary>
		/// <param name="node">The node to remove from the tree.</param>
		/// <returns>The resultant root node corrected by .FixHierarchy()</returns>
		public Node<IEvaluate<T>> RemoveNode(
			Node<IEvaluate<T>> node)
		{
			if (node == null || node.Parent == null) return null;
			var root = node.Root;
			node.Parent.Remove(node);
			return FixHierarchy(root, true);
		}

		/// <summary>
		/// If possible, adds a constant to this node's children.
		/// If not possible, returns null.
		/// </summary>
		/// <param name="sourceNode">The node to attempt adding a constant to.</param>
		/// <returns>A new node within a new tree containing the updated evaluation.</returns>
		public IEvaluate<T> AddConstant(Node<IEvaluate<T>> sourceNode, T value)
			=> sourceNode.Value is IParent<IEvaluate<T>>
				? ApplyClone(
					sourceNode,
					newNode => newNode.Add(Factory.Map(this.GetConstant(value))))
				: null;

	}

	public class EvaluateDoubleCatalog : EvaluationCatalog<double>
	{

		public EvaluateDoubleCatalog()
		{
		}

	}

}
