using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Audience : MonoBehaviour
{
    // シェーダー側のプロパティ名を毎回文字列で書かず、取り違えを防ぐためのID。
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int FanColorId = Shader.PropertyToID("_FanColor");
    private static readonly int FanFillId = Shader.PropertyToID("_FanFill");
    private static readonly int BoundsMinYId = Shader.PropertyToID("_BoundsMinY");
    private static readonly int BoundsMaxYId = Shader.PropertyToID("_BoundsMaxY");

    [Header("Fan")]
    [SerializeField, Min(0f)] private float fanPointPerSecond = 1f;
    [SerializeField, Min(0.01f)] private float fanPointToBecomeFan = 3f;

    [Header("Fan Up Effect")]
    [SerializeField] private GameObject fanUpPrefab;
    [SerializeField] private Vector3 fanUpHeadOffset = new Vector3(0f, 2f, 0f);
    [SerializeField, Min(0f)] private float fanUpRiseDistance = 1f;
    [SerializeField, Min(0f)] private float fanUpDuration = 1f;

    [Header("Movement")]
    [SerializeField] private Vector3 hitOffset;
    [SerializeField] private float moveDuration = 0.3f;
    [SerializeField] private InterpolationType interpolationType = InterpolationType.Linear;

    [Header("Dance Noise")]
    [SerializeField] private float danceAmplitude = 0.2f;
    [SerializeField] private float danceFrequency = 8f;

    [Header("Visual")]
    [SerializeField] private Renderer targetRenderer;
    [SerializeField] private Color spawnColor = Color.white;
    [SerializeField, Min(0f)] private float spawnColorDuration = 1f;
    [SerializeField] private Color targetColor = Color.white;
    [SerializeField] private Color waitingColor = Color.black;

    [Header("Fire")]
    [SerializeField, Min(0f)] private float fanPointDecreasePerSecond = 0.8f;
    private bool isTouchingFire;
    [SerializeField] private float fireDecreasePerSecond = 1f;

    [SerializeField] private GameObject urineAreaPrefab;　//小便エリア
    private GameObject currentArea;
    public bool IsTouchingPlaneHitFollower { get; private set; }
    public float FanPoint { get; private set; }
    public bool IsFan { get; private set; }

    private Vector3 initialPosition;
    private Vector3 interpolationStartPosition;
    private Vector3 targetPosition;
    private float interpolationTime;
    private float noiseSeed;
    private Color interpolationStartColor;
    private Color targetInterpolationColor;
    private Material targetMaterial;
    private float spawnColorTime;
    private bool isSpawnColorTransitioning;
    private bool hasBeenHit;
    private bool hasMovedToFanOffset;
    private bool areaSpawned = false;
    private void Start()
    {
        initialPosition = transform.position;
        targetPosition = initialPosition;

        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<Renderer>();
        }

        if (targetRenderer != null)
        {
            targetMaterial = targetRenderer.material;
            // 通常時の体色と、ファン化した時に使う色を最初にシェーダーへ渡しておく。
            SetBodyBaseColor(spawnColorDuration > 0f ? spawnColor : waitingColor);
            targetMaterial.SetColor(FanColorId, targetColor);
            targetMaterial.SetFloat(FanFillId, 0f);
            // 体の下から上へゲージを伸ばすため、モデルの高さ情報も渡す。
            targetMaterial.SetFloat(BoundsMinYId, targetRenderer.localBounds.min.y);
            targetMaterial.SetFloat(BoundsMaxYId, targetRenderer.localBounds.max.y);
            isSpawnColorTransitioning = spawnColorDuration > 0f;
        }

        interpolationStartColor = spawnColor;
        targetInterpolationColor = waitingColor;
        interpolationTime = moveDuration;
        noiseSeed = Random.value * 1000f;
    }

    private void Update()
    {
        UpdateSpawnColor();

        if (IsTouchingPlaneHitFollower)
        {
            IncreaseFanPoint();
        }

        if (IsTouchingPlaneHitFollower && !Input.GetMouseButton(0))
        {
            BeginReturnWait();
        }

        if (IsFan)
        {
            if (hasBeenHit && interpolationTime < moveDuration)
            {
                MoveToTarget();
            }

            ApplyDanceNoise();
            return;
        }

        if (hasBeenHit && interpolationTime < moveDuration)
        {
            MoveToTarget();
        }

        if (isTouchingFire)
        {
            DecreaseFanPoint();
        }
    }

    private void IncreaseFanPoint()
    {
        if (IsFan)
        {
            return;
        }

        FanPoint = Mathf.Min(
            FanPoint + fanPointPerSecond * Time.deltaTime,
            fanPointToBecomeFan
        );

        if (targetMaterial != null)
        {
            // ファン度の割合を 0～1 にして、シェーダーの塗りつぶし量として使う。
            targetMaterial.SetFloat(FanFillId, FanPoint / fanPointToBecomeFan);
        }

        if (FanPoint < fanPointToBecomeFan)
        {
            return;
        }

        IsFan = true;
        FanManager.Instance.AddFan();
        MoveToFanOffset();
        PlayFanUpEffect();
    }

    private void PlayFanUpEffect()
    {
        if (fanUpPrefab == null)
        {
            Debug.LogWarning(
                $"FanUp prefab is not assigned on '{name}'. Fan-up effect was skipped.",
                this
            );
            return;
        }

        GameObject effectObject = Instantiate(
            fanUpPrefab,
            transform.position + fanUpHeadOffset,
            fanUpPrefab.transform.rotation
        );

        FanUpEffect effect = effectObject.GetComponent<FanUpEffect>();
        if (effect == null)
        {
            effect = effectObject.AddComponent<FanUpEffect>();
        }

        effect.Play(fanUpRiseDistance, fanUpDuration);
    }

    public void DecreaseFanPoint()
    {
        if (IsFan)
        {
            return;
        }

        FanPoint = Mathf.Max(
            FanPoint - fanPointDecreasePerSecond * Time.deltaTime,
            0f
        );

        Debug.Log("FanPoint : " + FanPoint);

        if (targetMaterial != null)
        {
            targetMaterial.SetFloat(
                FanFillId,
                FanPoint / fanPointToBecomeFan
            );
        }

        float fill = FanPoint / fanPointToBecomeFan;
        Debug.Log("Fill : " + fill);

        targetMaterial.SetFloat(FanFillId, fill);
    }

    private void UpdateSpawnColor()
    {
        if (!isSpawnColorTransitioning || targetMaterial == null)
        {
            return;
        }

        spawnColorTime += Time.deltaTime;
        float rate = Mathf.Clamp01(spawnColorTime / spawnColorDuration);
        SetBodyBaseColor(Color.Lerp(spawnColor, waitingColor, rate));

        if (rate >= 1f)
        {
            isSpawnColorTransitioning = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleEnter(other);
    }

    private void OnTriggerExit(Collider other)
    {
        HandleExit(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleEnter(collision.collider);
    }

    private void OnCollisionExit(Collision collision)
    {
        HandleExit(collision.collider);
    }

    private void HandleEnter(Collider other)
    {
        if (other.CompareTag("Fire"))
        {
            isTouchingFire = true;
            return;
        }

        if (!other.CompareTag("Shoben"))
        {
            return;
        }

        if (!areaSpawned)
        {
            Vector3 pos = transform.position;
            pos.y = 0.02f;
            //Vector3 hitPos = other.ClosestPoint(transform.position);


            currentArea = Instantiate(
                urineAreaPrefab,
                pos,
                Quaternion.identity
            );
        }

        IsTouchingPlaneHitFollower = true;
    }

    private void HandleExit(Collider other)
    {
        if (other.CompareTag("Fire"))
        {
            isTouchingFire = false;
            return;
        }

        if (!other.CompareTag("Shoben"))
            return;

        if (currentArea != null)
        {
            Destroy(currentArea);
            currentArea = null;
        }

        areaSpawned = false;
        Destroy(currentArea, 1.5f);

        BeginReturnWait();
    }

    private void BeginReturnWait()
    {
        if (!IsTouchingPlaneHitFollower)
        {
            return;
        }

        // 一度たまったファン度はそのまま残し、接触判定だけ外す。
        IsTouchingPlaneHitFollower = false;
    }

    private void MoveToFanOffset()
    {
        if (hasMovedToFanOffset)
        {
            return;
        }

        // 触れてすぐではなく、ファンになった瞬間に移動と通常色への切り替えを始める。
        hasMovedToFanOffset = true;
        hasBeenHit = true;
        isSpawnColorTransitioning = false;
        BeginInterpolation(initialPosition + hitOffset, waitingColor);
    }

    private void ApplyDanceNoise()
    {
        float noise = Mathf.PerlinNoise(
            noiseSeed,
            Time.time * danceFrequency
        );

        float verticalOffset = (noise * 2f - 1f) * danceAmplitude;

        // 毎フレーム現在位置へ加算すると少しずつ位置がずれてしまうため、
        // 移動後の基準位置を中心にして上下させる。
        Vector3 danceCenter = interpolationTime < moveDuration
            ? transform.position
            : targetPosition;

        transform.position = danceCenter + Vector3.up * verticalOffset;
    }

    private void BeginInterpolation(Vector3 newTargetPosition, Color newTargetColor)
    {
        interpolationStartPosition = transform.position;
        targetPosition = newTargetPosition;
        // 補間開始時点の体色を保持しておくと、移動中も色が急に飛ばない。
        interpolationStartColor = GetBodyBaseColor();
        targetInterpolationColor = newTargetColor;
        interpolationTime = 0f;
    }

    private void MoveToTarget()
    {
        if (moveDuration <= 0f)
        {
            transform.position = targetPosition;

            if (targetMaterial != null)
            {
                SetBodyBaseColor(targetInterpolationColor);
            }

            return;
        }

        interpolationTime += Time.deltaTime;
        float rate = interpolationTime / moveDuration;

        transform.position = InterpolationUtility.Interpolate(
            interpolationStartPosition,
            targetPosition,
            rate,
            interpolationType
        );

        if (targetMaterial != null)
        {
            SetBodyBaseColor(InterpolationUtility.Interpolate(
                interpolationStartColor,
                targetInterpolationColor,
                rate,
                interpolationType
            ));
        }
    }

    private Color GetBodyBaseColor()
    {
        if (targetMaterial == null)
        {
            return waitingColor;
        }

        if (targetMaterial.HasProperty(BaseColorId))
        {
            return targetMaterial.GetColor(BaseColorId);
        }

        return targetMaterial.color;
    }

    private void SetBodyBaseColor(Color color)
    {
        if (targetMaterial == null)
        {
            return;
        }

        if (targetMaterial.HasProperty(BaseColorId))
        {
            targetMaterial.SetColor(BaseColorId, color);
        }

        // 通常の Material.color を参照する処理が残っていても見た目が揃うようにしておく。
        targetMaterial.color = color;
    }

    private void OnValidate()
    {
        fanPointPerSecond = Mathf.Max(0f, fanPointPerSecond);
        fanPointToBecomeFan = Mathf.Max(0.01f, fanPointToBecomeFan);
        fanUpRiseDistance = Mathf.Max(0f, fanUpRiseDistance);
        fanUpDuration = Mathf.Max(0f, fanUpDuration);
    }
}
