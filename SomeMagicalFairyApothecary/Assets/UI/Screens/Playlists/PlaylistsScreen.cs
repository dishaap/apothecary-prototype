using System;
using System.Collections.Generic;
using Game.UI;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Immutable, display-only description of a playlist tile. The screen receives
/// this from a service/model; it never fetches data itself.
/// </summary>
public readonly struct PlaylistInfo
{
    public readonly string Name;

    public PlaylistInfo(string name)
    {
        Name = name;
    }
}

/// <summary>
/// Controller for the playlists modal. Owns element queries, event registration
/// and class-based state for this screen only. All visuals live in UXML/USS; the
/// controller merely populates labels, toggles modifier classes and raises
/// events that the UIManager subscribes to. Visibility is driven by UIManager.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class PlaylistsScreen : MonoBehaviour, IScreen
{
    private const string HiddenClass = "scroll-modal--hidden";
    private const string CardHiddenClass = "playlist-card--hidden";
    private const string CardLabelName = "playlist-card__label";
    private const int MaxCards = 5;

    /// <summary>Raised when the user asks to close the panel (X button or backdrop).</summary>
    public event Action CloseRequested;

    /// <summary>Raised when a playlist card is chosen. The argument is its index.</summary>
    public event Action<int> PlaylistSelected;

    private UIDocument _document;
    private VisualElement _root;
    private VisualElement _panel;
    private Button _closeButton;
    private readonly Button[] _cardButtons = new Button[MaxCards];
    private readonly Label[] _cardLabels = new Label[MaxCards];

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
        _root = _document.rootVisualElement.Q<VisualElement>("playlists-root");
        _panel = _root.Q<VisualElement>("playlists-panel");
        _closeButton = _root.Q<Button>("playlists-close");

        for (int i = 0; i < MaxCards; i++)
        {
            VisualElement card = _root.Q<VisualElement>("card-" + i);
            _cardButtons[i] = card?.Q<Button>();
            _cardLabels[i] = card?.Q<Label>(CardLabelName);
        }
    }

    private void RegisterCallbacks()
    {
        _closeButton.clicked += () => CloseRequested?.Invoke();

        // Clicking the dimmed backdrop (but not the panel itself) closes the modal.
        _root.RegisterCallback<ClickEvent>(OnBackdropClicked);

        for (int i = 0; i < MaxCards; i++)
        {
            if (_cardButtons[i] == null)
            {
                continue;
            }

            int index = i;
            _cardButtons[i].clicked += () => PlaylistSelected?.Invoke(index);
        }
    }

    private void OnBackdropClicked(ClickEvent evt)
    {
        if (evt.target == _root)
        {
            CloseRequested?.Invoke();
        }
    }

    /// <summary>Populates the playlist cards from a data source; extra cards hide.</summary>
    public void SetPlaylists(IReadOnlyList<PlaylistInfo> playlists)
    {
        for (int i = 0; i < MaxCards; i++)
        {
            bool hasData = playlists != null && i < playlists.Count;

            if (_cardButtons[i] != null)
            {
                _cardButtons[i].EnableInClassList(CardHiddenClass, !hasData);
            }

            if (hasData && _cardLabels[i] != null)
            {
                _cardLabels[i].text = playlists[i].Name;
            }
        }
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
