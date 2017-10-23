﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Open.Evaluation.Hierarchy
{
    public static class Extensions
    {

		public static bool AreChildrenAligned(this NodeFactory<IEvaluate>.Node target)
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

		public static void AlignValues(this NodeFactory<IEvaluate>.Node target)
		{
			//var parent = target.Value as IParent;
			//if (parent != null)
			//{
			//	var nChildren = target.ToList();
			//	var count = nChildren.Count;
			//	if (count != parent.Children.Count)
			//		return true;

			//	for (var i = 0; i < count; i++)
			//	{
			//		if (nChildren[i] != parent.Children[i])
			//			return true;
			//	}
			//}

			//return false;
		}


	}
}