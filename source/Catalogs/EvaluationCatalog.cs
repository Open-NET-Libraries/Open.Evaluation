using Open.Evaluation.Core;
using Open.Hierarchy;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Open.Evaluation.Catalogs
{
	[SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
	[SuppressMessage("ReSharper", "UnusedMethodReturnValue.Global")]
	public partial class EvaluationCatalog<T> : Catalog<IEvaluate<T>>
		where T : IComparable
	{

		/// <summary>
		/// For any evaluation node, correct the hierarchy to match.
		/// 
		/// Uses .NewUsing methods to reconstruct the tree.
		/// </summary>
		/// <param name="target">The node tree to correct.</param>
		/// <param name="operateDirectly">If true will modify the target instead of a clone.</param>
		/// <returns>The updated tree.</returns>
		public Node<IEvaluate<T>> FixHierarchy(
			Node<IEvaluate<T>> target,
			bool operateDirectly = false)
		{
			if (!operateDirectly)
				target = target.Clone();

			// Is the node unmapped (nothing changed) then return it.
			if (target.Unmapped)
				return target;

			var value = target.Value;
			// Does this node's value contain children?
			if (value is IParent<IEnumerable<IEvaluate<T>>>)
			{
				var fixedChildren = target
					.Select(n =>
					{
						var f = FixHierarchy(n, true);
						var v = f.Value;
						if (f != n) f.Recycle(); // Only the owner of the target node should do the recycling.
						return Register(v);
					}).ToArray();

				Node<IEvaluate<T>> node;

				switch (value)
				{
					case IReproducable<IEnumerable<IEvaluate<T>>> r:
						// This recursion technique will operate on the leaves of the tree first.
						node = Factory.Map(
							(IEvaluate<T>)r.NewUsing(
								(ICatalog<IEvaluate>)this,
								// Using new children... Rebuild using new structure and check for registration.
								fixedChildren
							)
						);
						break;

					// Functions, exponent, etc...
					case IReproducable<(IEvaluate<T>, IEvaluate<T>)> e:
						Debug.Assert(target.Children.Count == 2);
						node = Factory.Map(
							(IEvaluate<T>)e.NewUsing(
								(ICatalog<IEvaluate>)this,
								(fixedChildren[0], fixedChildren[1])
							)
						);
						break;

					default:
						throw new Exception("Unknown IParent / IReproducable.");
				}

				target.Parent?.Replace(target, node);
				return node;

			}
			// else
			// No children? Then clear any child notes.

			target.Clear();

			var old = target.Value;
			var registered = Register(target.Value);
			if (old != registered)
				target.Value = registered;

			return target;
		}

		/// <summary>
		/// Provides a cloned node (as part of a cloned tree) for the handler to operate on.
		/// </summary>
		/// <param name="sourceNode">The node to clone.</param>
		/// <param name="clonedNodeHandler">the handler to pass the cloned node to.</param>
		/// <returns>The resultant root evaluation corrected by .FixHierarchy()</returns>
		public IEvaluate<T> ApplyClone(
			Node<IEvaluate<T>> sourceNode,
			Action<Node<IEvaluate<T>>> clonedNodeHandler)
		{
			var node = sourceNode.CloneTree(); // * new 1
			var root = node.Root;
			try
			{
				clonedNodeHandler(node);
				return FixHierarchy(root)
					.Recycle();
			}
			finally
			{
				root.Recycle(); // * 1
			}
		}


		/// <summary>
		/// Provides a cloned node (as part of a cloned tree) for the handler to operate on.
		/// </summary>
		/// <param name="sourceNode">The node to clone.</param>
		/// <param name="clonedNodeHandler">the handler to pass the cloned node to.</param>
		/// <returns>The resultant root evaluation corrected by .FixHierarchy()</returns>
		public IEvaluate<T> ApplyClone(
			Node<IEvaluate<T>> sourceNode,
			Func<Node<IEvaluate<T>>, IEvaluate<T>> clonedNodeHandler)
		{
			var node = sourceNode.CloneTree(); // * new 1
			var root = node.Root;
			var parent = node.Parent;
			try
			{
				var replacement = clonedNodeHandler(node);
				if (parent == null) return replacement;

				var rn = Factory.Map(replacement);
				try
				{
					parent.Replace(node, rn);
					node.Recycle();

					return FixHierarchy(root)
						.Recycle();
				}
				finally
				{

					rn.Recycle();
				}
			}
			finally
			{
				root.Recycle(); // * 1
			}
		}

		/// <summary>
		/// Provides a cloned node (as part of a cloned tree) for the handler to operate on.
		/// </summary>
		/// <param name="sourceNode">The node to clone.</param>
		/// <param name="clonedNodeHandler">the handler to pass the cloned node to.</param>
		/// <returns>The resultant root evaluation corrected by .FixHierarchy()</returns>
		public IEvaluate<T> ApplyClone(
			Node<IEvaluate<T>> sourceNode,
			Func<Node<IEvaluate<T>>, Node<IEvaluate<T>>> clonedNodeHandler)
		{
			var node = sourceNode.CloneTree(); // * new 1
			var root = node.Root;
			var parent = node.Parent;
			try
			{
				var replacement = clonedNodeHandler(node); // * new 2
				try
				{
					if (parent == null)
						return FixHierarchy(replacement)
							.Recycle();

					// ReSharper disable once InvertIf
					if (node != replacement)
					{
						parent.Replace(node, replacement);
						node.Recycle();
					}

					return FixHierarchy(root)
						.Recycle();
				}
				finally
				{
					replacement.Recycle(); // * 2
				}
			}
			finally
			{
				root.Recycle(); // * 1
			}
		}

		/// <summary>
		/// Removes a node from its parent.
		/// </summary>
		/// <param name="node">The node to remove from the tree.</param>
		/// <returns>The resultant root node corrected by .FixHierarchy()</returns>
		public Node<IEvaluate<T>> RemoveNode(
			Node<IEvaluate<T>> node)
		{
			if (node?.Parent == null) return null;
			var root = node.Root;
			node.Parent.Remove(node);
			return FixHierarchy(root, true);
		}

		/// <summary>
		/// Removes a node from a hierarchy by it's descendant index.
		/// </summary>
		/// <param name="sourceNode">The root node to remove a descendant from.</param>
		/// <param name="descendantIndex">The index of the descendant in the heirarchy (breadth-first).</param>
		/// <returns>The resultant root node corrected by .FixHierarchy()</returns>
		public IEvaluate<T> RemoveDescendantAt(
			Node<IEvaluate<T>> sourceNode, int descendantIndex)
			=> ApplyClone(sourceNode,
				newNode => RemoveNode(
					newNode
						.GetDescendantsOfType()
						.ElementAt(descendantIndex)));

		/// <summary>
		/// If possible, adds a constant to this node's children.
		/// If not possible, returns null.
		/// </summary>
		/// <param name="sourceNode">The node to attempt adding a constant to.</param>
		/// <param name="value">The constant value to add.</param>
		/// <returns>A new node within a new tree containing the updated evaluation.</returns>
		public IEvaluate<T> AddConstant(Node<IEvaluate<T>> sourceNode, T value)
			=> sourceNode.Value is IParent<IEvaluate<T>>
				? ApplyClone(
					sourceNode,
					newNode => newNode.Add(Factory.Map(this.GetConstant(value))))
				: null;


	}
}
