using TMPro;
using UnityEngine;

public class PomodoroTimer : MonoBehaviour
{
    [Header("Menus / Panels")]
    public GameObject startPomodoroMenu;
    public GameObject pomodoroTimer;

    [Header("Dropdowns")]
    public TMP_Dropdown sprintWindow;
    public TMP_Dropdown breakWindow;

    [Header("Timer UI")]
    public TMP_Text timeText;       
    public TMP_Text phaseText;       

    private float sprintDuration = 25f * 60f;
    private float breakDuration = 5f * 60f;

    private float remainingTime;
    private bool isRunning;
    private bool isSprintPhase = true;

    void Start()
    {
        if (pomodoroTimer != null) pomodoroTimer.SetActive(false);
        startPomodoroMenu.SetActive(false);
    }

    void Update()
    {
        if (!isRunning) return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            UpdateTimerUI();

            // Switch phase automatically
            SwitchPhase();
            return;
        }

        UpdateTimerUI();
    }

    public void OpenStartPomodoroMenu()
    {
        startPomodoroMenu.SetActive(true);
    }

    public void CancelPomodoro()
    {
        startPomodoroMenu.SetActive(false);
    }

    public void SetPomodoro()
    {
        sprintDuration = GetSprintDuration(sprintWindow.value);
        breakDuration = GetBreakDuration(breakWindow.value);

        startPomodoroMenu.SetActive(false);

        StartPomodoroTimer();
    }

    public void StartPomodoroTimer()
    {
        if (pomodoroTimer != null && !pomodoroTimer.activeInHierarchy)
            pomodoroTimer.SetActive(true);

        // Start fresh on Sprint
        isSprintPhase = true;
        remainingTime = sprintDuration;
        isRunning = true;

        UpdateTimerUI();
        UpdatePhaseUI();
    }

    // --- Button hooks ---
    public void Pause()
    {
        isRunning = false;
    }

    public void Play()
    {
        isRunning = true;
    }

    public void StopAndHide()
    {
        isRunning = false;
        remainingTime = 0f;

        if (pomodoroTimer != null) pomodoroTimer.SetActive(false);
    }

    // --- Internals ---
    private void SwitchPhase()
    {
        isSprintPhase = !isSprintPhase;
        remainingTime = isSprintPhase ? sprintDuration : breakDuration;
        
        isRunning = true;

        UpdatePhaseUI();
        UpdateTimerUI();
    }

    private void UpdateTimerUI()
    {
        if (timeText == null) return;

        int totalSeconds = Mathf.CeilToInt(remainingTime);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        timeText.text = $"{minutes:00}:{seconds:00}";
    }

    private void UpdatePhaseUI()
    {
        if (phaseText == null) return;
        phaseText.text = isSprintPhase ? "Sprint" : "Break";
    }

    // Helpers
    private float GetSprintDuration(int sprintOption)
    {
        switch (sprintOption)
        {
            case 0: return 25 * 60;
            case 1: return 30 * 60;
            case 2: return 40 * 60;
            default: return 25 * 60;
        }
    }

    private float GetBreakDuration(int breakOption)
    {
        switch (breakOption)
        {
            case 0: return 5 * 60;
            case 1: return 10 * 60;
            case 2: return 15 * 60;
            default: return 5 * 60;
        }
    }
}
