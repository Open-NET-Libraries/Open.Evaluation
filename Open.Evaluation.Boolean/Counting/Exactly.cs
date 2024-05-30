using Open.Evaluation.Core;
using System.Diagnostics.CodeAnalysis;

namespace Open.Evaluation.Boolean.Counting
{
	public class Exactly : CountingBase,
		IReproducable<(int, IEnumerable<IEvaluate<bool>>), IEvaluate<bool>>
	{
		internal Exactly(ICatalog<IEvaluate<bool>> catalog, int count, IEnumerable<IEvaluate<bool>> children)
			: base(catalog, nameof(Exactly), count, children)
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

			return new(count == Count, desc);
		}

		internal static Exactly Create(
			ICatalog<IEvaluate<bool>> catalog,
			(int count, IEnumerable<IEvaluate<bool>> children) param)
			=> catalog.Register(new Exactly(catalog, param.count, param.children));

		[SuppressMessage("Performance", "CA1822:Mark members as static")]
		public Exactly NewUsing(
			ICatalog<IEvaluate<bool>> catalog,
			(int, IEnumerable<IEvaluate<bool>>) param)
			=> Create(catalog, param);

		public Exactly NewUsing(
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
		public static IEvaluate<bool> CountExactly(
			this ICatalog<IEvaluate<bool>> catalog,
			(int count, IEnumerable<IEvaluate<bool>> children) param)
			=> Counting.Exactly.Create(catalog, param);
	}
}
