using Open.Hierarchy;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using Throw;

namespace Open.Evaluation.Core;

public static class CatalogExtensions
{
	public static IEnumerable<T> Flatten<T>(
		this ICatalog<T> catalog,
		IEnumerable<T> source)
		where T : class, IEvaluate
	{
		return source is null
			? throw new ArgumentNullException(nameof(source))
			: FlattenCore(catalog, source);

		static IEnumerable<T> FlattenCore(ICatalog<T> catalog, IEnumerable<T> source)
		{
			foreach (var child in source)
			{
				var c = catalog.GetReduced(child);
				if (c is IParent<T> flat)
				{
					foreach (var sc in flat.Children)
						yield return sc;
				}
				else
				{
					yield return c;
				}
			}
		}
	}

	private const string ReturnedNull = "Returned null.";

	/// <summary>
	/// <para>For any evaluation node, correct the hierarchy to match.</para>
	/// <para>Uses .NewUsing methods to reconstruct the tree.</para>
	/// </summary>
	/// <param name="target">The node tree to correct.</param>
	/// <param name="operateDirectly">If true will modify the target instead of a clone.</param>
	/// <returns>The updated tree.</returns>
	public static Node<IEvaluate<T>> FixHierarchy<T>(
		this ICatalog<IEvaluate<T>> catalog,
		Node<IEvaluate<T>> target,
		bool operateDirectly = false)
		where T : notnull, IEquatable<T>, IComparable<T>
	{
		target.ThrowIfNull();
		Contract.Ensures(Contract.Result<Node<IEvaluate<T>>>() is not null);
		Contract.EndContractBlock();

		if (!operateDirectly)
			target = target.Clone();

		// Is the node unmapped (nothing changed) then return it.
		if (target.Unmapped)
			return target;

		var value = target.Value;
		// Does this node's value contain children?
		if (value is IParent<IEvaluate<T>>)
		{
			var fixedChildren = target.Children.ToArray()
				.Select(n =>
				{
					var f = catalog.FixHierarchy(n, true);
					var v = f.Value;
					Debug.Assert(v is not null);
					if (f != n) f.Recycle(); // Only the owner of the target node should do the recycling.
					return catalog.Register(v);
				}).ToArray();

			Node<IEvaluate<T>> node;

			switch (value)
			{
				case IReproducable<IEnumerable<IEvaluate<T>>, IEvaluate<T>> r:
					// This recursion technique will operate on the leaves of the tree first.
					node = target.Source.Map(
						r.NewUsing(
							catalog,
							// Using new children... Rebuild using new structure and check for registration.
							fixedChildren
						)
					);
					break;

				// Functions, exponent, etc...
				case IReproducable<(IEvaluate<T>, IEvaluate<T>), IEvaluate<T>> e:
					Debug.Assert(fixedChildren.Length > 1);
					node = target.Source.Map(
						e.NewUsing(
							catalog,
							(fixedChildren[0], fixedChildren[1])
						)
					);
					break;

				default:
					throw new NotSupportedException("Unknown IParent / IReproducable.");
			}

			target.Parent?.Replace(target, node);
			return node;
		}
		// else
		// No children? Then clear any child notes.

		target.Clear();

		var old = target.Value!;
		var registered = catalog.Register(old); // Will throw if old is null.
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
	public static IEvaluate<T> ApplyClone<T>(
		this ICatalog<IEvaluate<T>> catalog,
		Node<IEvaluate<T>> sourceNode,
		Action<Node<IEvaluate<T>>> clonedNodeHandler)
		where T : notnull, IEquatable<T>, IComparable<T>
	{
		var node = sourceNode.CloneTree(); // * new 1
		var root = node.Root;
		try
		{
			clonedNodeHandler(node);
			return catalog.FixHierarchy(root).Recycle()!;
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
	public static IEvaluate<T> ApplyClone<T>(
		this ICatalog<IEvaluate<T>> catalog,
		Node<IEvaluate<T>> sourceNode,
		Func<Node<IEvaluate<T>>, IEvaluate<T>> clonedNodeHandler)
		where T : notnull, IEquatable<T>, IComparable<T>
	{
		sourceNode.ThrowIfNull();
		clonedNodeHandler.ThrowIfNull();
		Contract.EndContractBlock();

		var node = sourceNode.CloneTree(); // * new 1
		var root = node.Root;
		var parent = node.Parent;
		try
		{
			var replacement = clonedNodeHandler(node) ?? throw new ArgumentException(ReturnedNull, nameof(clonedNodeHandler));
			if (parent is null) return replacement;

			var rn = sourceNode.Source.Map(replacement);
			try
			{
				parent.Replace(node, rn);
				node.Recycle();

				return catalog.FixHierarchy(root).Recycle()!;
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
	public static IEvaluate<T> ApplyClone<T, TParam>(
		this ICatalog<IEvaluate<T>> catalog,
		Node<IEvaluate<T>> sourceNode,
		TParam param, // allow pass-through of data to avoid allocation.
		Func<Node<IEvaluate<T>>, TParam, IEvaluate<T>> clonedNodeHandler)
		where T : notnull, IEquatable<T>, IComparable<T>
	{
		sourceNode.ThrowIfNull();
		clonedNodeHandler.ThrowIfNull();
		Contract.EndContractBlock();

		var node = sourceNode.CloneTree(); // * new 1
		var root = node.Root;
		var parent = node.Parent;
		try
		{
			var replacement = clonedNodeHandler(node, param)
				?? throw new ArgumentException(ReturnedNull, nameof(clonedNodeHandler));

			if (parent is null) return replacement;

			var rn = sourceNode.Source.Map(replacement);
			try
			{
				parent.Replace(node, rn);
				node.Recycle();

				return catalog.FixHierarchy(root).Recycle()!;
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
	public static IEvaluate<T> ApplyClone<T>(
		this ICatalog<IEvaluate<T>> catalog,
		Node<IEvaluate<T>> sourceNode,
		Func<Node<IEvaluate<T>>, Node<IEvaluate<T>>> clonedNodeHandler)
		where T : notnull, IEquatable<T>, IComparable<T>
	{
		var node = sourceNode.CloneTree(); // * new 1
		var root = node.Root;
		var parent = node.Parent;
		try
		{
			var replacement = clonedNodeHandler(node)
				?? throw new ArgumentException(ReturnedNull, nameof(clonedNodeHandler)); // * new 2

			try
			{
				if (parent is null)
					return catalog.FixHierarchy(replacement).Recycle()!;

				// ReSharper disable once InvertIf
				if (node != replacement)
				{
					parent.Replace(node, replacement);
					node.Recycle();
				}

				return catalog.FixHierarchy(root).Recycle()!;
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
	public static Node<IEvaluate<T>> RemoveNode<T>(
		this ICatalog<IEvaluate<T>> catalog,
		Node<IEvaluate<T>> node)
		where T : notnull, IEquatable<T>, IComparable<T>
	{
		node.ThrowIfNull();
		var parent = node.Parent
			?? throw new ArgumentException("node cannot be removed without a parent.", nameof(node));
		Contract.EndContractBlock();

		var root = node.Root;
		parent.Remove(node);
		return catalog.FixHierarchy(root, true);
	}

	/// <summary>
	/// Removes a node from a hierarchy by it's descendant index.
	/// </summary>
	/// <param name="sourceNode">The root node to remove a descendant from.</param>
	/// <param name="descendantIndex">The index of the descendant in the hierarchy (breadth-first).</param>
	/// <returns>The resultant root node corrected by .FixHierarchy()</returns>
	public static IEvaluate<T> RemoveDescendant<T>(
		this ICatalog<IEvaluate<T>> catalog,
		Node<IEvaluate<T>> sourceNode, int descendantIndex)
		where T : notnull, IEquatable<T>, IComparable<T>
		=> catalog.ApplyClone(sourceNode,
			newNode => catalog.RemoveNode(
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
	public static IEvaluate<T>? TryAddConstant<T>(
		this ICatalog<IEvaluate<T>> catalog,
		Node<IEvaluate<T>> sourceNode, T value)
		where T : notnull, IEquatable<T>, IComparable<T>
		=> sourceNode.Value is IParent<IEvaluate<T>>
			? catalog.ApplyClone(
				sourceNode,
				newNode => newNode.Add(sourceNode.Source.Map(catalog.GetConstant(value))))
			: null;
}