using UnityEngine;

public class GameAudioManager : MonoBehaviour
{
    [Header("Urine")]
    [SerializeField] private AudioClip shobenClip;
    [SerializeField] private AudioClip powerShobenClip;
    [SerializeField] private AudioClip shobenUpClip;
    [SerializeField] private AudioClip reloadClip;

    [Header("Actions")]
    [SerializeField] private AudioClip posingClip;
    [SerializeField] private AudioClip obstacleClip;
    [SerializeField] private AudioClip fanIncreaseClip;
    [SerializeField] private AudioClip bechaClip;

    [Header("Sources")]
    [SerializeField, Range(0f, 1f)] private float loopVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float oneShotVolume = 1f;

    private AudioSource loopSource;
    private AudioSource oneShotSource;

    public static GameAudioManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        PrepareSources();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void PlayUrineLoop(Player.UrineStage stage)
    {
        AudioClip clip = stage == Player.UrineStage.Third ? powerShobenClip : shobenClip;
        if (clip == null)
        {
            StopUrineLoop();
            return;
        }

        PrepareSources();
        if (loopSource.clip == clip && loopSource.isPlaying)
        {
            return;
        }

        loopSource.clip = clip;
        loopSource.volume = loopVolume;
        loopSource.loop = true;
        loopSource.Play();
    }

    public void StopUrineLoop()
    {
        if (loopSource == null)
        {
            return;
        }

        loopSource.Stop();
        loopSource.clip = null;
    }

    public void PlayShobenUp() => PlayOneShot(shobenUpClip);
    public void PlayReload() => PlayOneShot(reloadClip);
    public void PlayPosing() => PlayOneShot(posingClip);
    public void PlayObstacle() => PlayOneShot(obstacleClip);
    public void PlayFanIncrease() => PlayOneShot(fanIncreaseClip);
    public void PlayBecha() => PlayOneShot(bechaClip);

    private void PlayOneShot(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        PrepareSources();
        oneShotSource.PlayOneShot(clip, oneShotVolume);
    }

    private void PrepareSources()
    {
        if (loopSource == null)
        {
            loopSource = gameObject.AddComponent<AudioSource>();
            loopSource.playOnAwake = false;
        }

        if (oneShotSource == null)
        {
            oneShotSource = gameObject.AddComponent<AudioSource>();
            oneShotSource.playOnAwake = false;
        }
    }
}
