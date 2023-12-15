using Open.Evaluation.Core;
using Open.Hierarchy;
using Throw;

namespace Open.Evaluation.Hierarchy;

public static class Extensions
{
	public static bool AreChildrenAligned(this Node<IEvaluate> target)
		=> target.ThrowIfNull().Value switch
		{
			IParent parent => target.SequenceEqual(parent.Children),
			_ => target.Count == 0 // Value does not have children? Return true only if this has no children.
		};

	public static int CountDistinctDescendantValuesOfType<T, TType, TSelect>(this Node<T> node, Func<TType, TSelect> selector)
		=> node
			.GetDescendantsOfType()
			.Select(n => n.Value!)
			.OfType<TType>()
			.Select(selector)
			.Distinct()
			.Count();

	public static int CountDistinctDescendantValuesOfType<T, TType>(this Node<T> node)
		=> node
			.GetDescendantsOfType()
			.Select(n => n.Value!)
			.OfType<TType>()
			.Distinct()
			.Count();

	public static int CountDistinctParameters<T>(this Node<T> node)
		=> node.CountDistinctDescendantValuesOfType<T, IParameter, ushort>(p => p.ID);
}
