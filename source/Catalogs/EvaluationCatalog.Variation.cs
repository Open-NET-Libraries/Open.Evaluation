using Open.Evaluation.Arithmetic;
using Open.Evaluation.Core;
using Open.Hierarchy;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading;

namespace Open.Evaluation.Catalogs;

using EvalDoubleVariationCatalog = EvaluationCatalog<double>.VariationCatalog;

public partial class EvaluationCatalog<T>
	where T : IComparable
{
	private VariationCatalog? _variation;
	public VariationCatalog Variation =>
		LazyInitializer.EnsureInitialized(ref _variation, () => new VariationCatalog(this))!;

	public class VariationCatalog : SubmoduleBase<EvaluationCatalog<T>>
	{
		internal VariationCatalog(EvaluationCatalog<T> source) : base(source)
		{
		}
	}
}

public static partial class EvaluationCatalogExtensions
{
	public static bool IsValidForRemoval<T>(this Node<IEvaluate<T>> gene, bool ifRoot = false)
		where T : struct, IComparable
	{
		if (gene == gene.Root) return ifRoot;
		// Validate worthyness.
		var parent = gene.Parent;
		Debug.Assert(parent is not null);

		switch (parent.Value)
		{
			case OperatorBase<T> _ when parent.Count < 2:
			case Exponent<T> _:
				return false;
		}

		// Search for potential futility...
		// Basically, if there is no dynamic nodes left after reduction then it's not worth removing.
		return !parent.Any(g => g != gene && g.Value is not IConstant)
			   && parent.IsValidForRemoval(true);
	}

	/// <summary>
	/// Removes a node from its parent.
	/// </summary>
	/// <param name="catalog">The catalog to use.</param>
	/// <param name="node">The node to remove from the tree.</param>
	/// <param name="newRoot">The resultant root node corrected by .FixHierarchy()</param>
	/// <returns>true if sucessful</returns>
	public static bool TryRemoveValid(
		this EvalDoubleVariationCatalog catalog,
		Node<IEvaluate<double>> node,
#if NETSTANDARD2_1_OR_GREATER
		[NotNullWhen(true)]
# endif
		out IEvaluate<double> newRoot)
	{
		Debug.Assert(catalog is not null);
		if (catalog is null) throw new ArgumentNullException(nameof(catalog));
		if (node is null) throw new ArgumentNullException(nameof(node));
		if (IsValidForRemoval(node))
		{
			newRoot = catalog.Catalog
				.RemoveNode(node.CloneTree())
				.Recycle()!;
			return true;
		}
		newRoot = default!;
		return false;
	}

	/// <summary>
	/// Removes a node from its parent.
	/// </summary>
	/// <param name="catalog">The catalog to use.</param>
	/// <param name="sourceNode">The root node to remove a descendant from.</param>
	/// <param name="descendantIndex">The index of the descendant in the heirarchy (breadth-first).</param>
	/// <param name="newRoot">The resultant root node corrected by .FixHierarchy()</param>
	/// <returns>true if sucessful</returns>
	public static bool TryRemoveValidAt(
		this EvalDoubleVariationCatalog catalog,
		Node<IEvaluate<double>> sourceNode,
		int descendantIndex,
		out IEvaluate<double> newRoot)
	{
		Debug.Assert(catalog is not null);
		if (catalog is null) throw new ArgumentNullException(nameof(catalog));
		if (sourceNode is null) throw new ArgumentNullException(nameof(sourceNode));

		return TryRemoveValid(
			catalog,
			sourceNode
				.GetDescendantsOfType()
				.ElementAt(descendantIndex),
			out newRoot);
	}

	static bool CheckPromoteChildrenValidity(
		IParent parent)
		// Validate worthyness.
		=> parent?.Children.Count == 1;

	public static IEvaluate<double>? PromoteChildren(
		this EvalDoubleVariationCatalog catalog,
		Node<IEvaluate<double>> node)
	{
		if (catalog is null) throw new ArgumentNullException(nameof(catalog));
		if (node is null) throw new ArgumentNullException(nameof(node));
		Contract.EndContractBlock();

		// Validate worthyness.
		if (!CheckPromoteChildrenValidity(node)) return null;

		return catalog.Catalog.ApplyClone(node,
			newNode => newNode.Children.Single().Value!);
	}

	// This should handle the case of demoting a function.
	public static IEvaluate<double>? PromoteChildrenAt(
		this EvalDoubleVariationCatalog catalog,
		Node<IEvaluate<double>> root, int descendantIndex)
	{
		if (catalog is null) throw new ArgumentNullException(nameof(catalog));
		if (root is null) throw new ArgumentNullException(nameof(root));
		Contract.EndContractBlock();

		return PromoteChildren(catalog,
			root.GetDescendantsOfType()
				.ElementAt(descendantIndex));
	}

	public static IEvaluate<double> ApplyFunction(
		this EvalDoubleVariationCatalog catalog,
		Node<IEvaluate<double>> node, char fn, IEnumerable<IEvaluate<double>> parameters)
	{
		if (catalog is null) throw new ArgumentNullException(nameof(catalog));
		if (node is null) throw new ArgumentNullException(nameof(node));
		Contract.EndContractBlock();

		if (!Registry.Arithmetic.Functions.Contains(fn))
			throw new ArgumentException("Invalid function operator.", nameof(fn));

		var c = catalog.Catalog;
		return c.ApplyClone(node, _ =>
			Registry.Arithmetic.GetFunction(c, fn, parameters.ToArray()));
	}

	public static IEvaluate<double>? ApplyRandomFunction(
		this EvalDoubleVariationCatalog catalog,
		Node<IEvaluate<double>> node)
	{
		if (catalog is null) throw new ArgumentNullException(nameof(catalog));
		if (node is null) throw new ArgumentNullException(nameof(node));
		Contract.EndContractBlock();

		var c = catalog.Catalog;
		var n = Registry.Arithmetic.GetRandomFunction(c, node.Value!);
		return n is null ? null : c.ApplyClone(node, _ => n);
	}

	public static IEvaluate<double> ApplyFunctionAt(
		this EvalDoubleVariationCatalog catalog,
		Node<IEvaluate<double>> root, int descendantIndex, char fn, IEnumerable<IEvaluate<double>> parameters)
	{
		if (catalog is null) throw new ArgumentNullException(nameof(catalog));
		if (root is null) throw new ArgumentNullException(nameof(root));
		Contract.EndContractBlock();

		return ApplyFunction(catalog,
			root.GetDescendantsOfType()
				.ElementAt(descendantIndex).Parent!, fn, parameters);
	}

	public static IEvaluate<double> IncreaseParameterExponents(
		this EvalDoubleVariationCatalog catalog,
		IEvaluate<double> root)
	{
		if (catalog is null) throw new ArgumentNullException(nameof(catalog));
		if (root is null) throw new ArgumentNullException(nameof(root));
		Contract.EndContractBlock();

		if (root is not IParent)
			return root;

		var cat = catalog.Catalog;
		var tree = cat.Factory.Map(root);
		foreach (var p in tree
			.GetDescendantsOfType()
			.Where(d => d.Value is IParameter<double>)
			.ToArray())
		{
			if (p.Parent is null)
			{
				Debugger.Break();
				continue; // Should never happen.
			}

			if (p.Parent.Value is Exponent<double> exponent && exponent.Power is IConstant<double> c)
			{
				var a = Math.Abs(c.Value);
				var direction = c.Value < 0 ? -1 : +1;
				var newValue = (a is > 0 and < 1)
					? (c.Value > 0 ? 1 : -1)
					: (c.Value + direction);

				p.Parent[0]
					= cat.Factory.GetNodeWithValue(cat.GetConstant(newValue));
			}
			else
			{
				p.Parent.Replace(p,
					cat.Factory.Map(cat.GetExponent(p.Value!, 2)));
			}
		}

		var pet = cat.FixHierarchy(tree).Recycle()!;
		tree.Recycle();

		return cat.TryGetReduced(pet, out var red) ? red : pet;
	}

	public static IEvaluate<TResult> FlattenProductofSums<TResult>(
		this EvaluationCatalog<TResult>.VariationCatalog catalog,
		IEvaluate<TResult> root)
		where TResult : struct, IComparable
	{
		if (catalog is null) throw new ArgumentNullException(nameof(catalog));
		if (root is null) throw new ArgumentNullException(nameof(root));
		Contract.EndContractBlock();

		var cat = catalog.Catalog;

	retry:
		if (root is not IParent)
			return root;

		var tree = cat.Factory.Map(root);
		var first = tree
			.GetNodesOfType()
			.FirstOrDefault(d => d.Value is Product<TResult> e
				&& e.Children.Length > 1 && e.Children.OfType<Sum<TResult>>().Any());

		if (first is null)
		{
			tree.Recycle();
			return root;
		}

		var product = (Product<TResult>)first.Value;
		var children = product.Children;
		var newChildren = children.ToList();
		var sums = newChildren.ExtractType<Sum<TResult>>();
		IEvaluate<TResult> productOfSum;
		if (sums.Count == 1)
		{
			var next = newChildren[0];
			newChildren.RemoveAt(0);
			productOfSum = cat.ProductOfSum(next, sums[0]);
		}
		else
		{
			productOfSum = cat.ProductOfSums(sums);
		}

		var oRoot = root;
		var replacment = newChildren.Count == 0 ? productOfSum : Product<TResult>.Create(cat, newChildren.Append(productOfSum));
		if (root == product)
		{
			root = replacment;
		}
		else
		{
			if (first.Parent is null) throw new Exception("Impossbile to replace.");
			first.Parent.Replace(first, cat.Factory.Map(replacment));
			var pet = cat.FixHierarchy(tree).Recycle()!;

			root = cat.TryGetReduced(pet, out var red) ? red : pet;
		}

		tree.Recycle();

		if (root == oRoot) return root;
		goto retry;
	}
}
