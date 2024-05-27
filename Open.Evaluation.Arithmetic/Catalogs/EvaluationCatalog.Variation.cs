namespace Open.Evaluation.Arithmetic;
public partial class EvaluationCatalog<T>
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

public static partial class CatalogExtensions
{
	public static bool IsValidForRemoval<T>(this Node<IEvaluate<T>> gene, bool ifRoot = false)
		where T : notnull, INumber<T>
	{
		if (gene == gene.Root) return ifRoot;
		// Validate worthiness.
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
	/// <returns>true if successful</returns>
	public static bool TryRemoveValid<T>(
		this EvaluationCatalog<T>.VariationCatalog catalog,
		Node<IEvaluate<T>> node,
		[NotNullWhen(true)] out IEvaluate<T> newRoot)
		where T : notnull, INumber<T>
	{
		Debug.Assert(catalog is not null);
		catalog.ThrowIfNull();
		node.ThrowIfNull();
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
	/// <param name="descendantIndex">The index of the descendant in the hierarchy (breadth-first).</param>
	/// <param name="newRoot">The resultant root node corrected by .FixHierarchy()</param>
	/// <returns>true if successful</returns>
	public static bool TryRemoveValidAt<T>(
		this EvaluationCatalog<T>.VariationCatalog catalog,
		Node<IEvaluate<T>> sourceNode,
		int descendantIndex,
		out IEvaluate<T> newRoot)
		where T : notnull, INumber<T>
	{
		Debug.Assert(catalog is not null);
		catalog.ThrowIfNull();
		sourceNode.ThrowIfNull();
		Contract.EndContractBlock();

		return TryRemoveValid(
			catalog,
			sourceNode
				.GetDescendantsOfType()
				.ElementAt(descendantIndex),
			out newRoot);
	}

	static bool CheckPromoteChildrenValidity(
		IParent parent)
		// Validate worthiness.
		=> parent?.Children.Count == 1;

	public static IEvaluate<T>? PromoteChildren<T>(
		this EvaluationCatalog<T>.VariationCatalog catalog,
		Node<IEvaluate<T>> node)
		where T : notnull, INumber<T>
	{
		catalog.ThrowIfNull();
		node.ThrowIfNull();
		Contract.EndContractBlock();

		// Validate worthiness.
		return CheckPromoteChildrenValidity(node)
			? catalog.Catalog.ApplyClone(node, newNode => newNode.Children.Single().Value!)
			: null;
	}

	// This should handle the case of demoting a function.
	public static IEvaluate<T>? PromoteChildrenAt<T>(
		this EvaluationCatalog<T>.VariationCatalog catalog,
		Node<IEvaluate<T>> root, int descendantIndex)
		where T : notnull, INumber<T>
	{
		catalog.ThrowIfNull();
		root.ThrowIfNull();
		Contract.EndContractBlock();

		return PromoteChildren(catalog,
			root.GetDescendantsOfType()
				.ElementAt(descendantIndex));
	}

	public static IEvaluate<T> ApplyFunction<T>(
		this EvaluationCatalog<T>.VariationCatalog catalog,
		Node<IEvaluate<T>> node, char fn, IEnumerable<IEvaluate<T>> parameters)
		where T : notnull, INumber<T>
	{
		catalog.ThrowIfNull();
		node.ThrowIfNull();
		Contract.EndContractBlock();

		if (!Registry.Functions.Contains(fn))
			throw new ArgumentException("Invalid function operator.", nameof(fn));

		var c = catalog.Catalog;
		return c.ApplyClone(node, _ =>
			Registry.GetFunction(c, fn, parameters.ToArray()));
	}

	public static IEvaluate<T>? ApplyRandomFunction<T>(
		this EvaluationCatalog<T>.VariationCatalog catalog,
		Node<IEvaluate<T>> node)
		where T : notnull, INumber<T>
	{
		catalog.ThrowIfNull();
		node.ThrowIfNull();
		Contract.EndContractBlock();

		var c = catalog.Catalog;
		var n = Registry.GetRandomFunction(c, node.Value!);
		return n is null ? null : c.ApplyClone(node, _ => n);
	}

	public static IEvaluate<T> ApplyFunctionAt<T>(
		this EvaluationCatalog<T>.VariationCatalog catalog,
		Node<IEvaluate<T>> root, int descendantIndex, char fn, IEnumerable<IEvaluate<T>> parameters)
		where T : notnull, INumber<T>
	{
		catalog.ThrowIfNull();
		root.ThrowIfNull();
		Contract.EndContractBlock();

		return ApplyFunction(catalog,
			root.GetDescendantsOfType()
				.ElementAt(descendantIndex).Parent!, fn, parameters);
	}

	public static IEvaluate<T> IncreaseParameterExponents<T>(
		this EvaluationCatalog<T>.VariationCatalog catalog,
		IEvaluate<T> root)
		where T : notnull, INumber<T>
	{
		catalog.ThrowIfNull();
		root.ThrowIfNull();
		Contract.EndContractBlock();

		if (root is not IParent)
			return root;

		var cat = catalog.Catalog;
		var tree = cat.Factory.Map(root);
		foreach (var p in tree
			.GetDescendantsOfType()
			.Where(d => d.Value is IParameter<T>)
			.ToArray())
		{
			if (p.Parent is null)
			{
				Debugger.Break();
				continue; // Should never happen.
			}

			if (p.Parent.Value is Exponent<T> exponent && exponent.Power is IConstant<T> c)
			{
				var a = T.Abs(c.Value);
				var direction = T.IsNegative(c.Value) ? -T.One : +T.One;
				var newValue = (a < T.One && a > T.Zero)
					? (T.IsPositive(c.Value) ? +T.One : -T.One)
					: (c.Value + direction);

				p.Parent[0]
					= cat.Factory.GetNodeWithValue(cat.GetConstant(newValue));
			}
			else
			{
				p.Parent.Replace(p,
					cat.Factory.Map(cat.GetExponent(p.Value, Value<T>.Two)));
			}
		}

		var pet = cat.FixHierarchy(tree).Recycle()!;
		tree.Recycle();

		return cat.TryGetReduced(pet, out var red) ? red : pet;
	}

	public static IEvaluate<TResult> FlattenProductofSums<TResult>(
		this EvaluationCatalog<TResult>.VariationCatalog catalog,
		IEvaluate<TResult> root)
		where TResult : notnull, INumber<TResult>
	{
		catalog.ThrowIfNull();
		root.ThrowIfNull();
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
			if (first.Parent is null) throw new NotSupportedException("Impossible to replace when first parent is null.");
			first.Parent.Replace(first, cat.Factory.Map(replacment));
			var pet = cat.FixHierarchy(tree).Recycle()!;

			root = cat.TryGetReduced(pet, out var red) ? red : pet;
		}

		tree.Recycle();

		if (root == oRoot) return root;
		goto retry;
	}
}
