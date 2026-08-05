using System;
using Game.UI;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Controller for the Pomodoro timer modal. Owns element queries, event
/// registration and class-based state for this screen only. It is display-only:
/// the countdown lives in <see cref="PomodoroService"/>; this screen shows the
/// pushed time/phase/progress and raises intent events the UIManager routes back
/// into the service. Visibility is owned by the UIManager.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class PomodoroScreen : MonoBehaviour, IScreen
{
    private const string HiddenClass = "scroll-modal--hidden";
    private const string BreakClass = "pomodoro-panel--break";
    private const string RunningClass = "pomodoro__toggle--running";

    private const string StartLabel = "Start";
    private const string PauseLabel = "Pause";
    private const string FocusLabel = "Focus";
    private const string BreakLabel = "Break";

    private const int SecondsPerMinute = 60;

    /// <summary>Raised when the user asks to close the panel (X button or backdrop).</summary>
    public event Action CloseRequested;

    /// <summary>Raised when the play/pause button is pressed.</summary>
    public event Action PlayPauseRequested;

    /// <summary>Raised when the reset button is pressed.</summary>
    public event Action ResetRequested;

    /// <summary>Raised when the skip button is pressed.</summary>
    public event Action SkipRequested;

    /// <summary>Raised when the player picks a new focus length, in minutes.</summary>
    public event Action<int> FocusMinutesRequested;

    /// <summary>Raised when the player picks a new break length, in minutes.</summary>
    public event Action<int> BreakMinutesRequested;

    private UIDocument _document;
    private VisualElement _root;
    private VisualElement _panel;
    private Button _closeButton;
    private Label _phaseLabel;
    private Label _timeLabel;
    private VisualElement _progressFill;
    private Button _toggleButton;
    private Button _resetButton;
    private Button _skipButton;
    private DropdownField _focusDropdown;
    private DropdownField _breakDropdown;

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
        _root = _document.rootVisualElement.Q<VisualElement>("pomodoro-root");
        _panel = _root.Q<VisualElement>("pomodoro-panel");
        _closeButton = _root.Q<Button>("pomodoro-close");
        _phaseLabel = _root.Q<Label>("pomodoro-phase");
        _timeLabel = _root.Q<Label>("pomodoro-time");
        _progressFill = _root.Q<VisualElement>("pomodoro-progress-fill");
        _toggleButton = _root.Q<Button>("pomodoro-toggle");
        _resetButton = _root.Q<Button>("pomodoro-reset");
        _skipButton = _root.Q<Button>("pomodoro-skip");
        _focusDropdown = _root.Q<DropdownField>("pomodoro-focus");
        _breakDropdown = _root.Q<DropdownField>("pomodoro-break");
    }

    private void RegisterCallbacks()
    {
        _closeButton.clicked += () => CloseRequested?.Invoke();
        _toggleButton.clicked += () => PlayPauseRequested?.Invoke();
        _resetButton.clicked += () => ResetRequested?.Invoke();
        _skipButton.clicked += () => SkipRequested?.Invoke();

        _focusDropdown.RegisterValueChangedCallback(evt => RaiseMinutes(evt.newValue, FocusMinutesRequested));
        _breakDropdown.RegisterValueChangedCallback(evt => RaiseMinutes(evt.newValue, BreakMinutesRequested));

        // Clicking the dimmed backdrop (but not the panel itself) closes the modal.
        _root.RegisterCallback<ClickEvent>(OnBackdropClicked);
    }

    private void RaiseMinutes(string choice, Action<int> handler)
    {
        if (int.TryParse(choice, out int minutes))
        {
            handler?.Invoke(minutes);
        }
    }

    private void OnBackdropClicked(ClickEvent evt)
    {
        if (evt.target == _root)
        {
            CloseRequested?.Invoke();
        }
    }

    /// <summary>Displays the seconds remaining as a mm:ss countdown.</summary>
    public void SetTimeSeconds(int secondsRemaining)
    {
        int clamped = Mathf.Max(0, secondsRemaining);
        int minutes = clamped / SecondsPerMinute;
        int seconds = clamped % SecondsPerMinute;
        _timeLabel.text = $"{minutes:00}:{seconds:00}";
    }

    /// <summary>Fills the progress bar to the given elapsed fraction (0..1).</summary>
    public void SetProgress(float normalized)
    {
        _progressFill.style.width = Length.Percent(Mathf.Clamp01(normalized) * 100f);
    }

    /// <summary>Updates the phase caption and tints the panel for break vs. focus.</summary>
    public void SetPhase(PomodoroPhase phase)
    {
        _phaseLabel.text = phase == PomodoroPhase.Focus ? FocusLabel : BreakLabel;
        _panel.EnableInClassList(BreakClass, phase == PomodoroPhase.Break);
    }

    /// <summary>Reflects the running state on the play/pause button (label + modifier class).</summary>
    public void SetRunning(bool running)
    {
        _toggleButton.text = running ? PauseLabel : StartLabel;
        _toggleButton.EnableInClassList(RunningClass, running);
    }

    /// <summary>Syncs the duration dropdowns to the service's configured lengths without re-notifying.</summary>
    public void SetDurations(int focusMinutes, int breakMinutes)
    {
        _focusDropdown.SetValueWithoutNotify(focusMinutes.ToString());
        _breakDropdown.SetValueWithoutNotify(breakMinutes.ToString());
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
