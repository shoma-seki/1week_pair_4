using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// シーンに配置済みのスポットライトを、手前から奥へ順番に点灯します。
/// </summary>
public class RunwaySpotlights : MonoBehaviour
{
    [Header("Target Lights")]
    [Tooltip("空の場合は、シーン内にある全ての Spot Light を自動収集します。")]
    [SerializeField] private List<Light> spotlights = new List<Light>();

    [Header("Sequence")]
    [SerializeField, Min(0f)] private float startDelay;
    [SerializeField, Min(0.01f)] private float interval = 0.25f;
    [SerializeField] private bool playOnStart = true;
    [Tooltip("最後まで点灯した後、先頭から繰り返します。")]
    [SerializeField] private bool loop;
    [Tooltip("再生開始時に対象ライトを全て消灯します。")]
    [SerializeField] private bool turnOffBeforePlay = true;

    private static RunwaySpotlights automaticController;
    private Coroutine sequenceCoroutine;
    private bool usesAutomaticTargets;

    private void Awake()
    {
        usesAutomaticTargets = spotlights.Count == 0;
        RefreshLights();
    }

    private void OnEnable()
    {
        if (!usesAutomaticTargets)
        {
            return;
        }

        if (automaticController != null && automaticController != this)
        {
            Debug.LogWarning("Spot Light の自動収集コントローラーが複数あるため、このコンポーネントを無効化しました。", this);
            enabled = false;
            return;
        }

        automaticController = this;
    }

    private void OnDisable()
    {
        Stop();
        if (automaticController == this)
        {
            automaticController = null;
        }
    }

    private void Start()
    {
        if (playOnStart)
        {
            Play();
        }
    }

    [ContextMenu("Play")]
    public void Play()
    {
        Stop();
        RefreshLights();

        if (spotlights.Count == 0)
        {
            Debug.LogWarning("点灯対象の Spot Light が見つかりません。", this);
            return;
        }

        sequenceCoroutine = StartCoroutine(PlaySequence());
    }

    [ContextMenu("Stop")]
    public void Stop()
    {
        if (sequenceCoroutine == null)
        {
            return;
        }

        StopCoroutine(sequenceCoroutine);
        sequenceCoroutine = null;
    }

    [ContextMenu("Turn Off All")]
    public void TurnOffAll()
    {
        foreach (Light spotlight in spotlights)
        {
            if (spotlight != null)
            {
                spotlight.enabled = false;
            }
        }
    }

    [ContextMenu("Refresh Lights")]
    public void RefreshLights()
    {
        spotlights.RemoveAll(light => light == null || light.type != LightType.Spot);

        if (spotlights.Count == 0)
        {
            Light[] sceneLights = FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Light lightComponent in sceneLights)
            {
                if (lightComponent.type == LightType.Spot)
                {
                    spotlights.Add(lightComponent);
                }
            }
        }

        spotlights.Sort((a, b) => a.transform.position.z.CompareTo(b.transform.position.z));
    }

    private IEnumerator PlaySequence()
    {
        if (turnOffBeforePlay)
        {
            TurnOffAll();
        }

        if (startDelay > 0f)
        {
            yield return new WaitForSeconds(startDelay);
        }

        do
        {
            foreach (Light spotlight in spotlights)
            {
                if (spotlight != null)
                {
                    spotlight.enabled = true;
                }

                yield return new WaitForSeconds(interval);
            }

            if (loop)
            {
                TurnOffAll();
            }
        }
        while (loop);

        sequenceCoroutine = null;
    }
}
