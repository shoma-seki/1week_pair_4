using System;
using System.Collections;
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

    [Header("Check Collider Spawn")]
    [SerializeField, Tooltip("CheckColliderタグに触れたときに生成するPrefab")]
    private GameObject checkColliderSpawnPrefab;
    [SerializeField, Tooltip("触れたCheckColliderの位置から加算するオフセット")]
    private Vector3 checkColliderSpawnOffset;
    [SerializeField, Tooltip("接触時に一時的に表示する2つのUI Image")]
    private Image[] checkColliderImages = new Image[2];
    [SerializeField, Min(0.01f), Tooltip("Imageが現れるまでの時間（秒）")]
    private float checkColliderImageFadeInDuration = 0.15f;
    [SerializeField, Min(0f), Tooltip("Imageの表示を維持する時間（秒）")]
    private float checkColliderImageDuration = 1f;
    [SerializeField, Min(0.01f), Tooltip("Imageが消えるまでの時間（秒）")]
    private float checkColliderImageFadeOutDuration = 0.35f;

    private MusicManager musicManager;
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
    private Coroutine checkColliderImageCoroutine;
    private float[] checkColliderImageTargetAlphas;
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
    public event Action CheckColliderEntered;

    private void Awake()
    {
        currentUrine = maxUrine;
        previousUrineStage = CurrentUrineStage;
        materialRandom = new System.Random(Guid.NewGuid().GetHashCode());

        if (playerRenderer == null)
        {
            playerRenderer = GetComponentInChildren<Renderer>();
        }

        CacheCheckColliderImageAlphas();
        SetCheckColliderImagesEnabled(false);
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
        if (musicManager != null)
        {
            musicManager.Beat -= OnBeat;
        }
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

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("CheckCollider"))
        {
            return;
        }

        if (checkColliderSpawnPrefab != null)
        {
            Instantiate(
                checkColliderSpawnPrefab,
                other.transform.position + checkColliderSpawnOffset,
                other.transform.rotation);
        }

        if (checkColliderImageCoroutine != null)
        {
            StopCoroutine(checkColliderImageCoroutine);
        }

        checkColliderImageCoroutine = StartCoroutine(ShowCheckColliderImages());
        GameAudioManager.Instance?.PlayObstacle();
        CheckColliderEntered?.Invoke();
    }

    private IEnumerator ShowCheckColliderImages()
    {
        SetCheckColliderImagesAlpha(0f);
        SetCheckColliderImagesEnabled(true);
        yield return AnimateCheckColliderImageAlpha(0f, 1f, checkColliderImageFadeInDuration);
        yield return new WaitForSeconds(checkColliderImageDuration);
        yield return AnimateCheckColliderImageAlpha(1f, 0f, checkColliderImageFadeOutDuration);
        SetCheckColliderImagesEnabled(false);
        checkColliderImageCoroutine = null;
    }

    private IEnumerator AnimateCheckColliderImageAlpha(float from, float to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            SetCheckColliderImagesAlpha(Mathf.Lerp(from, to, progress));
            yield return null;
        }

        SetCheckColliderImagesAlpha(to);
    }

    private void CacheCheckColliderImageAlphas()
    {
        checkColliderImageTargetAlphas = new float[checkColliderImages?.Length ?? 0];

        for (int i = 0; i < checkColliderImageTargetAlphas.Length; i++)
        {
            checkColliderImageTargetAlphas[i] = checkColliderImages[i] != null
                ? checkColliderImages[i].color.a
                : 1f;
        }
    }

    private void SetCheckColliderImagesAlpha(float normalizedAlpha)
    {
        if (checkColliderImages == null)
        {
            return;
        }

        for (int i = 0; i < checkColliderImages.Length; i++)
        {
            Image image = checkColliderImages[i];
            if (image == null)
            {
                continue;
            }

            Color color = image.color;
            float targetAlpha = i < checkColliderImageTargetAlphas.Length
                ? checkColliderImageTargetAlphas[i]
                : 1f;
            color.a = targetAlpha * normalizedAlpha;
            image.color = color;
        }
    }

    private void SetCheckColliderImagesEnabled(bool isEnabled)
    {
        if (checkColliderImages == null)
        {
            return;
        }

        foreach (Image image in checkColliderImages)
        {
            if (image != null)
            {
                image.enabled = isEnabled;
            }
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
