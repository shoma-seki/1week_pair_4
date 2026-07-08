using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>既存のSpot LightをZが小さい順に点灯します。</summary>
public class RunwaySpotlights : MonoBehaviour
{
    [SerializeField] private List<Light> spotlights = new List<Light>();
    [Header("Result Camera Sync")]
    [SerializeField] private ResultCameraMover resultCamera;
    [Tooltip("カメラより何m先のライトまで点灯するか。")]
    [SerializeField] private float activationOffset;
    [Header("Fallback Sequence")]
    [SerializeField, Min(0.01f)] private float interval = 0.25f;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool turnOffBeforePlay = true;

    private static RunwaySpotlights automaticController;
    private Coroutine sequenceCoroutine;
    private bool usesAutomaticTargets;
    private int nextLightIndex;

    private void Awake()
    {
        usesAutomaticTargets = spotlights.Count == 0;
        RefreshLights();
    }

    private void OnEnable()
    {
        if (!usesAutomaticTargets) return;
        if (automaticController != null && automaticController != this)
        {
            enabled = false;
            return;
        }
        automaticController = this;
    }

    private void Start()
    {
        if (resultCamera == null) resultCamera = FindAnyObjectByType<ResultCameraMover>();
        if (playOnStart) Play();
    }

    private void Update()
    {
        if (resultCamera == null) return;

        float activationZ = resultCamera.transform.position.z + activationOffset;
        while (nextLightIndex < spotlights.Count)
        {
            Light spotlight = spotlights[nextLightIndex];
            if (spotlight != null && spotlight.transform.position.z > activationZ) break;
            if (spotlight != null) spotlight.enabled = true;
            nextLightIndex++;
        }
    }

    private void OnDisable()
    {
        Stop();
        if (automaticController == this) automaticController = null;
    }

    [ContextMenu("Play")]
    public void Play()
    {
        Stop();
        RefreshLights();
        nextLightIndex = 0;
        if (turnOffBeforePlay) TurnOffAll();
        if (resultCamera == null && spotlights.Count > 0)
            sequenceCoroutine = StartCoroutine(PlayTimedSequence());
    }

    [ContextMenu("Stop")]
    public void Stop()
    {
        if (sequenceCoroutine == null) return;
        StopCoroutine(sequenceCoroutine);
        sequenceCoroutine = null;
    }

    [ContextMenu("Turn Off All")]
    public void TurnOffAll()
    {
        foreach (Light spotlight in spotlights)
            if (spotlight != null) spotlight.enabled = false;
    }

    [ContextMenu("Turn On All")]
    public void TurnOnAll()
    {
        Stop();
        RefreshLights();
        foreach (Light spotlight in spotlights)
            if (spotlight != null) spotlight.enabled = true;
        nextLightIndex = spotlights.Count;
    }

    [ContextMenu("Refresh Lights")]
    public void RefreshLights()
    {
        spotlights.RemoveAll(light => light == null || light.type != LightType.Spot);
        if (spotlights.Count == 0)
        {
            Light[] sceneLights = FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (Light lightComponent in sceneLights)
                if (lightComponent.type == LightType.Spot) spotlights.Add(lightComponent);
        }
        spotlights.Sort((a, b) => a.transform.position.z.CompareTo(b.transform.position.z));
    }

    private IEnumerator PlayTimedSequence()
    {
        foreach (Light spotlight in spotlights)
        {
            if (spotlight != null) spotlight.enabled = true;
            yield return new WaitForSeconds(interval);
        }
        sequenceCoroutine = null;
    }
}
