using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ToDoList : MonoBehaviour
{
    [Header("Panels")]
    public GameObject toDoList;
    public GameObject viewListButton;

    [Header("Input")]
    public TMP_InputField newTask;

    [Header("List")]
    public Transform taskListContainer;      // Content object of a VerticalLayoutGroup/ScrollRect
    public Toggle taskTogglePrefab;          // UI Toggle (TextMeshPro) prefab

    // Track order: incomplete tasks should stay above completed tasks
    private readonly List<Toggle> incomplete = new();
    private readonly List<Toggle> complete = new();

    void Start()
    {
        if (toDoList != null) toDoList.SetActive(false);
    }

    public void ViewToDoList()
    {
        toDoList.SetActive(true);
        viewListButton.SetActive(false);
    }

    public void HideToDoList()
    {
        toDoList.SetActive(false);
        viewListButton.SetActive(true);
    }

    public void AddTask()
    {
        if (newTask == null) return;

        string text = newTask.text?.Trim();

        Debug.Log(text);
        if (string.IsNullOrEmpty(text)) return;

        // Create a new toggle row
        Toggle t = Instantiate(taskTogglePrefab, taskListContainer);
        t.isOn = false;

        TMP_Text label = t.transform.Find("Label")?.GetComponent<TMP_Text>();
        if (label == null)
            label = t.GetComponentInChildren<TMP_Text>(true);

        if (label != null) label.text = text;

        Debug.Log(t.labe)

        // Register in incomplete list + put it just above completed section
        incomplete.Add(t);

        // Wire callback
        t.onValueChanged.AddListener((isOn) => OnTaskToggled(t, isOn));

        // Put it at the right position in the hierarchy
        RebuildVisualOrder();

        // Clear input
        newTask.text = "";
        newTask.ActivateInputField();
    }

    private void OnTaskToggled(Toggle t, bool isOn)
    {
        if (t == null) return;

        // Update strike-through
        TMP_Text label = t.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            // Strikethrough via TextMeshPro rich text
            // (Make sure Rich Text is enabled on the TMP_Text component)
            string raw = StripStrikeTags(label.text);
            label.text = isOn ? $"<s>{raw}</s>" : raw;
        }

        // Move between lists
        if (isOn)
        {
            incomplete.Remove(t);
            if (!complete.Contains(t)) complete.Add(t);
        }
        else
        {
            complete.Remove(t);
            if (!incomplete.Contains(t)) incomplete.Add(t);
        }

        // Reorder UI so incomplete on top, complete at bottom
        RebuildVisualOrder();
    }

    private void RebuildVisualOrder()
    {
        // Incomplete first
        for (int i = 0; i < incomplete.Count; i++)
        {
            if (incomplete[i] != null)
                incomplete[i].transform.SetSiblingIndex(i);
        }

        // Then completed after
        for (int j = 0; j < complete.Count; j++)
        {
            if (complete[j] != null)
                complete[j].transform.SetSiblingIndex(incomplete.Count + j);
        }
    }

    private string StripStrikeTags(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        // Remove only the exact TMP strike tags we add
        return s.Replace("<s>", "").Replace("</s>", "");
    }
}
