using Open.Evaluation.Boolean.Counting;
using Open.Evaluation.Core;
using System.Diagnostics.CodeAnalysis;

namespace Open.Evaluation.Boolean.Counting
{
	public class AtMost : CountingBase,
		IReproducable<(int, IEnumerable<IEvaluate<bool>>), IEvaluate<bool>>
	{
		internal AtMost(ICatalog<IEvaluate<bool>> catalog, int count, IEnumerable<IEvaluate<bool>> children)
			: base(catalog, nameof(AtMost), count, children)
		{
		}

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

			return new(true, desc);
		}

		internal static AtMost Create(
			ICatalog<IEvaluate<bool>> catalog,
			(int count, IEnumerable<IEvaluate<bool>> children) param)
			=> catalog.Register(new AtMost(catalog, param.count, param.children));

		[SuppressMessage("Performance", "CA1822:Mark members as static")]
		public AtMost NewUsing(
			ICatalog<IEvaluate<bool>> catalog,
			(int, IEnumerable<IEvaluate<bool>>) param)
			=> Create(catalog, param);

		public AtMost NewUsing(
			(int, IEnumerable<IEvaluate<bool>>) param)
			=> Create(Catalog, param);

		IEvaluate<bool> IReproducable<(int, IEnumerable<IEvaluate<bool>>), IEvaluate<bool>>.NewUsing(ICatalog<IEvaluate<bool>> catalog, (int, IEnumerable<IEvaluate<bool>>) param)
			=> NewUsing(catalog, param);

		IEvaluate<bool> IReproducable<(int, IEnumerable<IEvaluate<bool>>), IEvaluate<bool>>.NewUsing((int, IEnumerable<IEvaluate<bool>>) param)
			=> NewUsing(param);
	}
}

namespace Open.Evaluation.Boolean
{
	public static partial class BooleanExtensions
	{
		public static AtMost CountAtMost(
			this ICatalog<IEvaluate<bool>> catalog,
			(int count, IEnumerable<IEvaluate<bool>> children) param)
			=> AtMost.Create(catalog, param);
	}
}
