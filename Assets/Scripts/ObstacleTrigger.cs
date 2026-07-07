using UnityEngine;

public class ObstacleTrigger : MonoBehaviour
{
    [SerializeField] private Obstacle obstacle;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            obstacle.StartMove();
        }
    }
}
