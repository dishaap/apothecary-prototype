using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Serializable to-do entry: a task caption and whether it has been completed.
/// </summary>
[Serializable]
public class TodoTask
{
    public string title;
    public bool done;
}

/// <summary>
/// Model that owns the player's to-do tasks and their completion state, and the
/// single source of truth for both. The display-only <see cref="TodoListScreen"/>
/// reads from this via the UIManager and never stores task data itself.
///
/// Tasks the player adds, completes or deletes persist between sessions: the list
/// is serialised to <see cref="PlayerPrefs"/> as JSON on every change and restored
/// on load. It raises <see cref="TasksChanged"/> for completion toggles and
/// <see cref="TasksListChanged"/> for structural edits (add/remove) so every view
/// can refresh in sync.
/// </summary>
public class TodoService : MonoBehaviour
{
    /// <summary>PlayerPrefs key under which the task list JSON is persisted.</summary>
    private const string SaveKey = "TodoService.Tasks.v1";

    [Tooltip("Starter tasks used only on the very first run, before the player has saved any of their own.")]
    [SerializeField]
    private List<TodoTask> tasks = new List<TodoTask>
    {
        new TodoTask { title = "Gather moonpetals at dawn", done = false },
        new TodoTask { title = "Brew a calming lavender tonic", done = false },
        new TodoTask { title = "Restock the dried mushroom shelf", done = false },
        new TodoTask { title = "Water the whispering ferns", done = false },
        new TodoTask { title = "Label the new elixir bottles", done = false }
    };

    /// <summary>Raised whenever a task's completion state changes.</summary>
    public event Action TasksChanged;

    /// <summary>Raised whenever a task is added or removed (the list length changes).</summary>
    public event Action TasksListChanged;

    /// <summary>The tasks in display order.</summary>
    public IReadOnlyList<TodoTask> Tasks => tasks;

    private void Awake()
    {
        Load();
    }

    /// <summary>Whether the task at the given index is complete.</summary>
    public bool IsDone(int index)
    {
        return index >= 0 && index < tasks.Count && tasks[index].done;
    }

    /// <summary>
    /// Sets the completion state of a task by index and persists it. Fires
    /// <see cref="TasksChanged"/> only when the state actually changes, so views
    /// stay in sync without redundant rebuilds.
    /// </summary>
    public void SetDone(int index, bool done)
    {
        if (index < 0 || index >= tasks.Count || tasks[index].done == done)
        {
            return;
        }

        tasks[index].done = done;
        Save();
        TasksChanged?.Invoke();
    }

    /// <summary>
    /// Appends a new, incomplete task to the end of the list and persists it.
    /// Blank or whitespace-only titles are ignored. Fires <see cref="TasksListChanged"/>.
    /// </summary>
    public void AddTask(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return;
        }

        tasks.Add(new TodoTask { title = title.Trim(), done = false });
        Save();
        TasksListChanged?.Invoke();
    }

    /// <summary>
    /// Removes the task at the given index and persists the change. Fires
    /// <see cref="TasksListChanged"/> when a task is actually removed.
    /// </summary>
    public void RemoveTask(int index)
    {
        if (index < 0 || index >= tasks.Count)
        {
            return;
        }

        tasks.RemoveAt(index);
        Save();
        TasksListChanged?.Invoke();
    }

    /// <summary>
    /// Restores the persisted task list. On the very first run (no saved data) the
    /// inspector-authored starter tasks are kept and immediately persisted so they
    /// become the player's editable baseline.
    /// </summary>
    private void Load()
    {
        if (!PlayerPrefs.HasKey(SaveKey))
        {
            Save();
            return;
        }

        string json = PlayerPrefs.GetString(SaveKey);
        TodoTaskList payload = JsonUtility.FromJson<TodoTaskList>(json);
        if (payload != null && payload.tasks != null)
        {
            tasks = payload.tasks;
        }
    }

    /// <summary>Persists the current task list to PlayerPrefs as JSON.</summary>
    private void Save()
    {
        TodoTaskList payload = new TodoTaskList { tasks = tasks };
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(payload));
        PlayerPrefs.Save();
    }

    /// <summary>Serialisable wrapper so JsonUtility can round-trip the task list.</summary>
    [Serializable]
    private class TodoTaskList
    {
        public List<TodoTask> tasks;
    }
}
