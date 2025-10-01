using UnityEngine;

public class MusicManager : MonoBehaviour
{
    [Header("Background Music")]
    public AudioClip[] backgroundMusicTracks;
    public float musicVolume = 0.5f;
    public bool playMusicOnStart = true;
    
    private AudioSource audioSource;
    private int currentTrackIndex = 0;
    private string currentSceneName = "";
    
    public static MusicManager Instance { get; private set; }
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Subscribe to scene loading events
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Configure audio source
        audioSource.loop = true;
        audioSource.volume = musicVolume;
        audioSource.playOnAwake = false;
        
        // Start playing music if enabled
        if (playMusicOnStart && backgroundMusicTracks.Length > 0)
        {
            PlayCurrentTrack();
        }
    }
    
    void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    
    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        string newSceneName = scene.name;
        
        // Check if we're moving to a different level
        // Only restart if we're actually changing to a different scene
        if (currentSceneName != "" && newSceneName != currentSceneName)
        {
            Debug.Log($"MusicManager: New level detected - {newSceneName} (was {currentSceneName})");
            
            // Restart music for new level
            RestartMusicForNewLevel();
        }
        else if (currentSceneName == "")
        {
            Debug.Log($"MusicManager: First scene load - {newSceneName}, starting music");
            // First time loading any scene, just start music
            if (backgroundMusicTracks.Length > 0 && !audioSource.isPlaying)
            {
                PlayCurrentTrack();
            }
        }
        else
        {
            Debug.Log($"MusicManager: Same level restarted - {newSceneName}, keeping music playing");
        }
        
        currentSceneName = newSceneName;
    }
    
    private void RestartMusicForNewLevel()
    {
        if (backgroundMusicTracks.Length > 0)
        {
            // Stop current music
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
            
            // Start playing from the beginning
            PlayCurrentTrack();
        }
    }
    
    private void PlayCurrentTrack()
    {
        if (backgroundMusicTracks.Length > 0 && currentTrackIndex < backgroundMusicTracks.Length)
        {
            audioSource.clip = backgroundMusicTracks[currentTrackIndex];
            audioSource.Play();
            Debug.Log($"MusicManager: Playing track {currentTrackIndex + 1}/{backgroundMusicTracks.Length} - {audioSource.clip.name}");
        }
    }
    
    public void SetVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (audioSource != null)
        {
            audioSource.volume = musicVolume;
        }
    }
    
    public void PlayNextTrack()
    {
        if (backgroundMusicTracks.Length > 1)
        {
            currentTrackIndex = (currentTrackIndex + 1) % backgroundMusicTracks.Length;
            PlayCurrentTrack();
        }
    }
    
    public void PlayPreviousTrack()
    {
        if (backgroundMusicTracks.Length > 1)
        {
            currentTrackIndex = (currentTrackIndex - 1 + backgroundMusicTracks.Length) % backgroundMusicTracks.Length;
            PlayCurrentTrack();
        }
    }
    
    public void StopMusic()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }
    
    public void ResumeMusic()
    {
        if (audioSource != null && !audioSource.isPlaying && backgroundMusicTracks.Length > 0)
        {
            PlayCurrentTrack();
        }
    }
    
    public bool IsPlaying()
    {
        return audioSource != null && audioSource.isPlaying;
    }
}
