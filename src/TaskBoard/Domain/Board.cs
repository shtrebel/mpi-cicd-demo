namespace TaskBoard.Domain;

/// <summary>Задача на доске.</summary>
public sealed record WorkItem(string Id, string Title);

/// <summary>Бросается, когда этап уже держит столько задач, сколько разрешено.</summary>
public sealed class WipLimitExceededException : InvalidOperationException
{
    public WipLimitExceededException(string stage, int limit)
        : base($"Этап «{stage}» уже держит {limit} задач: лимит незавершённой работы исчерпан.")
    {
        Stage = stage;
        Limit = limit;
    }

    public string Stage { get; }

    public int Limit { get; }
}

/// <summary>Этап доски. Лимит null означает, что этап не ограничен.</summary>
public sealed class Stage
{
    private readonly List<WorkItem> _items = new();

    public Stage(string name, int? limit = null)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("У этапа должно быть название.", nameof(name));
        }

        if (limit is <= 0)
        {
            throw new ArgumentException("Лимит должен быть положительным.", nameof(limit));
        }

        Name = name;
        Limit = limit;
    }

    public string Name { get; }

    public int? Limit { get; }

    public IReadOnlyList<WorkItem> Items => _items;

    public bool IsFull => Limit.HasValue && _items.Count >= Limit.Value;

    internal void Add(WorkItem item)
    {
        if (IsFull)
        {
            throw new WipLimitExceededException(Name, Limit!.Value);
        }

        _items.Add(item);
    }

    internal void Remove(WorkItem item) => _items.Remove(item);
}

/// <summary>Доска задач с лимитом незавершённой работы на каждом этапе.</summary>
public sealed class Board
{
    private readonly List<Stage> _stages;

    public Board(params Stage[] stages)
    {
        if (stages.Length == 0)
        {
            throw new ArgumentException("На доске должен быть хотя бы один этап.", nameof(stages));
        }

        _stages = stages.ToList();
    }

    public IReadOnlyList<Stage> Stages => _stages;

    /// <summary>Положить новую задачу на этап.</summary>
    public void Place(WorkItem item, string stageName) => Find(stageName).Add(item);

    /// <summary>Перенести задачу на другой этап. Место на исходном этапе освобождается.</summary>
    public void Move(string itemId, string toStageName)
    {
        var from = StageOf(itemId);
        var item = from.Items.Single(i => i.Id == itemId);
        var to = Find(toStageName);

        if (ReferenceEquals(from, to))
        {
            return;
        }

        to.Add(item);
        from.Remove(item);
    }

    /// <summary>Этап, на котором сейчас находится задача.</summary>
    public Stage StageOf(string itemId)
    {
        var stage = _stages.FirstOrDefault(s => s.Items.Any(i => i.Id == itemId));
        return stage ?? throw new ArgumentException($"Задача «{itemId}» на доске не найдена.", nameof(itemId));
    }

    private Stage Find(string stageName)
    {
        var stage = _stages.FirstOrDefault(s => s.Name == stageName);
        return stage ?? throw new ArgumentException($"Этап «{stageName}» на доске не найден.", nameof(stageName));
    }
}
