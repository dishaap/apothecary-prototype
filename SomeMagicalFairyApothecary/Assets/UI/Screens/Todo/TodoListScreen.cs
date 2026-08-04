using System;
using System.Collections.Generic;
using Game.UI;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Immutable, display-only description of a single to-do row. The screen receives
/// this from a service/model; it never fetches data itself.
/// </summary>
public readonly struct TodoItemInfo
{
    public readonly string Title;
    public readonly bool Done;

    public TodoItemInfo(string title, bool done)
    {
        Title = title;
        Done = done;
    }
}

/// <summary>
/// Controller for the to-do list modal. Owns element queries, event registration
/// and class-based state for this screen only. Rows are built from the reusable
/// TodoItem template; the controller populates them, toggles modifier classes and
/// raises events the UIManager subscribes to. Visibility and the task data are
/// driven from outside (UIManager + TodoService).
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class TodoListScreen : MonoBehaviour, IScreen
{
    private const string HiddenClass = "scroll-modal--hidden";
    private const string DoneClass = "todo-item--done";
    private const string ItemName = "todo-item";
    private const string ToggleName = "todo-item__toggle";
    private const string DeleteName = "todo-item__delete";
    private const string LabelName = "todo-item__label";

    [Tooltip("Reusable TodoItem template cloned once per task in the list.")]
    [SerializeField] private VisualTreeAsset todoItemTemplate;

    /// <summary>Raised when the user asks to close the panel (X button or backdrop).</summary>
    public event Action CloseRequested;

    /// <summary>Raised when a row is toggled: the task index and the requested done state.</summary>
    public event Action<int, bool> TaskToggled;

    /// <summary>Raised when the user commits a new task from the add field. The argument is the typed title.</summary>
    public event Action<string> TaskAddRequested;

    /// <summary>Raised when a row's delete button is pressed. The argument is the task index.</summary>
    public event Action<int> TaskDeleteRequested;

    private UIDocument _document;
    private VisualElement _root;
    private Button _closeButton;
    private Label _titleLabel;
    private Label _progressLabel;
    private ScrollView _scrollView;
    private VisualElement _listContainer;
    private TextField _addField;
    private Button _addButton;

    private readonly List<VisualElement> _rows = new List<VisualElement>();
    private readonly List<bool> _doneStates = new List<bool>();

    private bool _isVisible;
    private bool _isInitialized;

    private void OnEnable()
    {
        // The UIDocument only clones its visual tree once enabled, so element
        // queries must happen in OnEnable (not Awake). Guard against repeated
        // enable/disable cycles so callbacks are only registered once.
        if (_isInitialized)
        {
            return;
        }

        _document = GetComponent<UIDocument>();
        CacheElements();
        RegisterCallbacks();
        _isVisible = !_root.ClassListContains(HiddenClass);
        _isInitialized = true;
    }

    private void CacheElements()
    {
        _root = _document.rootVisualElement.Q<VisualElement>("todo-root");
        _closeButton = _root.Q<Button>("todo-close");
        _titleLabel = _root.Q<Label>("todo-title");
        _progressLabel = _root.Q<Label>("todo-progress");
        _scrollView = _root.Q<ScrollView>("todo-scroll");
        _listContainer = _root.Q<VisualElement>("todo-list");
        _addField = _root.Q<TextField>("todo-add-field");
        _addButton = _root.Q<Button>("todo-add-btn");
    }

    private void RegisterCallbacks()
    {
        _closeButton.clicked += () => CloseRequested?.Invoke();
        _addButton.clicked += SubmitNewTask;

        // Pressing Enter in the add field commits the task, just like the + button.
        _addField.RegisterCallback<KeyDownEvent>(OnAddFieldKeyDown);

        // Clicking the dimmed backdrop (but not the panel itself) closes the modal.
        _root.RegisterCallback<ClickEvent>(OnBackdropClicked);
    }

    private void OnAddFieldKeyDown(KeyDownEvent evt)
    {
        if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
        {
            SubmitNewTask();
            evt.StopPropagation();
        }
    }

    /// <summary>Commits the add field's text as a new task and clears the field.</summary>
    private void SubmitNewTask()
    {
        string title = _addField.value;
        if (string.IsNullOrWhiteSpace(title))
        {
            return;
        }

        TaskAddRequested?.Invoke(title.Trim());
        _addField.value = string.Empty;
    }

    private void OnBackdropClicked(ClickEvent evt)
    {
        if (evt.target == _root)
        {
            CloseRequested?.Invoke();
        }
    }

    /// <summary>Sets the heading shown above the list.</summary>
    public void SetTitle(string title)
    {
        _titleLabel.text = title;
    }

    /// <summary>
    /// Rebuilds the to-do rows from a data source. Any design-time preview rows are
    /// cleared first; one TodoItem is cloned per task and wired to raise
    /// <see cref="TaskToggled"/> when clicked.
    /// </summary>
    public void SetTasks(IReadOnlyList<TodoItemInfo> tasks)
    {
        _listContainer.Clear();
        _rows.Clear();
        _doneStates.Clear();

        if (tasks == null || todoItemTemplate == null)
        {
            UpdateProgress();
            return;
        }

        for (int i = 0; i < tasks.Count; i++)
        {
            TemplateContainer row = todoItemTemplate.Instantiate();
            TodoItemInfo info = tasks[i];

            row.Q<Label>(LabelName).text = info.Title;

            VisualElement rowElement = row.Q<VisualElement>(ItemName);
            rowElement.EnableInClassList(DoneClass, info.Done);

            Button toggleButton = row.Q<Button>(ToggleName);
            Button deleteButton = row.Q<Button>(DeleteName);

            int index = i;
            toggleButton.clicked += () => TaskToggled?.Invoke(index, !_doneStates[index]);
            deleteButton.clicked += () => TaskDeleteRequested?.Invoke(index);

            _rows.Add(rowElement);
            _doneStates.Add(info.Done);
            _listContainer.Add(row);
        }

        UpdateProgress();
    }

    /// <summary>
    /// Rebuilds the rows on the next layout tick. Use when the rebuild is triggered
    /// from within a row's own event (e.g. a delete button removing that row), so the
    /// list is not mutated while that event is still being dispatched. When
    /// <paramref name="scrollToNewest"/> is set, the list scrolls to the last row once
    /// rebuilt so a freshly added task is always visible at the bottom.
    /// </summary>
    public void SetTasksDeferred(IReadOnlyList<TodoItemInfo> tasks, bool scrollToNewest = false)
    {
        _root.schedule.Execute(() =>
        {
            SetTasks(tasks);
            if (scrollToNewest)
            {
                ScrollToNewest();
            }
        });
    }

    /// <summary>Scrolls the list so the last (newest) task row is brought into view.</summary>
    private void ScrollToNewest()
    {
        if (_scrollView == null || _rows.Count == 0)
        {
            return;
        }

        _scrollView.ScrollTo(_rows[_rows.Count - 1]);
    }

    /// <summary>Reflects an authoritative done state for a single row (kept in sync by the service).</summary>
    public void SetDone(int index, bool done)
    {
        if (index < 0 || index >= _rows.Count)
        {
            return;
        }

        _doneStates[index] = done;
        _rows[index].EnableInClassList(DoneClass, done);
        UpdateProgress();
    }

    /// <summary>Refreshes the "N / M done" progress caption from the cached row states.</summary>
    private void UpdateProgress()
    {
        if (_progressLabel == null)
        {
            return;
        }

        int completed = 0;
        for (int i = 0; i < _doneStates.Count; i++)
        {
            if (_doneStates[i])
            {
                completed++;
            }
        }

        _progressLabel.text = completed + " / " + _doneStates.Count + " done";
    }

    // ── IScreen ──────────────────────────────────────────────────────────
    public void Show()
    {
        _isVisible = true;
        _root.RemoveFromClassList(HiddenClass);
    }

    public void Hide()
    {
        _isVisible = false;
        _root.AddToClassList(HiddenClass);
    }

    public void Toggle()
    {
        if (_isVisible)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }
}
