namespace DotnetAgent.Core;

/// <summary>
/// Менеджер отмены изменений (Undo) через резервные копии файлов.
///
/// Фаза 3: откат последних изменений.
///
/// Перед каждым изменением файла через инструменты агента
/// сохраняется резервная копия. Команда "undo" восстанавливает
/// последнее изменение.
///
/// Резервные копии хранятся в памяти (не на диске) в пределах сессии.
/// Поддерживает до MaxUndoSteps шагов назад.
/// </summary>
public sealed class UndoManager
{
    private const int MaxUndoSteps = 20;

    // Стек: каждый элемент — это список изменений одной "операции"
    // (одна операция может затрагивать несколько файлов)
    private readonly Stack<UndoSnapshot> _undoStack = new();

    /// <summary>
    /// Начинает новую операцию (группу изменений).
    /// Вызывается перед выполнением инструмента который может изменить файлы.
    /// </summary>
    public UndoTransaction BeginTransaction(string description)
        => new(this, description);

    /// <summary>
    /// Откатывает последнюю операцию.
    /// Возвращает описание того что было откатано, или null если нечего откатывать.
    /// </summary>
    public string? Undo()
    {
        if (_undoStack.Count == 0)
            return null;

        var snapshot = _undoStack.Pop();
        var restored = new List<string>();

        foreach (var (filePath, originalContent) in snapshot.FileBackups)
        {
            try
            {
                if (originalContent == null)
                {
                    // Файл был создан — удаляем его
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        restored.Add($"  удалён: {filePath}");
                    }
                }
                else
                {
                    // Файл был изменён — восстанавливаем
                    File.WriteAllText(filePath, originalContent, System.Text.Encoding.UTF8);
                    restored.Add($"  восстановлен: {filePath}");
                }
            }
            catch (Exception ex)
            {
                restored.Add($"  ошибка при восстановлении {filePath}: {ex.Message}");
            }
        }

        return $"Откат операции '{snapshot.Description}':\n" + string.Join("\n", restored);
    }

    /// <summary>Количество доступных шагов назад.</summary>
    public int UndoCount => _undoStack.Count;

    internal void CommitSnapshot(UndoSnapshot snapshot)
    {
        _undoStack.Push(snapshot);

        // Ограничиваем размер стека
        while (_undoStack.Count > MaxUndoSteps)
        {
            // Stack не поддерживает удаление снизу напрямую — пересоздаём
            var items = _undoStack.ToArray();
            _undoStack.Clear();
            foreach (var item in items.Take(MaxUndoSteps).Reverse())
                _undoStack.Push(item);
        }
    }
}

/// <summary>Снимок состояния файлов до операции.</summary>
public record UndoSnapshot(
    string Description,
    IReadOnlyDictionary<string, string?> FileBackups);

/// <summary>
/// Транзакция для записи изменений в UndoManager.
///
/// Использование:
/// <code>
///   using var tx = undoManager.BeginTransaction("write_file");
///   tx.BackupFile(filePath);   // сохраняем до изменения
///   File.WriteAllText(filePath, newContent);
///   tx.Commit();               // фиксируем транзакцию
/// </code>
/// </summary>
public sealed class UndoTransaction : IDisposable
{
    private readonly UndoManager _manager;
    private readonly string _description;
    private readonly Dictionary<string, string?> _backups = new();
    internal UndoTransaction(UndoManager manager, string description)
    {
        _manager = manager;
        _description = description;
    }

    /// <summary>
    /// Сохраняет текущее содержимое файла перед изменением.
    /// Если файл не существует — сохраняет null (значит файл был создан).
    /// </summary>
    public void BackupFile(string filePath)
    {
        if (_backups.ContainsKey(filePath)) return; // уже сохранён

        _backups[filePath] = File.Exists(filePath)
            ? File.ReadAllText(filePath, System.Text.Encoding.UTF8)
            : null;
    }

    /// <summary>Фиксирует транзакцию — помещает снимок в стек undo.</summary>
    public void Commit()
    {
        if (_backups.Count > 0)
            _manager.CommitSnapshot(new UndoSnapshot(_description, _backups));
    }

    public void Dispose()
    {
        // Если не вызвали Commit — транзакция не фиксируется
    }
}
