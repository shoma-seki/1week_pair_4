using UnityEngine;

public class FireCOntorol : MonoBehaviour
{
    [SerializeField] private float lifeTime = 5.0f;
    [SerializeField] private int hp = 3;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    // Update is called once per frame
    void Update()
    {
        //‰¼
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Destroy(gameObject);
        }
    }
}
