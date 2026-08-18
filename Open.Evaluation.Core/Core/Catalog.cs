using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Open.Evaluation.Core;

public class Catalog<T> : DisposableBase, ICatalog<T>
	where T : class, IEvaluate
{
	public Catalog()
	{
		IdLookup = IdPool.GetAlternateLookup<ReadOnlySpan<char>>();
	}

	private static Catalog<T>? _instance;
	public static Catalog<T> Shared
		=> LazyInitializer.EnsureInitialized(ref _instance);

	protected override void OnDispose()
	{
		Registry.Clear();
		Reductions.Clear();
		IdPool.Clear();
	}

	readonly ConditionalWeakTable<string, T> Registry = new();
	readonly ConcurrentDictionary<string, string> IdPool = new();
	readonly ConcurrentDictionary<string, string>.AlternateLookup<ReadOnlySpan<char>> IdLookup;

	/// <summary>
	/// Gets the actual string that will be used for looking up an entry in the catalog.
	/// </summary>
	/// <remarks>
	/// By using strings instead of the actual object, we can reduce the memory footprint of the catalog
	/// and entries that are not used can be garbage collected.
	/// </remarks>
	public string GetPooledId(string id)
	{
		id.ThrowIfNull().OnlyInDebug();
		Contract.EndContractBlock();

		if (IdLookup.TryGetValue(id, out string? result))
			return result;

		if (IdLookup.TryAdd(id, id))
			return id;

		if (IdLookup.TryGetValue(id, out result))
			return result;

		// Should never happen. Throw.
		throw new UnreachableException("Failed to add ID to lookup.");
	}

	public void Register<TItem>(ref TItem item)
		where TItem : notnull, T
	{
		item.ThrowIfNull();
		Contract.EndContractBlock();

		item = Register(item);
	}

	protected virtual TItem OnBeforeRegistration<TItem>(TItem item)
		=> item;

	public TItem Register<TItem>(TItem item)
		where TItem : notnull, T
	{
		item.ThrowIfNull();
		string id = item.ToString().ThrowIfNull();
		Contract.EndContractBlock();

		id = GetPooledId(id);
		T? result = Registry.GetOrAdd(id, OnBeforeRegistration(item));
		Debug.Assert(result is not null);
		Debug.Assert(result.Catalog == this);

		// Interning is keyed purely by string (ToString()/Describe()), so a rendering
		// collision between two distinct node types would otherwise surface here as a
		// blind, unhelpful InvalidCastException. This is a cold (registration-only) path,
		// so the check stays always-on rather than Debug-only.
		return result is TItem typed
			? typed
			: throw new InvalidOperationException(
				$"Catalog identity collision: key '{id}' is registered as {result.GetType()} but {typeof(TItem)} was requested.");
	}

	[SuppressMessage("Style", "IDE0046:Convert to conditional expression",
		Justification = "The hit arm must remain a separate statement so the miss arm's closure allocation is visibly excluded from it.")]
	public TItem Register<TItem>(
		string id, Func<string, ICatalog<T>, TItem> factory)
		where TItem : notnull, T
	{
		id.ThrowIfNull();
		factory.ThrowIfNull();
		Contract.EndContractBlock();

		id = GetPooledId(id);
		// Hit path first, WITHOUT touching GetOrAdd: the inline capturing lambda below
		// forces Roslyn to allocate its closure on EVERY call, even guaranteed hits. This
		// method sits on hot predicate paths (e.g. IsSquareRoot), so the found case must be
		// allocation-free; the closure is only constructed on an actual miss.
		if (Registry.TryGetValue(id, out T? found))
		{
			return found is TItem typedFound
				? typedFound
				: throw new InvalidOperationException(
					$"Catalog identity collision: key '{id}' is registered as {found.GetType()} but {typeof(TItem)} was requested.");
		}

		return (TItem)Registry.GetOrAdd(id, k =>
		{
			TItem? e = factory(k, this);
			Debug.Assert(e is not null);
			Debug.Assert(e.Catalog == this);
			string? hash = e.ToString();
			Debug.Assert(hash == k);
			return hash != k
				? throw new ArgumentException($"Does not match instance.ToString().\nkey: {k}\nhash: {hash}", nameof(id))
				: (T)OnBeforeRegistration(e);
		});
	}

	[return: NotNull]
	[SuppressMessage("Style", "IDE0046:Convert to conditional expression",
		Justification = "The hit arm must remain a separate statement so the miss arm's closure allocation is visibly excluded from it.")]
	public TItem Register<TItem, TParam>(string id, TParam param, Func<string, ICatalog<T>, TParam, TItem> factory)
		where TItem : notnull, T
	{
		id.ThrowIfNull();
		factory.ThrowIfNull();
		Contract.EndContractBlock();

		id = GetPooledId(id);
		// Hit path without closure allocation -- see the note in the sibling overload.
		if (Registry.TryGetValue(id, out T? found))
		{
			return found is TItem typedFound
				? typedFound
				: throw new InvalidOperationException(
					$"Catalog identity collision: key '{id}' is registered as {found.GetType()} but {typeof(TItem)} was requested.");
		}

		return (TItem)Registry.GetOrAdd(id, k =>
		{
			TItem? e = factory(k, this, param);
			Debug.Assert(e is not null);
			Debug.Assert(e.Catalog == this);
			string? hash = e.ToString();
			Debug.Assert(hash == k);
			return hash != k
				? throw new ArgumentException($"Does not match instance.ToStringRepresentation().\nkey: {k}\nhash: {hash}", nameof(id))
				: (T)OnBeforeRegistration(e);
		});
	}

	public bool TryGetItem<TItem>(string id, [MaybeNullWhen(false)] out TItem item)
		where TItem : notnull, T
	{
		id.ThrowIfNull();
		Contract.EndContractBlock();

		id = GetPooledId(id);
		bool result = Registry.TryGetValue(id, out T? e);
		if (result && e is not null)
		{
			Debug.Assert(e.Catalog == this);
			// A found entry of the wrong TItem type is a catalog identity collision --
			// stay loud (matching Register), but descriptive instead of a blind cast.
			if (e is not TItem typed)
			{
				throw new InvalidOperationException(
					$"Catalog identity collision: key '{id}' is registered as {e.GetType()} but {typeof(TItem)} was requested.");
			}

			item = typed;
			return true;
		}

		// A not-found result is a legitimate outcome of the Try-pattern, not a bug.
		// Register guarantees a successful lookup never yields null:
		Debug.Assert(!result, "a successful lookup must never yield a null entry");
		item = default;
		return false;
	}

	public Node<T>.Factory Factory { get; } = new Node<T>.Factory();

	readonly ConditionalWeakTable<IReducibleEvaluation<T>, T> Reductions = [];

	[return: NotNull]
	public T GetReduced([DisallowNull] T source)
	{
		T src = Register(source);
		if (src is not IReducibleEvaluation<T> s)
			return src;

		// Hit path without closure allocation: the inline lambda below captures src, so
		// Roslyn allocates its closure on every call -- even when the reduction is already
		// cached (the steady state on evaluation/mutation paths). Look up first.
		if (Reductions.TryGetValue(s, out T? cached))
			return cached;

		return Reductions.GetValue(s, _ =>
			{
				int count = 0;
				T result = src;
				while (result is IReducibleEvaluation<T> red
					   && red.TryGetReduced(out T? r))
				{
					Debug.Assert(red.Description.Value != r.Description.Value, "Multiple instances of the same item are being exposed.");

					result = r;
					count++;
#if DEBUG
					if (count > 3)
						Debugger.Break();
#endif
					if (count > 10)
						break;
				}

				return Register(result);
			});
	}

	public bool TryGetReduced(
		[DisallowNull] T source, [NotNull] out T reduction)
	{
		reduction = GetReduced(source);
		return !reduction.Equals(source);
	}

	public abstract class SubmoduleBase(
		ICatalog<T> catalog, Node<T>.Factory factory)
	{
		// ReSharper disable once UnusedAutoPropertyAccessor.Global
		public ICatalog<T> Catalog { get; } = catalog ?? throw new ArgumentNullException(nameof(catalog));
		internal readonly Node<T>.Factory Factory = factory ?? throw new ArgumentNullException(nameof(factory));
	}

	public abstract class SubmoduleBase<TCatalog>(
		[DisallowNull] TCatalog catalog)
		: SubmoduleBase(catalog ?? throw new ArgumentNullException(nameof(catalog)), catalog.Factory)
		where TCatalog : Catalog<T>
	{
		public new TCatalog Catalog { get; } = catalog;
	}
}
