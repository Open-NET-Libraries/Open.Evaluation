using Open.Hierarchy;
using System.Collections.Generic;

namespace Open.Evaluation.Modifiers
{
	public interface IModifyChildren<T, TChild> : IParent<TChild>
    {
		T AddChild(TChild child);

		T AddChildren(IEnumerable<TChild> children);

		T AddChildren(TChild c1, TChild c2, params TChild[] rest);
    }
}
