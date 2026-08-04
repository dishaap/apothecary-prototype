using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Serializable playlist entry pairing an audio clip with the now-playing
/// metadata displayed by the music player UI.
/// </summary>
[Serializable]
public class MusicTrack
{
    public AudioClip clip;
    public string title;
    public string artist;
    public string album;
}

/// <summary>
/// Audio backend for the floating music player. Owns the <see cref="AudioSource"/>
/// and the playlist, listens to the display-only <see cref="MusicPlayerScreen"/>'s
/// input events, drives playback, and pushes now-playing data back into the
/// screen. The screen holds no audio state or data of its own.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class MusicPlayerService : MonoBehaviour
{
    private const float DefaultVolume = 0.7f;

    [SerializeField] private MusicPlayerScreen screen;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private List<MusicTrack> playlist = new List<MusicTrack>();

    private readonly HashSet<int> _favoritedTracks = new HashSet<int>();
    private readonly System.Random _shuffleRandom = new System.Random();

    private int _currentTrackIndex;
    private bool _wantsPlayback;
    private bool _isShuffleOn;
    private bool _isRepeatOn;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (screen == null)
        {
            screen = GetComponent<MusicPlayerScreen>();
        }

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.volume = DefaultVolume;
    }

    private void OnEnable()
    {
        screen.PlayPauseRequested += OnPlayPauseRequested;
        screen.NextRequested += OnNextRequested;
        screen.PreviousRequested += OnPreviousRequested;
        screen.ShuffleToggled += OnShuffleToggled;
        screen.RepeatToggled += OnRepeatToggled;
        screen.FavoriteToggled += OnFavoriteToggled;
        screen.VolumeChanged += OnVolumeChanged;
    }

    private void OnDisable()
    {
        screen.PlayPauseRequested -= OnPlayPauseRequested;
        screen.NextRequested -= OnNextRequested;
        screen.PreviousRequested -= OnPreviousRequested;
        screen.ShuffleToggled -= OnShuffleToggled;
        screen.RepeatToggled -= OnRepeatToggled;
        screen.FavoriteToggled -= OnFavoriteToggled;
        screen.VolumeChanged -= OnVolumeChanged;
    }

    private void Start()
    {
        // Load the first track into the UI without auto-playing.
        if (playlist.Count > 0)
        {
            LoadTrack(0, false);
        }
    }

    private void Update()
    {
        // Auto-advance when the current clip finishes. A user pause clears
        // _wantsPlayback, so this only triggers on natural end-of-track.
        if (_wantsPlayback && audioSource.clip != null && !audioSource.isPlaying)
        {
            if (_isRepeatOn)
            {
                audioSource.Play();
            }
            else
            {
                PlayNext();
            }
        }
    }

    /// <summary>Loads a playlist entry into the audio source and the UI, optionally starting playback.</summary>
    private void LoadTrack(int index, bool play)
    {
        if (playlist.Count == 0)
        {
            return;
        }

        _currentTrackIndex = Mathf.Clamp(index, 0, playlist.Count - 1);
        MusicTrack track = playlist[_currentTrackIndex];

        audioSource.clip = track.clip;
        audioSource.time = 0f;

        screen.SetTrack(new TrackInfo(track.title, track.artist, track.album));

        if (play && track.clip != null)
        {
            audioSource.Play();
            _wantsPlayback = true;
        }
    }

    /// <summary>Advances to the next track, respecting shuffle mode, and plays it.</summary>
    private void PlayNext()
    {
        if (playlist.Count == 0)
        {
            return;
        }

        int nextIndex = _isShuffleOn
            ? GetRandomTrackIndex()
            : (_currentTrackIndex + 1) % playlist.Count;
        LoadTrack(nextIndex, true);
    }

    /// <summary>Returns to the previous track, respecting shuffle mode, and plays it.</summary>
    private void PlayPrevious()
    {
        if (playlist.Count == 0)
        {
            return;
        }

        int previousIndex = _isShuffleOn
            ? GetRandomTrackIndex()
            : (_currentTrackIndex - 1 + playlist.Count) % playlist.Count;
        LoadTrack(previousIndex, true);
    }

    /// <summary>Picks a random track index different from the current one when possible.</summary>
    private int GetRandomTrackIndex()
    {
        if (playlist.Count <= 1)
        {
            return _currentTrackIndex;
        }

        int index;
        do
        {
            index = _shuffleRandom.Next(playlist.Count);
        }
        while (index == _currentTrackIndex);

        return index;
    }

    private void OnPlayPauseRequested(bool wantsToPlay)
    {
        if (playlist.Count == 0)
        {
            return;
        }

        _wantsPlayback = wantsToPlay;

        if (wantsToPlay)
        {
            if (audioSource.clip == null)
            {
                LoadTrack(_currentTrackIndex, true);
            }
            else if (audioSource.time > 0f)
            {
                audioSource.UnPause();
            }
            else
            {
                audioSource.Play();
            }
        }
        else
        {
            audioSource.Pause();
        }
    }

    private void OnNextRequested()
    {
        PlayNext();
    }

    private void OnPreviousRequested()
    {
        PlayPrevious();
    }

    private void OnShuffleToggled(bool isOn)
    {
        _isShuffleOn = isOn;
    }

    private void OnRepeatToggled(bool isOn)
    {
        _isRepeatOn = isOn;
    }

    private void OnFavoriteToggled(bool isFavorited)
    {
        if (isFavorited)
        {
            _favoritedTracks.Add(_currentTrackIndex);
        }
        else
        {
            _favoritedTracks.Remove(_currentTrackIndex);
        }
    }

    private void OnVolumeChanged(float normalizedVolume)
    {
        audioSource.volume = Mathf.Clamp01(normalizedVolume);
    }
}
