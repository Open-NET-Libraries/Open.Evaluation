/*!
 * @author electricessence / https://github.com/electricessence/
 * Licensing: MIT https://github.com/Open-NET-Libraries/Open.Evaluation/blob/master/LICENSE.txt
 */

using Open.Evaluation.Core;

namespace Open.Evaluation.Boolean.Counting
{
	public class Exactly : CountingBase,
		IReproducable<(int, IEnumerable<IEvaluate<bool>>), IEvaluate<bool>>
	{
		public const string Prefix = "Exactly";

		internal Exactly(int count, IEnumerable<IEvaluate<bool>> children)
			: base(Prefix, count, children)
		{
		}

		internal static IEvaluate<bool> Create(
			ICatalog<IEvaluate<bool>> catalog,
			(int count, IEnumerable<IEvaluate<bool>> children) param)
			=> catalog.Register(new Exactly(param.count, param.children));

		public IEvaluate<bool> NewUsing(
			ICatalog<IEvaluate<bool>> catalog,
			(int, IEnumerable<IEvaluate<bool>>) param)
			=> Create(catalog, param);

		protected override EvaluationResult<bool> EvaluateInternal(Context context)
		{
			var count = 0;
			var all = ChildResults(context).ToArray();
			var desc = Describe(all.Select(c => c.Description));
			foreach (var result in all)
			{
				if (result.Result) count++;
				if (count > Count) return new(false, desc);
			}

			return new(count == Count, desc);
		}
	}
}

namespace Open.Evaluation.Boolean
{
	public static partial class BooleanExtensions
	{
		public static IEvaluate<bool> CountExactly(
			this ICatalog<IEvaluate<bool>> catalog,
			(int count, IEnumerable<IEvaluate<bool>> children) param)
			=> Counting.Exactly.Create(catalog, param);
	}
}
