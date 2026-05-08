using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Bozo.ModularCharacters;

public class BodyPresetManager : MonoBehaviour
{
    [Header("Presets")]
    public List<BodyPreset> presets;

    [Header("BoZo References")]
    public OutfitSystem outfitSystem;

    private BodyPreset currentPreset;

    public void ApplyPreset(BodyPreset preset)
    {
        if (preset == null) return;
        if (outfitSystem == null) return;

        currentPreset = preset;
        Debug.Log($"Applying: {preset.presetName}");

        SetSlider("BodyType", preset.bodyType);
        SetSlider("Chest",    preset.chest);
        SetSlider("Weight",   preset.weight);
        SetSlider("Belly",    preset.belly);
        SetSlider("Muscle",   preset.muscle);

        outfitSystem.SetStance(preset.stance);
        outfitSystem.SetHeight(preset.height);

        Debug.Log("✅ Done!");
    }

    void SetSlider(string shapeName, float value)
    {
        // Find ALL sliders in scene
        var allSliders = 
            FindObjectsOfType<Slider>();

        Debug.Log(
            $"Total sliders found: " +
            $"{allSliders.Length}");

        foreach (var slider in allSliders)
        {
            // Check parent objects for
            // BlendSlider component
            var blendSlider = slider
                .GetComponentInParent
                <BlendSlider>();

            if (blendSlider != null)
            {
                // Get shape field
                var shapeField = 
                    typeof(BlendSlider)
                    .GetField("shape",
                    System.Reflection
                        .BindingFlags.NonPublic |
                    System.Reflection
                        .BindingFlags.Instance);

                if (shapeField != null)
                {
                    string shape = shapeField
                        .GetValue(blendSlider) 
                        as string;

                    if (shape == shapeName)
                    {
                        // Set slider value
                        // This triggers Apply!
                        slider.value = value;
                        Debug.Log(
                            $"✅ Set {shapeName}" +
                            $" = {value}");
                        return;
                    }
                }
            }
        }

        // Fallback
        Debug.LogWarning(
            $"Slider not found for: {shapeName}"+
            $" - using SetShape directly");
        outfitSystem.SetShape(shapeName, value);
    }
}