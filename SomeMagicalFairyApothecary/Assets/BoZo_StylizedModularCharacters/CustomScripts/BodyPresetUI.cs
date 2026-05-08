using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class BodyPresetUI : MonoBehaviour
{
    [Header("References")]
    public BodyPresetManager presetManager;
    public Transform buttonContainer;
    public GameObject presetButtonPrefab;

    [Header("Visual Settings")]
    public Color selectedColor =
        new Color(1f, 0.8f, 0.2f);
    public Color defaultColor =
        new Color(0.3f, 0.3f, 0.3f);

    private List<Button> buttons =
        new List<Button>();

    void Start()
    {
        BuildButtons();
        if (presetManager != null &&
            presetManager.presets.Count > 0)
        {
            SelectPreset(0);
        }
    }

    void BuildButtons()
    {
        // Clear existing buttons
        foreach (Transform child in buttonContainer)
            Destroy(child.gameObject);
        buttons.Clear();

        if (presetManager == null)
        {
            Debug.LogError(
                "PresetManager not assigned!");
            return;
        }

        if (presetButtonPrefab == null)
        {
            Debug.LogError(
                "Button prefab not assigned!");
            return;
        }

        for (int i = 0;
             i < presetManager.presets.Count; i++)
        {
            int index = i;
            BodyPreset preset =
                presetManager.presets[i];

            if (preset == null)
            {
                Debug.LogWarning(
                    $"Preset {i} is null!");
                continue;
            }

            // Create button
            GameObject obj = Instantiate(
                presetButtonPrefab,
                buttonContainer);

            // Set icon if available
            Image icon = obj.transform
                .Find("Icon")
                ?.GetComponent<Image>();
            if (icon != null &&
                preset.presetIcon != null)
                icon.sprite = preset.presetIcon;

            // Set label
            TextMeshProUGUI label = obj.transform
                .Find("Label")
                ?.GetComponent<TextMeshProUGUI>();
            if (label != null)
                label.text = preset.presetName;
            else
                Debug.LogWarning(
                    $"Label not found " +
                    $"on button {i}!");

            // Get button component
            Button button =
                obj.GetComponent<Button>();

            if (button != null)
            {
                button.onClick.AddListener(() =>
                {
                    Debug.Log(
                        $"Button {index} clicked!");
                    SelectPreset(index);
                });
                buttons.Add(button);
            }
            else
            {
                Debug.LogWarning(
                    $"No Button component " +
                    $"on prefab {i}!");
            }
        }
    }

    public void SelectPreset(int index)
    {
        if (index < 0 ||
            index >= buttons.Count)
        {
            Debug.LogWarning(
                $"Invalid index: {index}");
            return;
        }

        // Update button visuals
        for (int i = 0; i < buttons.Count; i++)
        {
            if (buttons[i] == null) continue;

            // Try root image first
            Image img = buttons[i]
                .GetComponent<Image>();

            // Fall back to child image
            if (img == null)
                img = buttons[i]
                    .GetComponentInChildren<Image>();

            if (img != null)
                img.color = (i == index)
                    ? selectedColor
                    : defaultColor;
        }

        // Apply the preset
        if (presetManager != null &&
            index < presetManager.presets.Count)
        {
            presetManager.ApplyPreset(
                presetManager.presets[index]);
        }
    }
}