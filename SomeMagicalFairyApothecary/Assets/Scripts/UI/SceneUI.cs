using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central owner of screen visibility for the scene. Screens never show or hide
/// themselves; they expose Show()/Hide() and raise intent events, and this
/// manager routes those into visibility changes. It also mediates data between
/// the display-only screens and the <see cref="MusicPlayerService"/> model.
/// </summary>
[DefaultExecutionOrder(1000)]
public class UIManager : MonoBehaviour
{
    public GameObject optionsMenu;
    public GameObject sceneMenu;

    [Header("Music Player")]
    [SerializeField] private MusicPlayerScreen musicPlayerScreen;
    [SerializeField] private PlaylistsScreen playlistsScreen;
    [SerializeField] private TrackListScreen trackListScreen;

    [Tooltip("Audio model that owns the tracks and the shared favourite state.")]
    [SerializeField] private MusicPlayerService musicPlayerService;

    [Tooltip("Playlist names shown in the playlists modal (display data).")]
    [SerializeField]
    private string[] playlistNames =
    {
        "Nightfall",
        "Dewdrops",
        "Wisteria",
        "A Cottage Morning"
    };

    private void OnEnable()
    {
        if (musicPlayerScreen != null)
        {
            musicPlayerScreen.PlaylistsRequested += OpenPlaylists;
        }

        if (playlistsScreen != null)
        {
            playlistsScreen.CloseRequested += ClosePlaylists;
            playlistsScreen.PlaylistSelected += OpenTrackList;
        }

        if (trackListScreen != null)
        {
            trackListScreen.CloseRequested += CloseTrackList;
            trackListScreen.BackRequested += GoBackToPlaylists;
            trackListScreen.TrackFavoriteToggled += OnTrackFavoriteToggled;
            trackListScreen.TrackSelected += OnTrackSelected;
        }

        if (musicPlayerService != null)
        {
            musicPlayerService.FavoritesChanged += RefreshTrackListFavorites;
            musicPlayerService.TrackChanged += OnTrackChanged;
        }
    }

    private void OnDisable()
    {
        if (musicPlayerScreen != null)
        {
            musicPlayerScreen.PlaylistsRequested -= OpenPlaylists;
        }

        if (playlistsScreen != null)
        {
            playlistsScreen.CloseRequested -= ClosePlaylists;
            playlistsScreen.PlaylistSelected -= OpenTrackList;
        }

        if (trackListScreen != null)
        {
            trackListScreen.CloseRequested -= CloseTrackList;
            trackListScreen.BackRequested -= GoBackToPlaylists;
            trackListScreen.TrackFavoriteToggled -= OnTrackFavoriteToggled;
            trackListScreen.TrackSelected -= OnTrackSelected;
        }

        if (musicPlayerService != null)
        {
            musicPlayerService.FavoritesChanged -= RefreshTrackListFavorites;
            musicPlayerService.TrackChanged -= OnTrackChanged;
        }
    }

    private void Start()
    {
        if (playlistsScreen != null)
        {
            var infos = new List<PlaylistInfo>(playlistNames.Length);
            foreach (string name in playlistNames)
            {
                infos.Add(new PlaylistInfo(name));
            }

            playlistsScreen.SetPlaylists(infos);
            playlistsScreen.Hide();
        }

        if (trackListScreen != null)
        {
            trackListScreen.Hide();
        }
    }

    /// <summary>Opens the playlists modal (routed from the music player).</summary>
    public void OpenPlaylists()
    {
        if (trackListScreen != null)
        {
            trackListScreen.Hide();
        }

        if (playlistsScreen != null)
        {
            playlistsScreen.Show();
        }
    }

    /// <summary>Closes the playlists modal.</summary>
    public void ClosePlaylists()
    {
        if (playlistsScreen != null)
        {
            playlistsScreen.Hide();
        }
    }

    /// <summary>Opens the track listing for a chosen playlist, drilling in from the library.</summary>
    public void OpenTrackList(int playlistIndex)
    {
        if (trackListScreen == null)
        {
            return;
        }

        string playlistName = playlistIndex >= 0 && playlistIndex < playlistNames.Length
            ? playlistNames[playlistIndex]
            : "Playlist";

        trackListScreen.SetTracks(playlistName, BuildTrackRows());

        if (musicPlayerService != null)
        {
            trackListScreen.SetNowPlaying(musicPlayerService.CurrentTrackIndex);
        }

        if (playlistsScreen != null)
        {
            playlistsScreen.Hide();
        }

        trackListScreen.Show();
    }

    /// <summary>Closes the whole music panel from the track listing (both the listing and the library).</summary>
    public void CloseTrackList()
    {
        if (trackListScreen != null)
        {
            trackListScreen.Hide();
        }

        if (playlistsScreen != null)
        {
            playlistsScreen.Hide();
        }
    }

    /// <summary>Steps back from the track listing to the playlists library.</summary>
    public void GoBackToPlaylists()
    {
        if (trackListScreen != null)
        {
            trackListScreen.Hide();
        }

        if (playlistsScreen != null)
        {
            playlistsScreen.Show();
        }
    }

    /// <summary>Builds the display rows for the current playlist, seeding favourite state from the service.</summary>
    private List<TrackRowInfo> BuildTrackRows()
    {
        var rows = new List<TrackRowInfo>();
        if (musicPlayerService == null)
        {
            return rows;
        }

        IReadOnlyList<MusicTrack> tracks = musicPlayerService.Tracks;
        for (int i = 0; i < tracks.Count; i++)
        {
            rows.Add(new TrackRowInfo(tracks[i].title, tracks[i].artist, musicPlayerService.IsFavorited(i)));
        }

        return rows;
    }

    /// <summary>Routes a track-list heart toggle into the service, the single source of favourites.</summary>
    private void OnTrackFavoriteToggled(int index, bool favorited)
    {
        if (musicPlayerService != null)
        {
            musicPlayerService.SetFavorite(index, favorited);
        }
    }

    /// <summary>Plays the track chosen from the listing.</summary>
    private void OnTrackSelected(int index)
    {
        if (musicPlayerService != null)
        {
            musicPlayerService.PlayTrack(index);
        }
    }

    /// <summary>Moves the now-playing highlight to the track the service just loaded.</summary>
    private void OnTrackChanged(int index)
    {
        if (trackListScreen != null)
        {
            trackListScreen.SetNowPlaying(index);
        }
    }

    /// <summary>Pushes the service's favourite state back onto the visible track rows so they stay in sync.</summary>
    private void RefreshTrackListFavorites()
    {
        if (musicPlayerService == null || trackListScreen == null)
        {
            return;
        }

        IReadOnlyList<MusicTrack> tracks = musicPlayerService.Tracks;
        for (int i = 0; i < tracks.Count; i++)
        {
            trackListScreen.SetFavorite(i, musicPlayerService.IsFavorited(i));
        }
    }

    /// <summary>Shows the options menu and hides the scene menu.</summary>
    public void OpenOptions()
    {
        optionsMenu.SetActive(true);
        sceneMenu.SetActive(false);
    }

    /// <summary>Loads the scene above the current one.</summary>
    public void GoUp()
    {
        SceneManager.LoadScene(1);
    }

    /// <summary>Loads the scene below the current one.</summary>
    public void GoDown()
    {
        SceneManager.LoadScene(0);
    }
}
