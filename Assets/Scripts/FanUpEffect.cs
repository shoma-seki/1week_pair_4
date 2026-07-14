using UnityEngine;

public class FanUpEffect : MonoBehaviour
{
    private Vector3 startPosition;
    private Vector3 endPosition;
    private float duration;
    private float elapsedTime;
    private bool isPlaying;

    public void Play(float riseDistance, float playDuration)
    {
        startPosition = transform.position;
        endPosition = startPosition + Vector3.up * Mathf.Max(0f, riseDistance);
        duration = Mathf.Max(0f, playDuration);
        elapsedTime = 0f;
        isPlaying = true;

        if (duration <= 0f)
        {
            transform.position = endPosition;
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (!isPlaying || duration <= 0f)
        {
            return;
        }

        elapsedTime += Time.deltaTime;
        float rate = Mathf.Clamp01(elapsedTime / duration);
        transform.position = Vector3.Lerp(startPosition, endPosition, rate);

        if (rate >= 1f)
        {
            isPlaying = false;
            Destroy(gameObject);
        }
    }
}
