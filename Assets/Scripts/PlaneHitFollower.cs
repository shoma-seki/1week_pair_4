using UnityEngine;

[RequireComponent(typeof(CapsuleCollider))]
public class PlaneHitFollower : MonoBehaviour
{
    [Header("Raycast")]
    [SerializeField] private CameraRaycaster cameraRaycaster;
    [SerializeField] private Transform player;
    [SerializeField] private Vector3 positionOffset;
    [SerializeField, Min(0f)] private float forwardOffsetFromPlayer;

    [Header("Radius")]
    [SerializeField] private float secondStageTime = 0.5f;
    [SerializeField] private float thirdStageTime = 1.5f;
    [SerializeField] private float firstStageMultiplier = 1.25f;
    [SerializeField] private float secondStageMultiplier = 1.5f;
    [SerializeField] private float thirdStageMultiplier = 2f;

    private CapsuleCollider capsuleCollider;
    private float initialRadius;
    private float clickDuration;
    private Player playerResource;
    private ParticleSystem[] childParticleSystems;
    private bool particlesPlaying;

    private void Awake()
    {
        capsuleCollider = GetComponent<CapsuleCollider>();
        initialRadius = capsuleCollider.radius;
        capsuleCollider.enabled = false;

        if (cameraRaycaster == null)
        {
            cameraRaycaster = FindAnyObjectByType<CameraRaycaster>();
        }

        if (player == null)
        {
            Player targetPlayer = FindAnyObjectByType<Player>();

            if (targetPlayer != null)
            {
                player = targetPlayer.transform;
                playerResource = targetPlayer;
            }
        }

        if (playerResource == null && player != null)
        {
            playerResource = player.GetComponent<Player>();
        }

        if (playerResource == null)
        {
            playerResource = FindAnyObjectByType<Player>();
        }

        childParticleSystems = GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem particles in childParticleSystems)
        {
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void Update()
    {
        MoveToPlaneHitPoint();
        UpdateColliderRadius();
    }

    private void MoveToPlaneHitPoint()
    {
        if (cameraRaycaster == null)
        {
            return;
        }

        if (cameraRaycaster.TryGetPlaneHitPoint(out Vector3 hitPoint))
        {
            Vector3 targetPosition = hitPoint + positionOffset;
            transform.position = ConstrainTargetPosition(targetPosition);
        }
    }

    public Vector3 ConstrainTargetPosition(Vector3 targetPosition)
    {
        if (player != null)
        {
            float minimumZ = player.position.z + forwardOffsetFromPlayer;
            targetPosition.z = Mathf.Max(targetPosition.z, minimumZ);
        }

        return targetPosition;
    }

    private void UpdateColliderRadius()
    {
        if (Input.GetMouseButton(0) && playerResource != null && playerResource.CanUrinate)
        {
            capsuleCollider.enabled = true;
            SetChildParticlesPlaying(true);
            clickDuration += Time.deltaTime;

            if (clickDuration >= thirdStageTime)
            {
                capsuleCollider.radius = initialRadius * thirdStageMultiplier;
            }
            else if (clickDuration >= secondStageTime)
            {
                capsuleCollider.radius = initialRadius * secondStageMultiplier;
            }
            else
            {
                capsuleCollider.radius = initialRadius * firstStageMultiplier;
            }

            return;
        }

        clickDuration = 0f;
        capsuleCollider.radius = initialRadius;
        capsuleCollider.enabled = false;
        SetChildParticlesPlaying(false);
    }

    private void OnDisable()
    {
        SetChildParticlesPlaying(false);
    }

    private void SetChildParticlesPlaying(bool playing)
    {
        if (particlesPlaying == playing || childParticleSystems == null) return;
        particlesPlaying = playing;

        foreach (ParticleSystem particles in childParticleSystems)
        {
            if (particles == null) continue;

            if (playing)
            {
                particles.Play(true);
            }
            else
            {
                particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }
    }
}
