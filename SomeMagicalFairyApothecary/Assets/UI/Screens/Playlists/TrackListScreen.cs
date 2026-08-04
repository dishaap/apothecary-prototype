using System;
using System.Collections.Generic;
using Game.UI;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Immutable, display-only description of a single track row. The screen
/// receives this from a service/model; it never fetches data itself.
/// </summary>
public readonly struct TrackRowInfo
{
    public readonly string Title;
    public readonly string Artist;
    public readonly bool Favorited;

    public TrackRowInfo(string title, string artist, bool favorited)
    {
        Title = title;
        Artist = artist;
        Favorited = favorited;
    }
}

/// <summary>
/// Controller for the track-listing modal. Owns element queries, event
/// registration and class-based state for this screen only. Rows are built from
/// the reusable TrackRow template; the controller merely populates them, toggles
/// modifier classes and raises events the UIManager subscribes to. Visibility
/// and the favourite data are driven from outside (UIManager + music service).
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class TrackListScreen : MonoBehaviour, IScreen
{
    private const string HiddenClass = "scroll-modal--hidden";
    private const string FavoritedClass = "icon-btn--favorited";
    private const string NowPlayingClass = "track-row--playing";
    private const string HeartButtonName = "icon-btn";
    private const string RowName = "track-row";

    [Tooltip("Reusable TrackRow template cloned once per track in the list.")]
    [SerializeField] private VisualTreeAsset trackRowTemplate;

    /// <summary>Raised when the user asks to close the whole panel (X button or backdrop).</summary>
    public event Action CloseRequested;

    /// <summary>Raised when the user asks to step back to the playlists library (back button).</summary>
    public event Action BackRequested;

    /// <summary>Raised when a row's heart is toggled: the track index and the requested state.</summary>
    public event Action<int, bool> TrackFavoriteToggled;

    /// <summary>Raised when a track row is clicked (outside its heart) to play that track.</summary>
    public event Action<int> TrackSelected;

    private UIDocument _document;
    private VisualElement _root;
    private Button _closeButton;
    private Button _backButton;
    private Label _titleLabel;
    private VisualElement _listContainer;

    private readonly List<Button> _favButtons = new List<Button>();
    private readonly List<bool> _favStates = new List<bool>();
    private readonly List<VisualElement> _rows = new List<VisualElement>();

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
        _root = _document.rootVisualElement.Q<VisualElement>("tracklist-root");
        _closeButton = _root.Q<Button>("tracklist-close");
        _backButton = _root.Q<Button>("tracklist-back");
        _titleLabel = _root.Q<Label>("tracklist-title");
        _listContainer = _root.Q<VisualElement>("track-list");
    }

    private void RegisterCallbacks()
    {
        _closeButton.clicked += () => CloseRequested?.Invoke();
        _backButton.clicked += () => BackRequested?.Invoke();

        // Clicking the dimmed backdrop (but not the panel itself) closes the modal.
        _root.RegisterCallback<ClickEvent>(OnBackdropClicked);
    }

    private void OnBackdropClicked(ClickEvent evt)
    {
        if (evt.target == _root)
        {
            CloseRequested?.Invoke();
        }
    }

    /// <summary>Sets the playlist heading shown above the track list.</summary>
    public void SetPlaylistName(string playlistName)
    {
        _titleLabel.text = playlistName;
    }

    /// <summary>
    /// Rebuilds the track rows from a data source. Any design-time preview rows
    /// are cleared first; one TrackRow is cloned per track and wired to raise
    /// <see cref="TrackFavoriteToggled"/> when its heart is pressed.
    /// </summary>
    public void SetTracks(string playlistName, IReadOnlyList<TrackRowInfo> tracks)
    {
        SetPlaylistName(playlistName);

        _listContainer.Clear();
        _favButtons.Clear();
        _favStates.Clear();
        _rows.Clear();

        if (tracks == null || trackRowTemplate == null)
        {
            return;
        }

        for (int i = 0; i < tracks.Count; i++)
        {
            TemplateContainer row = trackRowTemplate.Instantiate();
            TrackRowInfo info = tracks[i];

            row.Q<Label>("track-row__num").text = (i + 1).ToString();
            row.Q<Label>("track-row__title").text = info.Title;
            row.Q<Label>("track-row__artist").text = info.Artist;

            Button favButton = row.Q<Button>(HeartButtonName);
            favButton.EnableInClassList(FavoritedClass, info.Favorited);

            VisualElement rowElement = row.Q<VisualElement>(RowName);
            int index = i;

            favButton.clicked += () => TrackFavoriteToggled?.Invoke(index, !_favStates[index]);

            // Clicking the row plays that track; clicks on the heart are excluded
            // so favouriting never doubles as a play request.
            rowElement.RegisterCallback<ClickEvent>(evt =>
            {
                if (IsWithin(evt.target as VisualElement, favButton))
                {
                    return;
                }

                TrackSelected?.Invoke(index);
            });

            _favButtons.Add(favButton);
            _favStates.Add(info.Favorited);
            _rows.Add(rowElement);
            _listContainer.Add(row);
        }
    }

    /// <summary>Highlights the row of the currently loaded track and clears the rest.</summary>
    public void SetNowPlaying(int index)
    {
        for (int i = 0; i < _rows.Count; i++)
        {
            _rows[i].EnableInClassList(NowPlayingClass, i == index);
        }
    }

    /// <summary>Returns whether an element is the given ancestor or nested within it.</summary>
    private static bool IsWithin(VisualElement element, VisualElement ancestor)
    {
        while (element != null)
        {
            if (element == ancestor)
            {
                return true;
            }

            element = element.parent;
        }

        return false;
    }

    /// <summary>Reflects an authoritative favourite state for a single row (kept in sync by the service).</summary>
    public void SetFavorite(int index, bool favorited)
    {
        if (index < 0 || index >= _favButtons.Count)
        {
            return;
        }

        _favStates[index] = favorited;
        _favButtons[index].EnableInClassList(FavoritedClass, favorited);
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
