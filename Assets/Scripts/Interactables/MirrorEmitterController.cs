using UnityEngine;

// Attached to spawned mirror emitters; handles auto-destruction after inactivity (TTL).
public class MirrorEmitterController : MonoBehaviour
{
    private float timeToLive = 2.0f;
    private float remainingTime;

    void Start()
    {
        remainingTime = timeToLive;
    }

    void Update()
    {
        remainingTime -= Time.deltaTime;
        if (remainingTime <= 0f)
        {
            Destroy(gameObject);
        }
    }

    public void SetTimeToLive(float ttl)
    {
        timeToLive = ttl;
        remainingTime = ttl;
    }

    public void RefreshTimeToLive()
    {
        remainingTime = timeToLive;
    }
}
