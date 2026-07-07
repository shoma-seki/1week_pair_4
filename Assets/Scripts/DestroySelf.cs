using UnityEngine;

public class DestroySelf : MonoBehaviour
{
    void SelfDestroy()
    {
        Destroy(gameObject);
    }
}
