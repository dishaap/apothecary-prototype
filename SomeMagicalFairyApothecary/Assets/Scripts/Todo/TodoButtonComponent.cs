using System;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Persistent on-screen launcher button that asks the UIManager to open the
/// to-do list. It is display-only: it raises <see cref="Clicked"/> and never
/// shows or hides any panel itself (the UIManager owns visibility).
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class TodoButtonComponent : MonoBehaviour
{
    private const string ButtonName = "todo-button";

    /// <summary>Raised when the launcher button is pressed.</summary>
    public event Action Clicked;

    private UIDocument _document;
    private Button _button;
    private bool _isInitialized;

    private void OnEnable()
    {
        // The UIDocument only clones its visual tree once enabled, so the query
        // must happen in OnEnable. Guard so the click callback is registered once.
        if (_isInitialized)
        {
            return;
        }

        _document = GetComponent<UIDocument>();
        _button = _document.rootVisualElement.Q<Button>(ButtonName);
        _button.clicked += () => Clicked?.Invoke();
        _isInitialized = true;
    }
}
