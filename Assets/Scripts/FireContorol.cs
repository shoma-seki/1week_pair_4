using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[RequireComponent(typeof(Collider))]
public class FireCOntorol : MonoBehaviour
{
    [Header("Lifetime")]
    [SerializeField, Min(0f)] private float lifeTime = 5f;

    [Header("Player Passing")]
    [SerializeField] private Player player;

    [Header("Extinguishing")]
    [FormerlySerializedAs("extinguishTime")]
    [SerializeField, Min(0.01f)] private float extinguishContactTime = 2f;
    [SerializeField, Min(0.01f)] private float shrinkDuration = 0.5f;
    [SerializeField] private ParticleSystem[] fireParticles;

    [Header("Fan Decrease")]
    [SerializeField, Min(0.01f)] private float fanDecreaseInterval = 3f;
    [SerializeField, Min(0)] private int fanDecreaseAmount = 1;

    private readonly HashSet<Collider> planeHitContacts = new HashSet<Collider>();
    private float contactTime;
    private float fanDecreaseTimer;
    private bool isExtinguishing;

    private void Awake()
    {
        if (fireParticles == null || fireParticles.Length == 0)
        {
            fireParticles = GetComponentsInChildren<ParticleSystem>(true);
        }

        StopFireParticles();
    }

    private void Start()
    {
        FindPlayerIfNeeded();

        if (lifeTime > 0f)
        {
            Destroy(gameObject, lifeTime);
        }
    }

    private void Update()
    {
        if (isExtinguishing)
        {
            return;
        }

        if (TryExtinguishBehindPlayer())
        {
            return;
        }

        UpdateFanDecrease();
        UpdateExtinguishingContact();
    }

    private bool TryExtinguishBehindPlayer()
    {
        FindPlayerIfNeeded();
        if (player == null || transform.position.z >= player.transform.position.z)
        {
            return false;
        }

        StartCoroutine(ShrinkAndExtinguish());
        return true;
    }

    private void FindPlayerIfNeeded()
    {
        if (player == null)
        {
            player = FindAnyObjectByType<Player>();
        }
    }

    private void UpdateFanDecrease()
    {
        if (fanDecreaseAmount <= 0)
        {
            return;
        }

        fanDecreaseTimer += Time.deltaTime;
        if (fanDecreaseTimer < fanDecreaseInterval)
        {
            return;
        }

        // Preserve excess time so long frames do not permanently shift the interval.
        fanDecreaseTimer %= fanDecreaseInterval;
        FanManager.Instance.RemoveFan(fanDecreaseAmount);
    }

    private void UpdateExtinguishingContact()
    {
        int removedContactCount = planeHitContacts.RemoveWhere(contact => contact == null);
        if (planeHitContacts.Count == 0)
        {
            contactTime = 0f;
            if (removedContactCount > 0)
            {
                StopFireParticles();
            }
            return;
        }

        contactTime += Time.deltaTime;
        if (contactTime >= extinguishContactTime)
        {
            StartCoroutine(ShrinkAndExtinguish());
        }
    }

    private void RegisterContact(Collider other)
    {
        if (isExtinguishing || !IsPlaneHit(other))
        {
            return;
        }

        planeHitContacts.RemoveWhere(contact => contact == null);
        bool wasNotTouching = planeHitContacts.Count == 0;
        planeHitContacts.Add(other);

        if (wasNotTouching && planeHitContacts.Count > 0)
        {
            PlayFireParticles();
        }
    }

    private void UnregisterContact(Collider other)
    {
        if (!IsPlaneHit(other))
        {
            return;
        }

        planeHitContacts.Remove(other);
        if (planeHitContacts.Count == 0)
        {
            contactTime = 0f;
            if (!isExtinguishing)
            {
                StopFireParticles();
            }
        }
    }

    private static bool IsPlaneHit(Collider other)
    {
        return other != null &&
            (other.GetComponentInParent<PlaneHitFollower>() != null || other.CompareTag("Shoben"));
    }

    private void PlayFireParticles()
    {
        if (fireParticles == null)
        {
            return;
        }

        foreach (ParticleSystem particle in fireParticles)
        {
            if (particle != null && !particle.isPlaying)
            {
                particle.Play(true);
            }
        }
    }

    private void StopFireParticles()
    {
        if (fireParticles == null)
        {
            return;
        }

        foreach (ParticleSystem particle in fireParticles)
        {
            if (particle != null)
            {
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }

    private IEnumerator ShrinkAndExtinguish()
    {
        isExtinguishing = true;
        planeHitContacts.Clear();

        Vector3 startScale = transform.localScale;
        float elapsed = 0f;

        while (elapsed < shrinkDuration)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, elapsed / shrinkDuration);
            yield return null;
        }

        transform.localScale = Vector3.zero;
        StopFireParticles();

        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        RegisterContact(other);
    }

    private void OnTriggerStay(Collider other)
    {
        // Also register on Stay in case this component was enabled during an overlap.
        RegisterContact(other);
    }

    private void OnTriggerExit(Collider other)
    {
        UnregisterContact(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        RegisterContact(collision.collider);
    }

    private void OnCollisionStay(Collision collision)
    {
        RegisterContact(collision.collider);
    }

    private void OnCollisionExit(Collision collision)
    {
        UnregisterContact(collision.collider);
    }

    private void OnDisable()
    {
        StopFireParticles();
        planeHitContacts.Clear();
        contactTime = 0f;
    }

    private void OnValidate()
    {
        lifeTime = Mathf.Max(0f, lifeTime);
        extinguishContactTime = Mathf.Max(0.01f, extinguishContactTime);
        shrinkDuration = Mathf.Max(0.01f, shrinkDuration);
        fanDecreaseInterval = Mathf.Max(0.01f, fanDecreaseInterval);
        fanDecreaseAmount = Mathf.Max(0, fanDecreaseAmount);
    }
}
