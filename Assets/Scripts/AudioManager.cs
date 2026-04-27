using UnityEngine;
using System;


    /// <summary>
    /// Defines the specific sound events in the application.
    /// </summary>
    public enum SoundType
    {
        UIClick,
        Success,
        Error,
        BackgroundMusic
    }

    /// <summary>
    /// Defines a playable audio clip and its properties for the inspector.
    /// </summary>
    [Serializable]
    public class Sound
    {
        public SoundType soundType;
        public AudioClip clip;

        [Range(0f, 1f)]
        public float volume = 1f;

        [Range(0.1f, 3f)]
        public float pitch = 1f;

        public bool loop;

        [HideInInspector]
        public AudioSource source;
    }

/// <summary>
/// A centralized global manager for playing 2D UI and Music audio using enums.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Dictionary")]
    public Sound[] sounds;

    // Sets up the singleton and generates an AudioSource for every sound in the array
    private void Awake()
    {
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

        foreach (Sound s in sounds)
        {
            s.source = gameObject.AddComponent<AudioSource>();
            s.source.clip = s.clip;
            s.source.volume = s.volume;
            s.source.pitch = s.pitch;
            s.source.loop = s.loop;
        }
    }

    // Finds a sound by its enum type and plays it
    public void Play(SoundType type)
    {
        Sound s = Array.Find(sounds, sound => sound.soundType == type);

        if (s == null)
        {
            Debug.LogWarning($"SoundType: {type} not found in AudioManager!");
            return;
        }

        if (s.loop)
        {
            if (!s.source.isPlaying) s.source.Play();
        }
        else
        {
            // OneShot allows overlapping clicks without cutting each other off
            s.source.PlayOneShot(s.clip);
        }
    }

    // Stops a looping sound
    public void Stop(SoundType type)
    {
        Sound s = Array.Find(sounds, sound => sound.soundType == type);
        if (s != null && s.source.isPlaying)
        {
            s.source.Stop();
        }
    }
}