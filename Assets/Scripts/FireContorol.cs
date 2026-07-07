using UnityEngine;

[RequireComponent(typeof(Collider))]
public class FireCOntorol : MonoBehaviour
{
    [SerializeField, Min(0f)] private float lifeTime = 5f;
    [SerializeField, Min(0.01f)] private float extinguishTime = 2f;

    private float urineContactTime;

    private void Start()
    {
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

        if (urineContactTime >= extinguishTime)
        {
            Destroy(gameObject);
        }

        //Audience audience = other.GetComponent<Audience>();

        //if (audience != null)
        //{
        //    Debug.Log("AudienceéÊìæÅI");
        //    audience.DecreaseFanPoint();
        //}

        //Debug.Log(other.name);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Shoben"))
        {
            urineContactTime = 0f;
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
