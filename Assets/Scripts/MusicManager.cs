using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    [Serializable]
    private class MusicData
    {
        public AudioClip clip;
        [Min(1f)] public float bpm = 120f;
        [Min(0f), Tooltip("Seconds from the start of the clip to the first beat.")]
        public float firstBeatOffset;
    }

    [SerializeField] private List<MusicData> musicList = new List<MusicData>();
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool loop = true;

    public event Action<int> Beat;

    public double SecondsPerBeat { get; private set; }
    public AudioClip CurrentClip => audioSource != null ? audioSource.clip : null;

    private AudioSource audioSource;
    private MusicData currentMusic;
    private double songStartDspTime;
    private double nextBeatDspTime;
    private int beatIndex;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = loop;
    }

    private void Start()
    {
        if (playOnStart)
        {
            PlayRandomMusic();
        }
    }

    private void Update()
    {
        if (currentMusic == null || !audioSource.isPlaying)
        {
            return;
        }

        double dspTime = AudioSettings.dspTime;
        while (dspTime >= nextBeatDspTime)
        {
            Beat?.Invoke(beatIndex);
            beatIndex++;
            nextBeatDspTime = songStartDspTime + currentMusic.firstBeatOffset + beatIndex * SecondsPerBeat;
        }
    }

    public void PlayRandomMusic()
    {
        if (musicList == null || musicList.Count == 0)
        {
            Debug.LogWarning("MusicManager has no music configured.", this);
            return;
        }

        List<MusicData> playableMusic = musicList.FindAll(music => music.clip != null && music.bpm > 0f);
        if (playableMusic.Count == 0)
        {
            Debug.LogWarning("No playable music was found. Check AudioClip and BPM settings.", this);
            return;
        }

        Play(playableMusic[UnityEngine.Random.Range(0, playableMusic.Count)]);
    }

    private void Play(MusicData music)
    {
        currentMusic = music;
        SecondsPerBeat = 60d / music.bpm;
        beatIndex = 0;

        audioSource.Stop();
        audioSource.clip = music.clip;
        audioSource.loop = loop;

        songStartDspTime = AudioSettings.dspTime + 0.1d;
        nextBeatDspTime = songStartDspTime + music.firstBeatOffset;
        audioSource.PlayScheduled(songStartDspTime);
    }
}
