using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central owner of screen visibility for the scene. Screens never show or hide
/// themselves; they expose Show()/Hide() and raise intent events, and this
/// manager routes those into visibility changes.
/// </summary>
[DefaultExecutionOrder(1000)]
public class UIManager : MonoBehaviour
{
    public GameObject optionsMenu;
    public GameObject sceneMenu;

    [Header("Music Player")]
    [SerializeField] private MusicPlayerScreen musicPlayerScreen;
    [SerializeField] private PlaylistsScreen playlistsScreen;

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
        }
    }

    private void Start()
    {
        if (playlistsScreen == null)
        {
            return;
        }

        var infos = new List<PlaylistInfo>(playlistNames.Length);
        foreach (string name in playlistNames)
        {
            infos.Add(new PlaylistInfo(name));
        }

        playlistsScreen.SetPlaylists(infos);
        playlistsScreen.Hide();
    }

    /// <summary>Opens the playlists modal (routed from the music player).</summary>
    public void OpenPlaylists()
    {
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
