using UnityEngine;

[CreateAssetMenu(fileName = "New Body Preset",
                 menuName = "BoZo/Body Preset")]
public class BodyPreset : ScriptableObject
{
    [Header("Display")]
    public string presetName;
    public Sprite presetIcon;

    [Header("Blendshapes (0-100)")]
    [Range(0f, 100f)] public float bodyType;
    [Range(0f, 100f)] public float chest;
    [Range(0f, 100f)] public float weight;
    [Range(0f, 100f)] public float belly;
    [Range(0f, 100f)] public float muscle;

    [Header("Animator (0-1)")]
    [Range(0f, 1f)] public float stance;
    [Range(0f, 1f)] public float height;
}