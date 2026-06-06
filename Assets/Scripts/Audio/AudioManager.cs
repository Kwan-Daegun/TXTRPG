using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;

    [Header("Background Music Tracks")]
    public AudioClip mainMenuBGM;
    public AudioClip dungeonBGM;

    private void Awake()
    {
        // Singleton pattern to keep the audio manager alive across scenes
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Automatically switch music when a scene loads
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Adjust the scene names to match your project's build settings
        if (scene.name == "TitleScene")
        {
            PlayBGM(mainMenuBGM);
        }
        else if (scene.name == "DungeonScene")
        {
            PlayBGM(dungeonBGM);
        }
    }

    public void PlayBGM(AudioClip clip)
    {
        if (clip == null) return;

        // Prevent restarting the track if it is already playing
        if (bgmSource.clip == clip) return;

        bgmSource.clip = clip;
        bgmSource.Play();
    }
}