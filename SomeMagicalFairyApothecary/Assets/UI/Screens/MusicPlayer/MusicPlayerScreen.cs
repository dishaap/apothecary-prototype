using System;
using Game.UI;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Immutable, display-only description of the track shown by the music player.
/// The screen receives this from a service/model; it never fetches data itself.
/// </summary>
public readonly struct TrackInfo
{
    public readonly string Title;
    public readonly string Artist;
    public readonly string Album;

    public TrackInfo(string title, string artist, string album)
    {
        Title = title;
        Artist = artist;
        Album = album;
    }
}

/// <summary>
/// Controller for the floating music player bar. Owns element queries, event
/// registration and class-based state for this screen only. All visuals live
/// in UXML/USS; this class merely toggles modifier classes and raises events
/// that a music service can subscribe to.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class MusicPlayerScreen : MonoBehaviour, IScreen
{
    private const string HiddenClass = "music-player--hidden";
    private const string OnModifierClass = "icon-btn--on";
    private const string FavoritedModifierClass = "icon-btn--favorited";
    private const string PlayingModifierClass = "icon-btn--playing";

    /// <summary>Raised when the play/pause button is pressed. True means "now playing".</summary>
    public event Action<bool> PlayPauseRequested;

    /// <summary>Raised when the next-track button is pressed.</summary>
    public event Action NextRequested;

    /// <summary>Raised when the previous-track button is pressed.</summary>
    public event Action PreviousRequested;

    /// <summary>Raised when shuffle is toggled. True means "shuffle on".</summary>
    public event Action<bool> ShuffleToggled;

    /// <summary>Raised when repeat is toggled. True means "repeat on".</summary>
    public event Action<bool> RepeatToggled;

    /// <summary>Raised when the favourite (heart) is toggled. True means "favourited".</summary>
    public event Action<bool> FavoriteToggled;

    /// <summary>Raised when the normalized volume (0..1) changes.</summary>
    public event Action<float> VolumeChanged;

    /// <summary>Raised when the album-art (music-notes) thumbnail is pressed to open the playlists.</summary>
    public event Action PlaylistsRequested;

    private UIDocument _document;
    private VisualElement _root;

    private VisualElement _artButton;
    private Button _favoriteButton;
    private Button _shuffleButton;
    private Button _previousButton;
    private Button _playButton;
    private Button _nextButton;
    private Button _repeatButton;
    private Button _volumeButton;
    private Slider _volumeSlider;

    private Label _titleLabel;
    private Label _artistLabel;
    private Label _albumLabel;

    private bool _isVisible = true;
    private bool _isPlaying;
    private bool _isShuffleOn;
    private bool _isRepeatOn;
    private bool _isFavorited;
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
        _isInitialized = true;
    }

    private void CacheElements()
    {
        _root = _document.rootVisualElement.Q<VisualElement>("music-player-root");

        // The album-art thumbnail doubles as the "open playlists" button.
        _artButton = _root.Q<VisualElement>("mp-art");

        // Each control is a TemplateContainer named in the UXML; query its Button.
        _favoriteButton = _root.Q<VisualElement>("btn-favorite").Q<Button>();
        _shuffleButton = _root.Q<VisualElement>("btn-shuffle").Q<Button>();
        _previousButton = _root.Q<VisualElement>("btn-prev").Q<Button>();
        _playButton = _root.Q<VisualElement>("btn-play").Q<Button>();
        _nextButton = _root.Q<VisualElement>("btn-next").Q<Button>();
        _repeatButton = _root.Q<VisualElement>("btn-repeat").Q<Button>();
        _volumeButton = _root.Q<VisualElement>("btn-volume").Q<Button>();

        _volumeSlider = _root.Q<Slider>("mp-volume");

        _titleLabel = _root.Q<Label>("mp-title");
        _artistLabel = _root.Q<Label>("mp-artist");
        _albumLabel = _root.Q<Label>("mp-album");
    }

    private void RegisterCallbacks()
    {
        _artButton.RegisterCallback<ClickEvent>(_ => PlaylistsRequested?.Invoke());
        _playButton.clicked += OnPlayPauseClicked;
        _nextButton.clicked += () => NextRequested?.Invoke();
        _previousButton.clicked += () => PreviousRequested?.Invoke();
        _shuffleButton.clicked += OnShuffleClicked;
        _repeatButton.clicked += OnRepeatClicked;
        _favoriteButton.clicked += OnFavoriteClicked;
        _volumeButton.clicked += OnVolumeButtonClicked;
        _volumeSlider.RegisterValueChangedCallback(OnVolumeSliderChanged);
    }

    /// <summary>Populates the now-playing labels from a data source.</summary>
    public void SetTrack(TrackInfo track)
    {
        _titleLabel.text = track.Title;
        _artistLabel.text = track.Artist;
        _albumLabel.text = track.Album;
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

    // ── Event handlers ───────────────────────────────────────────────────
    private void OnPlayPauseClicked()
    {
        _isPlaying = !_isPlaying;
        _playButton.EnableInClassList(PlayingModifierClass, _isPlaying);
        PlayPauseRequested?.Invoke(_isPlaying);
    }

    private void OnShuffleClicked()
    {
        _isShuffleOn = !_isShuffleOn;
        _shuffleButton.EnableInClassList(OnModifierClass, _isShuffleOn);
        ShuffleToggled?.Invoke(_isShuffleOn);
    }

    private void OnRepeatClicked()
    {
        _isRepeatOn = !_isRepeatOn;
        _repeatButton.EnableInClassList(OnModifierClass, _isRepeatOn);
        RepeatToggled?.Invoke(_isRepeatOn);
    }

    private void OnFavoriteClicked()
    {
        _isFavorited = !_isFavorited;
        _favoriteButton.EnableInClassList(FavoritedModifierClass, _isFavorited);
        FavoriteToggled?.Invoke(_isFavorited);
    }

    private void OnVolumeButtonClicked()
    {
        // Mute toggle: 0 when muted, restore to full otherwise.
        _volumeSlider.value = _volumeSlider.value > 0f ? 0f : 1f;
    }

    private void OnVolumeSliderChanged(ChangeEvent<float> evt)
    {
        VolumeChanged?.Invoke(evt.newValue);
    }
}
