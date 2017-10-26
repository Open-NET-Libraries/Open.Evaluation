/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/electricessence/Open.Evaluation/blob/master/LICENSE.txt
 */

using System.Collections.Generic;
namespace Open.Evaluation.Hierarchy
{
	public interface IParent
	{
		IReadOnlyList<object> Children { get; }
	}

	public interface IParent<out TChild> : IParent
	{
		new IReadOnlyList<TChild> Children { get; }
	}

}