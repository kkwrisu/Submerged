using UnityEngine;

public class LightFlicker : MonoBehaviour
{
    [Header("Intensidade")]
    public float minIntensity = 0f;
    public float maxIntensity = 2f;

    [Header("Velocidade")]
    public float minInterval = 0.05f;
    public float maxInterval = 0.2f;

    private Light spotlight;
    private float timer;
    private float nextInterval;

    private void Awake()
    {
        spotlight = GetComponent<Light>();
        nextInterval = Random.Range(minInterval, maxInterval);
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= nextInterval)
        {
            timer = 0f;
            nextInterval = Random.Range(minInterval, maxInterval);
            spotlight.intensity = Random.Range(minIntensity, maxIntensity);
        }
    }
}