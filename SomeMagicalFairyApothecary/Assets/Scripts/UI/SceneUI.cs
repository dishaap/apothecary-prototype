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
    /// <summary>Grid index of the auto-populated Favourites playlist (always shown first).</summary>
    private const int FavoritesPlaylistIndex = 0;

    public GameObject optionsMenu;
    public GameObject sceneMenu;

    [Header("Music Player")]
    [SerializeField] private MusicPlayerScreen musicPlayerScreen;
    [SerializeField] private PlaylistsScreen playlistsScreen;
    [SerializeField] private TrackListScreen trackListScreen;

    [Tooltip("Audio model that owns the tracks and the shared favourite state.")]
    [SerializeField] private MusicPlayerService musicPlayerService;

    [Tooltip("Name of the auto-populated favourites playlist shown first in the library.")]
    [SerializeField] private string favoritesPlaylistName = "Favourites";

    [Tooltip("Playlist names shown in the playlists modal (display data).")]
    [SerializeField]
    private string[] playlistNames =
    {
        "Nightfall",
        "Dewdrops",
        "Wisteria",
        "A Cottage Morning"
    };

    [Header("To-Do")]
    [SerializeField] private TodoListScreen todoListScreen;

    [Tooltip("On-screen launcher button that opens the to-do list.")]
    [SerializeField] private TodoButtonComponent todoButton;

    [Tooltip("Model that owns the player's to-do tasks and their completion state.")]
    [SerializeField] private TodoService todoService;

    [Tooltip("Heading shown at the top of the to-do list panel.")]
    [SerializeField] private string todoListTitle = "To-Do List";

    // Display names for every tile, index-aligned with the playlists grid. The
    // Favourites playlist occupies index 0; the serialized names follow.
    private readonly List<string> _playlistDisplayNames = new List<string>();

    // Grid index of the playlist currently shown in the track list, or -1 when the
    // track list is closed. Used to rebuild Favourites live as favourites change.
    private int _openPlaylistIndex = -1;

    // Set when the player adds a task, so the next to-do rebuild scrolls the newly
    // added row into view at the bottom of the list.
    private bool _scrollTodoToNewestOnRebuild;

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

        if (todoListScreen != null)
        {
            todoListScreen.CloseRequested += CloseTodoList;
            todoListScreen.TaskToggled += OnTaskToggled;
            todoListScreen.TaskAddRequested += OnTaskAddRequested;
            todoListScreen.TaskDeleteRequested += OnTaskDeleteRequested;
        }

        if (todoButton != null)
        {
            todoButton.Clicked += ToggleTodoList;
        }

        if (todoService != null)
        {
            todoService.TasksChanged += RefreshTodoList;
            todoService.TasksListChanged += RefreshTodoListStructure;
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

        if (todoListScreen != null)
        {
            todoListScreen.CloseRequested -= CloseTodoList;
            todoListScreen.TaskToggled -= OnTaskToggled;
            todoListScreen.TaskAddRequested -= OnTaskAddRequested;
            todoListScreen.TaskDeleteRequested -= OnTaskDeleteRequested;
        }

        if (todoButton != null)
        {
            todoButton.Clicked -= ToggleTodoList;
        }

        if (todoService != null)
        {
            todoService.TasksChanged -= RefreshTodoList;
            todoService.TasksListChanged -= RefreshTodoListStructure;
        }
    }

    private void Start()
    {
        if (playlistsScreen != null)
        {
            // Favourites is always the first tile, followed by the authored playlists.
            _playlistDisplayNames.Clear();
            _playlistDisplayNames.Add(favoritesPlaylistName);
            _playlistDisplayNames.AddRange(playlistNames);

            var infos = new List<PlaylistInfo>(_playlistDisplayNames.Count);
            foreach (string name in _playlistDisplayNames)
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

        if (todoListScreen != null)
        {
            todoListScreen.Hide();
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

        _openPlaylistIndex = playlistIndex;

        string playlistName = playlistIndex >= 0 && playlistIndex < _playlistDisplayNames.Count
            ? _playlistDisplayNames[playlistIndex]
            : "Playlist";

        trackListScreen.SetTracks(playlistName, BuildTrackRows(playlistIndex));

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
        _openPlaylistIndex = -1;

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
        _openPlaylistIndex = -1;

        if (trackListScreen != null)
        {
            trackListScreen.Hide();
        }

        if (playlistsScreen != null)
        {
            playlistsScreen.Show();
        }
    }

    /// <summary>
    /// Builds the display rows for a playlist, seeding favourite state from the
    /// service. The Favourites playlist is auto-populated: only favourited tracks
    /// are included. Every row carries its authoritative service track index.
    /// </summary>
    private List<TrackRowInfo> BuildTrackRows(int playlistIndex)
    {
        var rows = new List<TrackRowInfo>();
        if (musicPlayerService == null)
        {
            return rows;
        }

        bool favoritesOnly = playlistIndex == FavoritesPlaylistIndex;
        IReadOnlyList<MusicTrack> tracks = musicPlayerService.Tracks;
        for (int i = 0; i < tracks.Count; i++)
        {
            bool isFavorited = musicPlayerService.IsFavorited(i);
            if (favoritesOnly && !isFavorited)
            {
                continue;
            }

            rows.Add(new TrackRowInfo(i, tracks[i].title, tracks[i].artist, isFavorited));
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

        // The Favourites playlist's very membership changes with favourites, so when
        // it is the open playlist rebuild it (deferred, since this can fire from a
        // heart toggle inside that same list). Other playlists just refresh hearts.
        if (_openPlaylistIndex == FavoritesPlaylistIndex)
        {
            trackListScreen.SetTracksDeferred(
                _playlistDisplayNames[FavoritesPlaylistIndex],
                BuildTrackRows(FavoritesPlaylistIndex),
                musicPlayerService.CurrentTrackIndex);
            return;
        }

        IReadOnlyList<MusicTrack> tracks = musicPlayerService.Tracks;
        for (int i = 0; i < tracks.Count; i++)
        {
            trackListScreen.SetFavorite(i, musicPlayerService.IsFavorited(i));
        }
    }

    /// <summary>Opens the to-do list panel, rebuilding its rows from the current tasks.</summary>
    public void OpenTodoList()
    {
        if (todoListScreen == null)
        {
            return;
        }

        todoListScreen.SetTitle(todoListTitle);
        todoListScreen.SetTasks(BuildTodoItems());
        todoListScreen.Show();
    }

    /// <summary>Closes the to-do list panel.</summary>
    public void CloseTodoList()
    {
        if (todoListScreen != null)
        {
            todoListScreen.Hide();
        }
    }

    /// <summary>Shows the to-do list panel if hidden, hides it if shown (handy for a single toggle button).</summary>
    public void ToggleTodoList()
    {
        if (todoListScreen == null)
        {
            return;
        }

        todoListScreen.SetTitle(todoListTitle);
        todoListScreen.SetTasks(BuildTodoItems());
        todoListScreen.Toggle();
    }

    /// <summary>Builds the display rows for the to-do list from the service's tasks.</summary>
    private List<TodoItemInfo> BuildTodoItems()
    {
        var items = new List<TodoItemInfo>();
        if (todoService == null)
        {
            return items;
        }

        IReadOnlyList<TodoTask> tasks = todoService.Tasks;
        for (int i = 0; i < tasks.Count; i++)
        {
            items.Add(new TodoItemInfo(tasks[i].title, tasks[i].done));
        }

        return items;
    }

    /// <summary>Routes a row toggle into the service, the single source of task completion.</summary>
    private void OnTaskToggled(int index, bool done)
    {
        if (todoService != null)
        {
            todoService.SetDone(index, done);
        }
    }

    /// <summary>Routes a newly typed task into the service, which owns and persists the list.</summary>
    private void OnTaskAddRequested(string title)
    {
        if (todoService != null)
        {
            // Flag so the follow-up rebuild scrolls the new task into view at the bottom.
            _scrollTodoToNewestOnRebuild = true;
            todoService.AddTask(title);
        }
    }

    /// <summary>Routes a row's delete request into the service, which owns and persists the list.</summary>
    private void OnTaskDeleteRequested(int index)
    {
        if (todoService != null)
        {
            todoService.RemoveTask(index);
        }
    }

    /// <summary>Pushes the service's completion state back onto the visible rows so they stay in sync.</summary>
    private void RefreshTodoList()
    {
        if (todoService == null || todoListScreen == null)
        {
            return;
        }

        IReadOnlyList<TodoTask> tasks = todoService.Tasks;
        for (int i = 0; i < tasks.Count; i++)
        {
            todoListScreen.SetDone(i, todoService.IsDone(i));
        }
    }

    /// <summary>
    /// Rebuilds the whole list after a structural change (add/remove). Deferred,
    /// since a delete fires from within the row's own click while it is being removed.
    /// A preceding add asks the list to scroll the new task into view.
    /// </summary>
    private void RefreshTodoListStructure()
    {
        if (todoService == null || todoListScreen == null)
        {
            return;
        }

        bool scrollToNewest = _scrollTodoToNewestOnRebuild;
        _scrollTodoToNewestOnRebuild = false;
        todoListScreen.SetTasksDeferred(BuildTodoItems(), scrollToNewest);
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
