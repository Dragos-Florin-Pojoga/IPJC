using UnityEngine;
using UnityEngine.UI;

public class FloatingHealthBar : MonoBehaviour
{
    [Header("Health Bar")]
    [SerializeField] private Slider healthSlider;
    
    [Header("Damage Trail")]
    [SerializeField] private Slider trailSlider;
    [Tooltip("How long to wait before the trail starts catching up (seconds)")]
    [SerializeField] private float trailDelay = 0.5f;
    [Tooltip("How fast the trail catches up to current health")]
    [SerializeField] private float trailLerpSpeed = 2f;
    
    private float trailDelayTimer = 0f;
    private float targetHealth = 1f;

    void Awake()
    {
        // Initialize both sliders to full
        if (healthSlider != null) healthSlider.value = 1f;
        if (trailSlider != null) trailSlider.value = 1f;
        targetHealth = 1f;
    }

    public void UpdateHealthBar(object sender, ResourceChangedEventArgs e)
    {
        float newRatio = e.Ratio;
        
        // Update the main health bar immediately
        if (healthSlider != null)
        {
            healthSlider.value = newRatio;
        }
        
        // If we took damage, reset the trail delay timer
        if (newRatio < targetHealth)
        {
            trailDelayTimer = trailDelay;
        }
        
        targetHealth = newRatio;
    }

    void Update()
    {
        // Face the camera
        if (Camera.main != null)
        {
            transform.rotation = Camera.main.transform.rotation;
        }
        
        // Handle trail slider catch-up
        if (trailSlider != null && trailSlider.value > targetHealth)
        {
            if (trailDelayTimer > 0)
            {
                trailDelayTimer -= Time.deltaTime;
            }
            else
            {
                // Lerp the trail towards the current health
                trailSlider.value = Mathf.Lerp(trailSlider.value, targetHealth, trailLerpSpeed * Time.deltaTime);
                
                // Snap if close enough
                if (Mathf.Abs(trailSlider.value - targetHealth) < 0.001f)
                {
                    trailSlider.value = targetHealth;
                }
            }
        }
        else if (trailSlider != null && trailSlider.value < targetHealth)
        {
            // If healed, snap the trail up immediately
            trailSlider.value = targetHealth;
        }
    }
}
