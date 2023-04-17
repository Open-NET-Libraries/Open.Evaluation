using Open.Evaluation.Core;
using Open.Hierarchy;

namespace Open.Evaluation.Hierarchy;

public static class Extensions
{
	public static bool AreChildrenAligned(this Node<IEvaluate> target)
	{
		if (target is null)
			throw new ArgumentNullException(nameof(target));

		switch (target.Value)
		{
			case IParent parent:
				// If the value contains children, return true only if they match.
				var children = target.ToArray();
				var count = children.Length;

				// The count should match...
				if (count != parent.Children.Count)
					return false;

				for (var i = 0; i < count; i++)
				{
					// Does the map of the children match the actual?
					if (children[i] != parent.Children[i])
						return false;
				}

				// Everything is the same..
				return true;

			default:
				// Value does not have children? Return true only if this has no children.
				return target.Count == 0;
		}
	}

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
		=> node.CountDistinctDescendantValuesOfType<T, Parameter, ushort>(p => p.ID);
}
