using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [Header("Sound Effects")]
    public AudioClip doorOpenSound;
    public AudioClip playerFlattenSound;
    public AudioClip trapDeathSound;
    
    [Header("Audio Settings")]
    public float soundVolume = 0.7f;
    
    private AudioSource audioSource;
    
    public static SoundManager Instance { get; private set; }
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
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
        
        // Configure audio source for sound effects
        audioSource.loop = false;
        audioSource.volume = soundVolume;
        audioSource.playOnAwake = false;
    }
    
    public void PlayDoorOpenSound()
    {
        if (doorOpenSound != null)
        {
            audioSource.PlayOneShot(doorOpenSound);
        }
    }
    
    public void PlayPlayerFlattenSound()
    {
        if (playerFlattenSound != null)
        {
            audioSource.PlayOneShot(playerFlattenSound);
        }
    }
    
    public void PlayTrapDeathSound()
    {
        if (trapDeathSound != null)
        {
            audioSource.PlayOneShot(trapDeathSound);
        }
    }
    
    public void SetVolume(float volume)
    {
        soundVolume = Mathf.Clamp01(volume);
        if (audioSource != null)
        {
            audioSource.volume = soundVolume;
        }
    }
}
