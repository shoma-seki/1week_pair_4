using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    public enum UrineStage { First, Second, Third }

    [Header("Urine Resource")]
    [SerializeField, Min(0.01f)] private float maxUrine = 100f;
    [SerializeField, Min(0f)] private float urineConsumptionPerSecond = 20f;

    [Header("Urine Stages")]
    [SerializeField, Min(0f)] private float secondStageTime = 0.5f;
    [SerializeField, Min(0f)] private float thirdStageTime = 1.5f;
    [SerializeField, Min(0f)] private float firstStageStrengthMultiplier = 1.25f;
    [SerializeField, Min(0f)] private float secondStageStrengthMultiplier = 1.5f;
    [SerializeField, Min(0f)] private float thirdStageStrengthMultiplier = 2f;
    [SerializeField, Range(0f, 1f)] private float secondStageAimSpeedMultiplier = 0.5f;
    [SerializeField, Range(0f, 1f)] private float thirdStageAimSpeedMultiplier = 0.25f;
    [SerializeField, Range(0.05f, 1f)] private float secondStageArcHeightMultiplier = 0.7f;
    [SerializeField, Range(0.05f, 1f)] private float thirdStageArcHeightMultiplier = 0.35f;

    [Header("Beat Movement")]
    [SerializeField, Min(0f)] private float distancePerBeat = 2.5f;
    [SerializeField, Range(0.05f, 1f)] private float moveDurationInBeats = 0.5f;
    [SerializeField] private bool requireMouseButton = true;

    [Header("Puni Puni Motion")]
    [SerializeField, Range(0f, 0.5f)] private float stretchAmount = 0.12f;
    [SerializeField, Range(0f, 0.5f)] private float squashAmount = 0.18f;

    [Header("Player Materials")]
    [SerializeField] private Renderer playerRenderer;
    [SerializeField, Tooltip("移動中に拍ごとに切り替える2つのマテリアル")]
    private Material[] movementMaterials = new Material[2];
    [SerializeField, Tooltip("左クリックを離したときにランダムで表示するマテリアル")]
    private Material[] stoppedMaterials;

    [Header("Obstacle Approach Feedback")]
    [SerializeField, Tooltip("障害物の接近時に一時的に表示する2つのUI Image")]
    private Image[] obstacleApproachImages = new Image[2];
    [SerializeField, Tooltip("障害物の接近時にImageと同時表示するTextMeshPro")]
    private TMP_Text obstacleApproachText;
    [SerializeField, Min(0.01f), Tooltip("Imageが現れるまでの時間（秒）")]
    private float obstacleApproachImageFadeInDuration = 0.15f;
    [SerializeField, Min(0f), Tooltip("Imageの表示を維持する時間（秒）")]
    private float obstacleApproachImageDuration = 1f;
    [SerializeField, Min(0.01f), Tooltip("Imageが消えるまでの時間（秒）")]
    private float obstacleApproachImageFadeOutDuration = 0.35f;

    [Header("Obstacle Hit Stop (Adjust Here)")]
    [SerializeField, Min(0f), Tooltip("Duration of the obstacle-hit stop in seconds.")]
    private float obstacleHitStopDuration = 0.08f;
    [SerializeField, Range(0f, 1f), Tooltip("Time scale during the hit stop. Zero completely freezes gameplay.")]
    private float obstacleHitStopTimeScale = 0f;
    [SerializeField, Tooltip("ヒットストップ中にCanvas上へ表示する2つのUI Image")]
    private Image[] obstacleHitImages = new Image[2];
    [SerializeField, Min(0f), Tooltip("1枚目から2枚目を表示するまでの実時間（秒）")]
    private float obstacleHitSecondImageDelay = 0.04f;

    private MusicManager musicManager;
    private CameraFollow cameraFollow;
    private Vector3 moveStart;
    private Vector3 moveTarget;
    private Vector3 originalScale;
    private float moveElapsed;
    private float moveDuration;
    private bool isMoving;
    private float currentUrine;
    private Vector3 startPosition;
    private float distanceTraveled;
    private bool canMove = true;
    private float urineHoldDuration;
    private int movementMaterialIndex;
    private System.Random materialRandom;
    private Coroutine obstacleApproachImageCoroutine;
    private Coroutine obstacleHitStopCoroutine;
    private float timeScaleBeforeHitStop = 1f;
    private float[] obstacleApproachImageTargetAlphas;
    private float obstacleApproachTextTargetAlpha = 1f;
    private bool wasUrinating;
    private UrineStage previousUrineStage;

    public float CurrentUrine => currentUrine;
    public float MaxUrine => maxUrine;
    public float UrineNormalized => maxUrine > 0f ? currentUrine / maxUrine : 0f;
    public bool CanUrinate => currentUrine > 0f;
    public float DistanceTraveled => distanceTraveled;
    public bool CanMove => canMove;
    public bool IsMoving => isMoving;
    public float UrineHoldDuration => urineHoldDuration;
    public UrineStage CurrentUrineStage => urineHoldDuration >= thirdStageTime
        ? UrineStage.Third
        : urineHoldDuration >= secondStageTime ? UrineStage.Second : UrineStage.First;
    public float CurrentStageStrengthMultiplier => CurrentUrineStage switch
    {
        UrineStage.Third => thirdStageStrengthMultiplier,
        UrineStage.Second => secondStageStrengthMultiplier,
        _ => firstStageStrengthMultiplier
    };
    public float CurrentAimSpeedMultiplier => CurrentUrineStage switch
    {
        UrineStage.Third => thirdStageAimSpeedMultiplier,
        UrineStage.Second => secondStageAimSpeedMultiplier,
        _ => 1f
    };
    public float CurrentArcHeightMultiplier => CurrentUrineStage switch
    {
        UrineStage.Third => thirdStageArcHeightMultiplier,
        UrineStage.Second => secondStageArcHeightMultiplier,
        _ => 1f
    };

    public event Action<float, float> UrineChanged;
    public event Action<float> DistanceChanged;
    public event Action ObstacleApproached;
    public event Action ObstacleHit;

    public void NotifyObstacleHit()
    {
        if (cameraFollow == null)
        {
            cameraFollow = FindAnyObjectByType<CameraFollow>();
        }

        // Trigger the camera directly before changing Time.timeScale.  This does
        // not depend on CameraFollow having subscribed to this Player in Start().
        cameraFollow?.PlayObstacleHitShake();
        PlayObstacleHitStop();
        ObstacleHit?.Invoke();
    }

    private void Awake()
    {
        currentUrine = maxUrine;
        previousUrineStage = CurrentUrineStage;
        materialRandom = new System.Random(Guid.NewGuid().GetHashCode());
        cameraFollow = FindAnyObjectByType<CameraFollow>();

        if (playerRenderer == null)
        {
            playerRenderer = GetComponentInChildren<Renderer>();
        }

        CacheObstacleApproachImageAlphas();
        SetObstacleApproachImagesEnabled(false);
        SetObstacleHitImagesEnabled(false);
    }

    private void Start()
    {
        startPosition = transform.position;
        originalScale = transform.localScale;
        musicManager = FindAnyObjectByType<MusicManager>();

        if (musicManager == null)
        {
            Debug.LogWarning("MusicManagerが見つからないため、プレイヤーは拍に合わせて移動できません。", this);
            return;
        }

        musicManager.Beat += OnBeat;
    }

    private void OnDestroy()
    {
        RestoreTimeScaleAfterHitStop();
        SetObstacleHitImagesEnabled(false);

        if (musicManager != null)
        {
            musicManager.Beat -= OnBeat;
        }
    }

    private void PlayObstacleHitStop()
    {
        if (obstacleHitStopDuration <= 0f)
        {
            SetObstacleHitImagesEnabled(false);
            return;
        }

        if (obstacleHitStopCoroutine != null)
        {
            StopCoroutine(obstacleHitStopCoroutine);
        }
        else
        {
            timeScaleBeforeHitStop = Time.timeScale;
        }

        obstacleHitStopCoroutine = StartCoroutine(ObstacleHitStop());
    }

    private IEnumerator ObstacleHitStop()
    {
        SetObstacleHitImagesEnabled(false);
        RandomizeObstacleHitImages();
        Time.timeScale = obstacleHitStopTimeScale;

        ShowObstacleHitImageWithSound(0);

        float secondImageDelay = Mathf.Min(obstacleHitSecondImageDelay, obstacleHitStopDuration);
        if (secondImageDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(secondImageDelay);
        }

        ShowObstacleHitImageWithSound(1);

        float remainingDuration = obstacleHitStopDuration - secondImageDelay;
        if (remainingDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(remainingDuration);
        }

        SetObstacleHitImagesEnabled(false);
        RestoreTimeScaleAfterHitStop();
    }

    private void ShowObstacleHitImageWithSound(int index)
    {
        if (obstacleHitImages == null || index < 0 || index >= obstacleHitImages.Length)
        {
            return;
        }

        Image image = obstacleHitImages[index];
        if (image == null)
        {
            return;
        }

        image.enabled = true;
        image.raycastTarget = false;
        GameAudioManager.Instance?.PlayBecha();
    }

    private void RandomizeObstacleHitImages()
    {
        foreach (Image image in obstacleHitImages ?? Array.Empty<Image>())
        {
            if (image == null)
            {
                continue;
            }

            RectTransform imageRect = image.rectTransform;
            RectTransform parentRect = imageRect.parent as RectTransform;
            if (parentRect != null)
            {
                // Keep the image's centre within its parent Canvas area.  The
                // half diagonal is used as padding so rotation does not push it
                // outside the screen when enough room is available.
                Vector2 parentSize = parentRect.rect.size;
                float halfDiagonal = imageRect.rect.size.magnitude * 0.5f;
                float horizontalPadding = parentSize.x > 0f
                    ? Mathf.Clamp01(halfDiagonal / parentSize.x)
                    : 0f;
                float verticalPadding = parentSize.y > 0f
                    ? Mathf.Clamp01(halfDiagonal / parentSize.y)
                    : 0f;

                float anchorX = horizontalPadding < 0.5f
                    ? UnityEngine.Random.Range(horizontalPadding, 1f - horizontalPadding)
                    : 0.5f;
                float anchorY = verticalPadding < 0.5f
                    ? UnityEngine.Random.Range(verticalPadding, 1f - verticalPadding)
                    : 0.5f;

                Vector2 randomAnchor = new Vector2(anchorX, anchorY);
                imageRect.anchorMin = randomAnchor;
                imageRect.anchorMax = randomAnchor;
                imageRect.anchoredPosition = Vector2.zero;
            }

            imageRect.localRotation = Quaternion.Euler(0f, 0f, UnityEngine.Random.Range(0f, 360f));
        }
    }

    private void SetObstacleHitImagesEnabled(bool isEnabled)
    {
        foreach (Image image in obstacleHitImages ?? Array.Empty<Image>())
        {
            if (image == null)
            {
                continue;
            }

            image.enabled = isEnabled;
            image.raycastTarget = false;
        }
    }

    private void RestoreTimeScaleAfterHitStop()
    {
        if (obstacleHitStopCoroutine == null)
        {
            return;
        }

        Time.timeScale = timeScaleBeforeHitStop;
        obstacleHitStopCoroutine = null;
    }

    private void Update()
    {
        UpdateDistanceTraveled();
        UpdateUrineResource();
        UpdateUrineStage();
        UpdateUrineAudio();
        UpdateMaterialState();

        if (!isMoving)
        {
            return;
        }

        moveElapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(moveElapsed / moveDuration);
        float jump = Mathf.Sin(progress * Mathf.PI);
        float landing = Mathf.Sin(Mathf.InverseLerp(0.7f, 1f, progress) * Mathf.PI);

        transform.position = Vector3.Lerp(moveStart, moveTarget, Mathf.SmoothStep(0f, 1f, progress));

        float verticalScale = 1f + jump * stretchAmount - landing * squashAmount;
        float horizontalScale = 1f - jump * stretchAmount * 0.35f + landing * squashAmount * 0.5f;
        transform.localScale = Vector3.Scale(originalScale, new Vector3(horizontalScale, verticalScale, horizontalScale));

        if (progress >= 1f)
        {
            isMoving = false;
            transform.position = moveTarget;
            transform.localScale = originalScale;
        }
    }

    public void StopMovement()
    {
        canMove = false;
        isMoving = false;
        transform.localScale = originalScale;
    }

    public void NotifyObstacleApproached()
    {
        if (obstacleApproachImageCoroutine != null)
        {
            StopCoroutine(obstacleApproachImageCoroutine);
        }

        obstacleApproachImageCoroutine = StartCoroutine(ShowObstacleApproachImages());
        GameAudioManager.Instance?.PlayObstacle();
        ObstacleApproached?.Invoke();
    }

    private IEnumerator ShowObstacleApproachImages()
    {
        SetObstacleApproachImagesAlpha(0f);
        SetObstacleApproachImagesEnabled(true);
        yield return AnimateObstacleApproachImageAlpha(0f, 1f, obstacleApproachImageFadeInDuration);
        yield return new WaitForSeconds(obstacleApproachImageDuration);
        yield return AnimateObstacleApproachImageAlpha(1f, 0f, obstacleApproachImageFadeOutDuration);
        SetObstacleApproachImagesEnabled(false);
        obstacleApproachImageCoroutine = null;
    }

    private IEnumerator AnimateObstacleApproachImageAlpha(float from, float to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            SetObstacleApproachImagesAlpha(Mathf.Lerp(from, to, progress));
            yield return null;
        }

        SetObstacleApproachImagesAlpha(to);
    }

    private void CacheObstacleApproachImageAlphas()
    {
        obstacleApproachImageTargetAlphas = new float[obstacleApproachImages?.Length ?? 0];

        for (int i = 0; i < obstacleApproachImageTargetAlphas.Length; i++)
        {
            obstacleApproachImageTargetAlphas[i] = obstacleApproachImages[i] != null
                ? obstacleApproachImages[i].color.a
                : 1f;
        }

        if (obstacleApproachText != null)
        {
            obstacleApproachTextTargetAlpha = obstacleApproachText.color.a;
        }
    }

    private void SetObstacleApproachImagesAlpha(float normalizedAlpha)
    {
        for (int i = 0; i < (obstacleApproachImages?.Length ?? 0); i++)
        {
            Image image = obstacleApproachImages[i];
            if (image == null)
            {
                continue;
            }

            Color color = image.color;
            float targetAlpha = i < obstacleApproachImageTargetAlphas.Length
                ? obstacleApproachImageTargetAlphas[i]
                : 1f;
            color.a = targetAlpha * normalizedAlpha;
            image.color = color;
        }

        if (obstacleApproachText != null)
        {
            Color color = obstacleApproachText.color;
            color.a = obstacleApproachTextTargetAlpha * normalizedAlpha;
            obstacleApproachText.color = color;
        }
    }

    private void SetObstacleApproachImagesEnabled(bool isEnabled)
    {
        foreach (Image image in obstacleApproachImages ?? Array.Empty<Image>())
        {
            if (image != null)
            {
                image.enabled = isEnabled;
            }
        }

        if (obstacleApproachText != null)
        {
            obstacleApproachText.enabled = isEnabled;
        }
    }

    private void UpdateDistanceTraveled()
    {
        float nextDistance = Mathf.Max(0f, transform.position.z - startPosition.z);

        if (Mathf.Approximately(nextDistance, distanceTraveled))
        {
            return;
        }

        distanceTraveled = nextDistance;
        DistanceChanged?.Invoke(distanceTraveled);
    }

    private void UpdateUrineResource()
    {
        float nextUrine = Input.GetMouseButton(0)
            ? Mathf.Max(0f, currentUrine - urineConsumptionPerSecond * Time.deltaTime)
            : maxUrine;

        if (Mathf.Approximately(nextUrine, currentUrine))
        {
            return;
        }

        currentUrine = nextUrine;
        UrineChanged?.Invoke(currentUrine, maxUrine);
    }

    private void UpdateUrineStage()
    {
        urineHoldDuration = Input.GetMouseButton(0) && CanUrinate
            ? urineHoldDuration + Time.deltaTime
            : 0f;
    }

    private void UpdateMaterialState()
    {
        if (Input.GetMouseButtonUp(0))
        {
            ShowRandomStoppedMaterial();
        }
        else if (Input.GetMouseButtonDown(0))
        {
            movementMaterialIndex = 0;
            ShowMovementMaterial();
        }
    }

    private void UpdateUrineAudio()
    {
        bool isUrinating = Input.GetMouseButton(0) && CanUrinate;
        UrineStage currentStage = CurrentUrineStage;

        if (isUrinating)
        {
            if (wasUrinating && currentStage > previousUrineStage)
            {
                GameAudioManager.Instance?.PlayShobenUp();
            }

            GameAudioManager.Instance?.PlayUrineLoop(currentStage);
        }
        else if (wasUrinating)
        {
            GameAudioManager.Instance?.StopUrineLoop();
        }

        if (Input.GetMouseButtonUp(0))
        {
            GameAudioManager.Instance?.PlayPosing();
            GameAudioManager.Instance?.PlayReload();
        }

        wasUrinating = isUrinating;
        previousUrineStage = currentStage;
    }

    private void ShowRandomStoppedMaterial()
    {
        if (playerRenderer == null || stoppedMaterials == null || stoppedMaterials.Length == 0)
        {
            return;
        }

        Material currentMaterial = playerRenderer.sharedMaterial;
        int candidateCount = 0;

        for (int index = 0; index < stoppedMaterials.Length; index++)
        {
            Material material = stoppedMaterials[index];
            if (material != null && material != currentMaterial)
            {
                candidateCount++;
            }
        }

        if (candidateCount == 0)
        {
            return;
        }

        int randomValue = materialRandom.Next(int.MaxValue);
        int selectedCandidate = randomValue % candidateCount;

        for (int index = 0; index < stoppedMaterials.Length; index++)
        {
            Material material = stoppedMaterials[index];
            if (material == null || material == currentMaterial)
            {
                continue;
            }

            if (selectedCandidate == 0)
            {
                playerRenderer.sharedMaterial = material;
                return;
            }

            selectedCandidate--;
        }
    }

    private void ShowMovementMaterial()
    {
        if (playerRenderer == null || movementMaterials == null || movementMaterials.Length < 2)
        {
            return;
        }

        Material material = movementMaterials[movementMaterialIndex];
        if (material != null)
        {
            playerRenderer.sharedMaterial = material;
        }
    }

    public float GetChargeStrengthMultiplier()
    {
        return CurrentStageStrengthMultiplier;
    }

    private void OnValidate()
    {
        maxUrine = Mathf.Max(0.01f, maxUrine);
        urineConsumptionPerSecond = Mathf.Max(0f, urineConsumptionPerSecond);
        secondStageTime = Mathf.Max(0f, secondStageTime);
        thirdStageTime = Mathf.Max(secondStageTime, thirdStageTime);
        firstStageStrengthMultiplier = Mathf.Max(0f, firstStageStrengthMultiplier);
        secondStageStrengthMultiplier = Mathf.Max(0f, secondStageStrengthMultiplier);
        thirdStageStrengthMultiplier = Mathf.Max(0f, thirdStageStrengthMultiplier);
        secondStageAimSpeedMultiplier = Mathf.Clamp01(secondStageAimSpeedMultiplier);
        thirdStageAimSpeedMultiplier = Mathf.Clamp01(thirdStageAimSpeedMultiplier);
        secondStageArcHeightMultiplier = Mathf.Clamp(secondStageArcHeightMultiplier, 0.05f, 1f);
        thirdStageArcHeightMultiplier = Mathf.Clamp(thirdStageArcHeightMultiplier, 0.05f, 1f);
        obstacleHitStopDuration = Mathf.Max(0f, obstacleHitStopDuration);
        obstacleHitStopTimeScale = Mathf.Clamp01(obstacleHitStopTimeScale);
        obstacleHitSecondImageDelay = Mathf.Max(0f, obstacleHitSecondImageDelay);

        if (Application.isPlaying && currentUrine > maxUrine)
        {
            currentUrine = maxUrine;
            UrineChanged?.Invoke(currentUrine, maxUrine);
        }
    }

    private void OnBeat(int beatIndex)
    {
        if (!canMove)
        {
            return;
        }

        if (requireMouseButton && !Input.GetMouseButton(0))
        {
            return;
        }

        movementMaterialIndex = 1 - movementMaterialIndex;
        ShowMovementMaterial();

        moveStart = transform.position;
        moveTarget = moveStart + Vector3.forward * distancePerBeat;
        moveElapsed = 0f;
        moveDuration = Mathf.Max(0.01f, (float)musicManager.SecondsPerBeat * moveDurationInBeats);
        isMoving = true;
    }
}
