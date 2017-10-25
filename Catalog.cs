using Open.Evaluation.Hierarchy;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;

namespace Open.Evaluation
{
	public class Catalog<T>
		where T : IEvaluate
	{
		public Catalog()
		{
			Variations = new VariationCatalog(this);
			Mutations = new MutationCatalog(this);
		}

		readonly ConcurrentDictionary<string, T> Registry = new ConcurrentDictionary<string, T>();

		public TItem Register<TItem>(TItem item)
			where TItem : T
		{
			return (TItem)Registry.GetOrAdd(item.ToStringRepresentation(), item);
		}

		public TItem Register<TItem>(string id, Func<string, TItem> factory)
			where TItem : T
		{
			return (TItem)Registry.GetOrAdd(id, k =>
			{
				var e = factory(k);
				if (e.ToStringRepresentation() != k)
					throw new Exception("Provided ID does not match instance.ToStringRepresentation().");
				return e;
			});
		}

		public bool TryGetItem<TItem>(string id, out TItem item)
			where TItem : T
		{
			var result = Registry.TryGetValue(id, out T e);
			item = (TItem)e;
			return result;
		}

		public readonly Node<T>.Factory Factory = new Node<T>.Factory();

		public abstract class CatalogSubmodule
		{
			protected Catalog<T> Source;

			protected CatalogSubmodule(Catalog<T> source)
			{
				Source = source;
			}
		}

		public class VariationCatalog : CatalogSubmodule
		{
			internal VariationCatalog(Catalog<T> source) : base(source)
			{

			}


			public Genome AddConstant(Genome source, int geneIndex)
			{
				var tree =
				var gene = source.Genes[geneIndex] as Sum;
				// Basically, as a helper, if we find a sum gene that doesn't have any constants, then ok, add '1'.
				return gene != null && !gene.Children.OfType<Constant>().Any()
					? ApplyClone(source, geneIndex, g => ((Sum)g).Add(new Constant(1)))
					: null;
			}

			public Genome IncreaseMultipleMagnitude(Genome source, int geneIndex)
			{
				return ApplyClone(source, geneIndex, g =>
				{
					var absMultiple = Math.Abs(g.Modifier);
					g.Modifier += g.Modifier / absMultiple;
				});
			}

			public Genome ReduceMultipleMagnitude(Genome source, int geneIndex)
			{
				return ApplyClone(source, geneIndex, g =>
				{
					var absMultiple = Math.Abs(g.Modifier);
					g.Modifier -= g.Modifier / absMultiple;
				});
			}

			public static bool CheckRemovalValidity(Node<T> source, Node<T> gene)
			{
				if (gene == source.Root) return false;
				// Validate worthyness.
				var parent = gene.Parent;
				Debug.Assert(parent != null);

				// Search for potential futility...
				// Basically, if there is no dynamic genes left after reduction then it's not worth removing.
				if (!parent.Where(g => g != gene && !(g.Value is Constant)).Any())
				{
					return CheckRemovalValidity(source, parent);
				}

				return false;
			}

			public Genome RemoveGene(Node<IEvaluate> source, Node<IEvaluate> gene)
			{
				if (CheckRemovalValidity(source, gene))
				{
					return ApplyClone(source, geneIndex, (g, newGenome) =>
					{
						var parent = newGenome.FindParent(g);
						parent.Remove(g);
					});
				}
				return null;

			}
			public static Genome RemoveGene(Genome source, IGene gene)
			{
				return RemoveGene(source, source.Genes.IndexOf(gene));
			}

			public bool CheckPromoteChildrenValidity(Genome source, IGene gene)
			{
				// Validate worthyness.
				var op = gene as GeneNode;
				return op != null && op.Count == 1;
			}

			// This should handle the case of demoting a function.
			public Genome PromoteChildren(Genome source, int geneIndex)
			{
				// Validate worthyness.
				var gene = source.Genes[geneIndex];

				if (CheckPromoteChildrenValidity(source, gene))
				{
					return ApplyClone(source, geneIndex, (g, newGenome) =>
					{
						var op = (GeneNode)g;
						var child = op.Children.Single();
						op.Remove(child);
						newGenome.Replace(g, child);
					});
				}
				return null;

			}

			public Genome PromoteChildren(Genome source, IGene gene)
			{
				return PromoteChildren(source, source.Genes.IndexOf(gene));
			}

			public Genome ApplyFunction(Genome source, int geneIndex, char fn)
			{
				if (!Operators.Available.Functions.Contains(fn))
					throw new ArgumentException("Invalid function operator.", "fn");

				// Validate worthyness.
				return ApplyClone(source, geneIndex, (g, newGenome) =>
				{
					var newFn = Operators.New(fn);
					newGenome.Replace(g, newFn);
					newFn.Add(g);
				});

			}

			public Genome ApplyFunction(Genome source, IGene gene, char fn)
			{
				return ApplyFunction(source, source.Genes.IndexOf(gene), fn);
			}
		}

		public class MutationCatalog : CatalogSubmodule
		{
			internal MutationCatalog(Catalog<T> source) : base(source)
			{

			}

			public static Genome MutateSign(Genome source, IGene gene, int options = 3)
			{
				var isRoot = source.Root == gene;
				var parentIsSquareRoot = source.FindParent(gene) is SquareRootGene;
				return ApplyClone(source, gene, g =>
				{
					switch (RandomUtilities.Random.Next(options))
					{
						case 0:
							// Alter Sign
							g.Modifier *= -1;
							if (parentIsSquareRoot)
							{
								// Sorry, not gonna mess with unreal (sqrt neg numbers yet).
								if (RandomUtilities.Random.Next(2) == 0)
									goto case 1;
								else
									goto case 2;
							}
							break;
						case 1:
							// Don't zero the root or make the internal multiple negative.
							if (isRoot && g.Modifier == +1 || parentIsSquareRoot && g.Modifier <= 0)
								goto case 2;
							// Decrease multiple.
							g.Modifier -= 1;
							break;
						case 2:
							// Don't zero the root. (makes no sense)
							if (isRoot && g.Modifier == -1)
								goto case 1;
							// Increase multiple.
							g.Modifier += 1;
							break;

					}
				});
			}

			public static Genome MutateParameter(Genome source, Parameter gene)
			{
				var inputParamCount = source.Genes.OfType<Parameter>().GroupBy(p => p.ToString()).Count();
				return ApplyClone(source, gene, (g, newGenome) =>
				{
					var parameter = (Parameter)g;
					var nextParameter = RandomUtilities.NextRandomIntegerExcluding(inputParamCount + 1, parameter.ID);
					newGenome.Replace(g, GetParameterGene(nextParameter, parameter.Modifier));
				});
			}

			public static Genome ChangeOperation(Genome source, IOperator gene)
			{
				bool isFn = gene is IFunction;
				if (isFn)
				{
					// Functions with no other options?
					if (Operators.Available.Functions.Count < 2)
						return null;
				}
				else
				{
					// Never will happen, but logic states that this is needed.
					if (Operators.Available.Operators.Count < 2)
						return null;
				}

				return ApplyClone(source, gene, (g, newGenome) =>
				{
					var og = (IOperator)g;
					IOperator replacement = isFn
						? Operators.GetRandomFunctionGene(og.Operator)
						: Operators.GetRandomOperationGene(og.Operator);
					replacement.AddThese(og.Children);
					og.Clear();
					newGenome.Replace(g, replacement);
				});
			}

			public static Genome AddParameter(Genome source, IOperator gene)
			{
				bool isFn = gene is IFunction;
				if (isFn)
				{
					// Functions with no other options?
					if (gene is SquareRootGene || gene is DivisionGene)
						return null;
				}

				var inputParamCount = source.Genes.OfType<Parameter>().GroupBy(p => p.ToString()).Count();
				return ApplyClone(source, gene, (g, newGenome) =>
				{
					var og = (IOperator)g;
					og.Add(GetParameterGene(RandomUtilities.Random.Next(inputParamCount + 1)));
				});
			}

			public static Genome BranchOperation(Genome source, IOperator gene)
			{
				var inputParamCount = source.Genes.OfType<Parameter>().GroupBy(p => p.ToString()).Count();
				return ApplyClone(source, gene, (g, newGenome) =>
				{
					var n = GetParameterGene(RandomUtilities.Random.Next(inputParamCount));
					var newOp = Operators.GetRandomOperationGene();

					if (gene is IFunction || RandomUtilities.Random.Next(4) == 0)
					{
						var index = RandomUtilities.Random.Next(2);
						if (index == 1)
						{
							newOp.Add(n);
							newOp.Add(g);
						}
						else
						{
							newOp.Add(g);
							newOp.Add(n);
						}
						newGenome.Replace(g, newOp);
					}
					else
					{
						newOp.Add(n);
						// Useless to divide a param by itself, avoid...
						newOp.Add(GetParameterGene(RandomUtilities.Random.Next(inputParamCount)));

						((IOperator)g).Add(newOp);
					}

				});
			}

			public static Genome Square(Genome source, IGene gene)
			{
				if (source.FindParent(gene) is SquareRootGene)
					return null;

				return ApplyClone(source, gene, (g, newGenome) =>
				{
					var newFn = new ProductGene(g.Modifier);
					g.Modifier = 1;
					newFn.Add(g);
					newFn.Add(g.Clone());
					newGenome.Replace(g, newFn);
				});
			}
		}

		public readonly VariationCatalog Variations;
		public readonly MutationCatalog Mutations;

	
	}

	public class Catalog : Catalog<IEvaluate<double>>
	{

	}

	public static class CatalogExtensions
	{

		/// <summary>
		/// For any evaluation node, correct the hierarchy to match.
		/// </summary>
		/// <param name="target">The node tree to correct.</param>
		/// <returns>The updated tree.</returns>
		public static Node<T> FixHierarchy<T>(
			this Catalog<T> catalog,
			Node<T> target)
			where T : IEvaluate
		{
			// Does this node's value contain children?
			if (target.Value is IReproducable r)
			{
				var factory = catalog.Factory;
				// This recursion technique will operate on the leaves of the tree first.
				var node = factory.Map(
					(T)r.CreateNewFrom(
						r.ReproductionParam,
						// Using new children... Rebuild using new structure and check for registration.
						target.Select(n => (IEvaluate)catalog.Register(catalog.FixHierarchy(n).Value))
					)
				);
				node.Parent = target.Parent;
				return node;
			}
			// No children? Then clear any child notes.
			else
			{
				if (target.Count != 0)
					target.Children.Clear();

				var old = target.Value;
				var registered = catalog.Register(target.Value);
				if (!old.Equals(registered))
					target.Value = registered;

				return target;
			}
		}

		public static T ApplyClone<T>(
			this Catalog<T> catalog,
			Node<T> sourceGene,
			Action<Node<T>> handler)

			where T : IEvaluate
		{
			var gene = catalog.Factory.CloneTree(sourceGene);
			handler(gene);
			return catalog.FixHierarchy(gene.Root).Value;
		}
	}
}
