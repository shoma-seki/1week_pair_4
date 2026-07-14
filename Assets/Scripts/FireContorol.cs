using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FireCOntorol : MonoBehaviour
{
    [SerializeField, Min(0f)] private float lifeTime = 5f;
    [SerializeField, Min(0.01f)] private float extinguishTime = 2f;

    private float urineContactTime;
    private Vector3 originalScale;
    private bool extinguished;

    private void Start()
    {
        originalScale = transform.localScale;
        if (lifeTime > 0f)
        {
            Destroy(gameObject, lifeTime);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Shoben"))
        {
            return;
        }

        urineContactTime += Time.deltaTime;
        float extinguishRate = Mathf.Clamp01(urineContactTime / extinguishTime);
        transform.localScale = originalScale * Mathf.Lerp(1f, 0.3f, extinguishRate * extinguishRate);

        if (!extinguished && urineContactTime >= extinguishTime)
        {
            extinguished = true;
            PresentationDirector director = PresentationDirector.Instance;
            if (director != null)
            {
                director.SpawnBurst(transform.position, new Color(0.65f, 0.92f, 1f), 42, 0.55f);
                director.Flash(new Color(0.55f, 0.85f, 1f, 0.16f), 0.4f);
                director.PlayTone(260f, 0.32f, 0.16f);
            }
            Destroy(gameObject);
        }

        //Audience audience = other.GetComponent<Audience>();

        //if (audience != null)
        //{
        //    Debug.Log("Audience取得！");
        //    audience.DecreaseFanPoint();
        //}

        //Debug.Log(other.name);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Shoben"))
        {
            urineContactTime = 0f;
            transform.localScale = originalScale;
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        OnTriggerStay(collision.collider);
    }

    private void OnCollisionExit(Collision collision)
    {
        OnTriggerExit(collision.collider);
    }

    private void OnValidate()
    {
        lifeTime = Mathf.Max(0f, lifeTime);
        extinguishTime = Mathf.Max(0.01f, extinguishTime);
    }
}
