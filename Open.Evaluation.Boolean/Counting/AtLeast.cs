using Open.Evaluation.Boolean.Counting;
using Open.Evaluation.Core;
using System.Diagnostics.CodeAnalysis;

namespace Open.Evaluation.Boolean.Counting
{
	public class AtLeast : CountingBase,
		IReproducable<(int, IEnumerable<IEvaluate<bool>>), IEvaluate<bool>>
	{
		internal AtLeast(ICatalog<IEvaluate<bool>> catalog, int count, IEnumerable<IEvaluate<bool>> children)
			: base(catalog, nameof(AtLeast), count, children)
		{
			if (count < 1)
				throw new ArgumentOutOfRangeException(nameof(count), count, "Must be at least 1.");
		}

		protected override EvaluationResult<bool> EvaluateInternal(Context context)
		{
			var count = 0;
			var all = ChildResults(context).ToArray();
			var desc = Describe(all.Select(c => c.Description));
			foreach (var result in all)
			{
				if (result.Result) count++;
				if (count == Count) return new(true, desc);
			}

			return new(false, desc);
		}

		internal static AtLeast Create(
			ICatalog<IEvaluate<bool>> catalog,
			(int count, IEnumerable<IEvaluate<bool>> children) param)
			=> catalog.Register(new AtLeast(catalog, param.count, param.children));

		[SuppressMessage("Performance", "CA1822:Mark members as static")]
		public AtLeast NewUsing(
			ICatalog<IEvaluate<bool>> catalog,
			(int, IEnumerable<IEvaluate<bool>>) param)
			=> Create(catalog, param);

		public AtLeast NewUsing(
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
		public static AtLeast CountAtLeast(
			this ICatalog<IEvaluate<bool>> catalog,
			(int count, IEnumerable<IEvaluate<bool>> children) param)
			=> Counting.AtLeast.Create(catalog, param);
	}
}