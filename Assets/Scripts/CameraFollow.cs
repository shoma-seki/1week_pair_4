using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform player;

    private Vector3 offset;

    private void Start()
    {
        if (player == null)
        {
            Player targetPlayer = FindAnyObjectByType<Player>();

            if (targetPlayer != null)
            {
                player = targetPlayer.transform;
            }
        }

        if (player != null)
        {
            offset = transform.position - player.position;
        }
    }

    private void LateUpdate()
    {
        if (player == null)
        {
            return;
        }

        transform.position = player.position + offset;
    }
}
