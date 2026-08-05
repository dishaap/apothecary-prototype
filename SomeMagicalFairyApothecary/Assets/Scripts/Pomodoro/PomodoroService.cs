using System;
using UnityEngine;

/// <summary>
/// The two alternating phases of a Pomodoro cycle: a focused work sprint
/// followed by a shorter rest.
/// </summary>
public enum PomodoroPhase
{
    Focus,
    Break
}

/// <summary>
/// Model that owns the Pomodoro timer state and is the single source of truth
/// for it. The display-only <see cref="PomodoroScreen"/> reads from this via the
/// UIManager and raises intent events back; it never runs the countdown itself.
///
/// The countdown advances in <see cref="Update"/> regardless of whether the panel
/// is open, so a sprint keeps running while the player works with the panel closed.
/// Completing a phase automatically switches to the other one. The player's chosen
/// focus and break lengths persist between sessions via <see cref="PlayerPrefs"/>.
/// </summary>
public class PomodoroService : MonoBehaviour
{
    /// <summary>Seconds in a minute; used to convert the configured minute lengths.</summary>
    private const int SecondsPerMinute = 60;

    /// <summary>PlayerPrefs key under which the configured focus length (minutes) persists.</summary>
    private const string FocusMinutesKey = "PomodoroService.FocusMinutes.v1";

    /// <summary>PlayerPrefs key under which the configured break length (minutes) persists.</summary>
    private const string BreakMinutesKey = "PomodoroService.BreakMinutes.v1";

    [Header("Durations (minutes)")]
    [Tooltip("Length of a focus sprint, in minutes. Overridden by the player's saved preference after the first run.")]
    [SerializeField] private int focusMinutes = 25;

    [Tooltip("Length of a break, in minutes. Overridden by the player's saved preference after the first run.")]
    [SerializeField] private int breakMinutes = 5;

    /// <summary>Raised once per whole second (and on resets/phase changes) with the seconds left in the current phase.</summary>
    public event Action<int> OnTickChanged;

    /// <summary>Raised when the timer switches between the focus and break phases.</summary>
    public event Action<PomodoroPhase> OnPhaseChanged;

    /// <summary>Raised when the timer starts or stops counting down.</summary>
    public event Action<bool> OnRunningChanged;

    private PomodoroPhase _phase = PomodoroPhase.Focus;
    private float _remainingSeconds;
    private bool _isRunning;

    // The whole-second value last broadcast, so Update only fires OnTickChanged
    // when the displayed time actually changes (roughly once per second).
    private int _lastBroadcastSecond = -1;

    /// <summary>The phase the timer is currently in.</summary>
    public PomodoroPhase Phase => _phase;

    /// <summary>Whether the countdown is currently running.</summary>
    public bool IsRunning => _isRunning;

    /// <summary>Whole seconds remaining in the current phase, rounded up.</summary>
    public int SecondsRemaining => Mathf.CeilToInt(_remainingSeconds);

    /// <summary>Configured focus length in minutes.</summary>
    public int FocusMinutes => focusMinutes;

    /// <summary>Configured break length in minutes.</summary>
    public int BreakMinutes => breakMinutes;

    /// <summary>Total seconds in the current phase, from its configured minute length.</summary>
    public int CurrentPhaseTotalSeconds => GetPhaseTotalSeconds(_phase);

    /// <summary>Elapsed fraction of the current phase in the 0..1 range (0 at the start, 1 when complete).</summary>
    public float Progress
    {
        get
        {
            int total = CurrentPhaseTotalSeconds;
            return total > 0 ? Mathf.Clamp01((total - _remainingSeconds) / total) : 0f;
        }
    }

    private void Awake()
    {
        LoadConfig();
        ResetToPhase(PomodoroPhase.Focus);
    }

    private void Update()
    {
        if (!_isRunning)
        {
            return;
        }

        _remainingSeconds -= Time.deltaTime;

        if (_remainingSeconds <= 0f)
        {
            _remainingSeconds = 0f;
            BroadcastTick();
            AdvancePhase();
            return;
        }

        BroadcastTick();
    }

    /// <summary>Starts (or resumes) the countdown.</summary>
    public void Play()
    {
        if (_isRunning)
        {
            return;
        }

        _isRunning = true;
        OnRunningChanged?.Invoke(true);
    }

    /// <summary>Pauses the countdown, keeping the remaining time.</summary>
    public void Pause()
    {
        if (!_isRunning)
        {
            return;
        }

        _isRunning = false;
        OnRunningChanged?.Invoke(false);
    }

    /// <summary>Toggles between running and paused (handy for a single play/pause button).</summary>
    public void TogglePlay()
    {
        if (_isRunning)
        {
            Pause();
        }
        else
        {
            Play();
        }
    }

    /// <summary>Stops the timer and returns it to the start of a fresh focus sprint.</summary>
    public void Reset()
    {
        bool wasRunning = _isRunning;
        _isRunning = false;
        ResetToPhase(PomodoroPhase.Focus);

        if (wasRunning)
        {
            OnRunningChanged?.Invoke(false);
        }
    }

    /// <summary>Immediately switches to the other phase, restarting its full duration. Keeps the running state.</summary>
    public void Skip()
    {
        AdvancePhase(keepRunningState: true);
    }

    /// <summary>Sets the focus length in minutes and persists it. Refreshes the countdown when currently on the focus phase.</summary>
    public void SetFocusMinutes(int minutes)
    {
        int clamped = Mathf.Max(1, minutes);
        if (clamped == focusMinutes)
        {
            return;
        }

        focusMinutes = clamped;
        SaveConfig();
        RefreshRemainingIfPhase(PomodoroPhase.Focus);
    }

    /// <summary>Sets the break length in minutes and persists it. Refreshes the countdown when currently on the break phase.</summary>
    public void SetBreakMinutes(int minutes)
    {
        int clamped = Mathf.Max(1, minutes);
        if (clamped == breakMinutes)
        {
            return;
        }

        breakMinutes = clamped;
        SaveConfig();
        RefreshRemainingIfPhase(PomodoroPhase.Break);
    }

    /// <summary>Switches to the next phase, restarts its duration and announces the change.</summary>
    private void AdvancePhase(bool keepRunningState = true)
    {
        _phase = _phase == PomodoroPhase.Focus ? PomodoroPhase.Break : PomodoroPhase.Focus;
        _remainingSeconds = CurrentPhaseTotalSeconds;
        _lastBroadcastSecond = -1;

        if (!keepRunningState && _isRunning)
        {
            _isRunning = false;
            OnRunningChanged?.Invoke(false);
        }

        OnPhaseChanged?.Invoke(_phase);
        BroadcastTick();
    }

    /// <summary>Rewinds to the start of the given phase and announces the reset.</summary>
    private void ResetToPhase(PomodoroPhase phase)
    {
        _phase = phase;
        _remainingSeconds = CurrentPhaseTotalSeconds;
        _lastBroadcastSecond = -1;

        OnPhaseChanged?.Invoke(_phase);
        BroadcastTick();
    }

    /// <summary>Restarts the current countdown from its configured length if the given phase is active.</summary>
    private void RefreshRemainingIfPhase(PomodoroPhase phase)
    {
        if (_phase != phase)
        {
            return;
        }

        _remainingSeconds = CurrentPhaseTotalSeconds;
        _lastBroadcastSecond = -1;
        BroadcastTick();
    }

    /// <summary>Fires <see cref="OnTickChanged"/> only when the displayed whole-second value changes.</summary>
    private void BroadcastTick()
    {
        int shown = SecondsRemaining;
        if (shown == _lastBroadcastSecond)
        {
            return;
        }

        _lastBroadcastSecond = shown;
        OnTickChanged?.Invoke(shown);
    }

    /// <summary>Total seconds in the given phase from its configured minute length.</summary>
    private int GetPhaseTotalSeconds(PomodoroPhase phase)
    {
        int minutes = phase == PomodoroPhase.Focus ? focusMinutes : breakMinutes;
        return minutes * SecondsPerMinute;
    }

    /// <summary>Restores the player's saved focus/break lengths, falling back to the inspector defaults.</summary>
    private void LoadConfig()
    {
        focusMinutes = Mathf.Max(1, PlayerPrefs.GetInt(FocusMinutesKey, focusMinutes));
        breakMinutes = Mathf.Max(1, PlayerPrefs.GetInt(BreakMinutesKey, breakMinutes));
    }

    /// <summary>Persists the configured focus/break lengths.</summary>
    private void SaveConfig()
    {
        PlayerPrefs.SetInt(FocusMinutesKey, focusMinutes);
        PlayerPrefs.SetInt(BreakMinutesKey, breakMinutes);
        PlayerPrefs.Save();
    }
}
