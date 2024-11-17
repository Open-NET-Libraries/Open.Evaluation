using Open.RandomizationExtensions;

namespace Open.Evaluation.Arithmetic;

public static partial class CatalogExtensions
{
	const string CannotOperatePowerNullValue = "Cannot operate when the power.Value is null.";

	public static IEvaluate<T> MutateSign<T>(
		this EvaluationCatalog<T>.MutationCatalog catalog,
		Node<IEvaluate<T>> node, byte options = 3)
		where T : notnull, INumber<T>
	{
		catalog.ThrowIfNull();
		node.ThrowIfNull();
		if (options > 3) throw new ArgumentOutOfRangeException(nameof(options));
		Contract.EndContractBlock();

        Node<IEvaluate<T>> n = node;
        bool isRoot = n == n.Root;
		Debug.Assert(!isRoot || n.Parent is null);
		// ReSharper disable once ImplicitlyCapturedClosure
		bool parentIsSquareRoot() => !isRoot && n.Parent?.Value is Exponent<T> ex && ex.IsSquareRoot();

		// ReSharper disable once AccessToModifiedClosure
		Lazy<Constant<T>> modifier = new(() => catalog.Catalog.GetMultiple(n.Value));

		try
		{
			switch (Randomizer.Random.Next(options))
			{
				case 0:
                    // Alter Sign
                    IEvaluate<T> result = catalog.Catalog.MultiplyNode(n, -T.One);

					// Sorry, not gonna mess with unreal (sqrt neg numbers yet).
					if (!parentIsSquareRoot()) return result;

					n = node.Source.Map(result);
					if (Randomizer.Random.Next(2) == 0)
						goto case 1;

					goto case 2;

				case 1:
					// Don't zero the root or make the internal multiple negative.
					if (isRoot && modifier.Value == T.One || parentIsSquareRoot() && modifier.Value <= T.Zero)
						goto case 2;

					// Decrease multiple.
					return catalog.Catalog.AdjustNodeMultiple(n, -T.One);

				case 2:
					// Don't zero the root. (makes no sense)
					if (isRoot && modifier.Value == T.One)
						goto case 1;
					// Increase multiple.
					return catalog.Catalog.AdjustNodeMultiple(n, +T.One);
			}
		}
		finally
		{
			if (n != node) n.Recycle();
		}

		throw new ArgumentOutOfRangeException(nameof(options));
	}

	public static IEvaluate<T> MutateParameter<T>(
		this EvaluationCatalog<T>.MutationCatalog catalog,
		Node<IEvaluate<T>> node)
		where T : notnull, INumber<T>
	{
		ArgumentNullException.ThrowIfNull(catalog);
		if (node?.Value is null)
			throw new ArgumentException("No node value.", nameof(node));
		if (node.Value is not IParameter p)
			throw new ArgumentException("Does not contain a Parameter.", nameof(node));
		Contract.EndContractBlock();

		return catalog.Catalog.ApplyClone(node, _ =>
		{
            IEvaluate<T> rv = node.Root.Value;
            int nextParameter = Randomizer.Random.NextExcluding(
				p == rv
					? p.Id
					: ((IParent)rv!).GetDescendants().OfType<IParameter>().Distinct().Count()
						+ (p.Id == 0 ? 1 : Randomizer.Random.Next(2)) /* Increase the possibility of parameter ID decrease vs increase */,
				p.Id);

			return catalog.Catalog.GetParameter(nextParameter);
		});
	}

	public static IEvaluate<T>? ChangeOperation<T>(
		this EvaluationCatalog<T>.MutationCatalog catalog,
		Node<IEvaluate<T>> node)
		where T : notnull, INumber<T>
	{
		ArgumentNullException.ThrowIfNull(catalog);
		node.ThrowIfNull();

		if (node.Value is not IOperator<T> o)
			throw new ArgumentException("Does not contain an Operation.", nameof(node));

        Symbol symbol = o.Symbol;
        bool isFn = Registry.Functions.Contains(symbol);
		if (isFn)
		{
			// Functions with no other options?
			if (Registry.Functions.Length < 2)
			{
				if (node.Count < 2)
					return null;
				isFn = false;
			}
		}

		if (!isFn)
		{
			// Never will happen, but logic states that this is needed.
			if (Registry.Operators.Length < 2)
				return null;
		}

        EvaluationCatalog<T> c = catalog.Catalog;
		return c.ApplyClone(node, _ => isFn
			? Registry.GetRandomFunction(c, o.Children.ToArray(), symbol)!
			: Registry.GetRandomOperator(c, o.Children, symbol)!);
	}

	public static IEvaluate<T>? AddParameter<T>(
		this EvaluationCatalog<T>.MutationCatalog catalog,
		Node<IEvaluate<T>> node)
		where T: notnull, INumber<T>
	{
		catalog.ThrowIfNull();
		ArgumentNullException.ThrowIfNull(node);
		Contract.EndContractBlock();

		return node.Value switch
		{
			Exponent<double> _ => null,

			IParent p => catalog.Catalog.ApplyClone(node,
				newNode => newNode.AddValue(catalog.Catalog.GetParameter(
					Randomizer.Random.Next(
						p.GetDescendants().OfType<IParameter>().Distinct().Count() + 1)))),

			_ => throw new ArgumentException("Invalid node type for adding a parameter.", nameof(node)),
		};
	}

	public static IEvaluate<T> BranchOperation<T>(
		this EvaluationCatalog<T>.MutationCatalog catalog,
		Node<IEvaluate<T>> node)
		where T : notnull, INumber<T>
	{
		catalog.ThrowIfNull();
		ArgumentNullException.ThrowIfNull(node);
		Contract.EndContractBlock();

		return catalog.Catalog.ApplyClone(node, (catalog, node), (newNode, param) =>
		{
			(EvaluationCatalog<T>.MutationCatalog catalog, Node<IEvaluate<T>> node) = param;
            IEvaluate<T> rv = node.Root.Value;
            int inputParamCount = rv is IParent p
                ? p.GetDescendants().OfType<IParameter>().Distinct().Count()
                : rv is IParameter ? 1 : 0;
            IParameter<T> parameter = catalog.Catalog.GetParameter(Randomizer.Random.Next(inputParamCount));
			IEvaluate<T>[] children;

            IEvaluate<T> nv = newNode.Value ?? throw new NotSupportedException(CannotOperateNewNodeNullValue);
			children
				= newNode.Value is IFunction<T> || Randomizer.Random.Next(4) == 0
				? Randomizer.Random.Next(2) == 1
					? [parameter, nv]
					: [nv, parameter]
				: [parameter, nv];

			return Registry.GetRandomOperator(catalog, children)!; // Will throw in ApplyClone if null.
		});
	}

	public static IEvaluate<T> AdjustExponent<T>(
		this EvaluationCatalog<T>.MutationCatalog catalog,
		Node<IEvaluate<T>> node, T value)
		where T : notnull, INumber<T>
	{
		catalog.ThrowIfNull();
		ArgumentNullException.ThrowIfNull(node);
		if (value == T.Zero) throw new ArgumentException("A value of zero will have no effect.", nameof(value));
		Contract.EndContractBlock();

		return node.Value is Exponent<T>
			? catalog.Catalog.ApplyClone(node, newNode =>
			{
                Node<IEvaluate<T>> power = newNode.Children[1];
				newNode.Replace(power,
					node.Source.Map(catalog.Catalog.SumOf(in value, power.Value ?? throw new NotSupportedException(CannotOperatePowerNullValue))));
			})
			: catalog.Catalog.ApplyClone(node, newNode =>
					catalog.Catalog.GetExponent(newNode.Value ?? throw new NotSupportedException(CannotOperateNewNodeNullValue), T.One + value));
	}

	public static IEvaluate<T> Square<T>(
		this EvaluationCatalog<T>.MutationCatalog catalog,
		Node<IEvaluate<T>> node)
		where T : notnull, INumber<T>
	{
		catalog.ThrowIfNull();
		ArgumentNullException.ThrowIfNull(node);
		Contract.EndContractBlock();

		return node.Value is Exponent<T>
			? catalog.Catalog.ApplyClone(node, newNode =>
			{
                Node<IEvaluate<T>> power = newNode.Children[1];
				newNode.Replace(power,
					node.Source.Map(catalog.Catalog.ProductOf(Value<T>.Two, power.Value ?? throw new NotSupportedException(CannotOperatePowerNullValue))));
			})
			: catalog.Catalog.ApplyClone(node, newNode =>
				catalog.Catalog.GetExponent(newNode.Value ?? throw new NotSupportedException(CannotOperateNewNodeNullValue), Value<T>.Two));
	}
}
