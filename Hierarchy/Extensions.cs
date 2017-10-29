using Open.Hierarchy;
using System.Linq;

namespace Open.Evaluation.Hierarchy
{
	public static class Extensions
    {

		public static bool AreChildrenAligned(this Node<IEvaluate> target)
		{
			if (target.Value is IParent parent)
			{
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
			}
			else
			{
				// Value does not have children? Return true only if this has no children.
				return target.Count == 0;
			}

			// Everything is the same..
			return true;
		}


	}
}
