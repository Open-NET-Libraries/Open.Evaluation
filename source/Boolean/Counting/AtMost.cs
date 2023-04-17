/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Evaluation.Core;

namespace Open.Evaluation.Boolean.Counting
{
	public class AtMost : CountingBase,
			IReproducable<(int, IEnumerable<IEvaluate<bool>>), IEvaluate<bool>>
	{
		public const string PREFIX = "AtMost";

		internal AtMost(int count, IEnumerable<IEvaluate<bool>> children)
			: base(PREFIX, count, children)
		{
		}

		internal static IEvaluate<bool> Create(
			ICatalog<IEvaluate<bool>> catalog,
			(int count, IEnumerable<IEvaluate<bool>> children) param)
			=> catalog.Register(new AtMost(param.count, param.children));

		public IEvaluate<bool> NewUsing(
			ICatalog<IEvaluate<bool>> catalog,
			(int, IEnumerable<IEvaluate<bool>>) param)
			=> Create(catalog, param);

		protected override bool EvaluateInternal(object context)
		{
			var count = 0;
			foreach (var result in ChildResults(context))
			{
				if ((bool)result) count++;
				if (count > Count) return false;
			}

			return true;
		}
	}
}

namespace Open.Evaluation.Boolean
{
	public static partial class BooleanExtensions
	{
		public static IEvaluate<bool> CountAtMost(
			this ICatalog<IEvaluate<bool>> catalog,
			(int count, IEnumerable<IEvaluate<bool>> children) param)
			=> Counting.AtMost.Create(catalog, param);
	}
}
