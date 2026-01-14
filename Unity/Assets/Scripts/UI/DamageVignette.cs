using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays a red vignette effect when the player takes damage.
/// Attach to a Canvas with a full-screen Image using a vignette sprite or gradient.
/// </summary>
public class DamageVignette : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("How long the vignette stays visible")]
    [SerializeField] private float fadeDuration = 0.5f;
    [Tooltip("Maximum alpha when damage is taken")]
    [SerializeField] private float maxAlpha = 0.6f;
    [Tooltip("Color of the vignette")]
    [SerializeField] private Color vignetteColor = new Color(0.8f, 0f, 0f, 1f);
    
    private Image vignetteImage;
    private float currentAlpha = 0f;
    private float fadeTimer = 0f;
    private bool isFading = false;
    
    private static DamageVignette instance;
    public static DamageVignette Instance => instance;
    
    void Awake()
    {
        instance = this;
        vignetteImage = GetComponent<Image>();
        
        if (vignetteImage == null)
        {
            Debug.LogError("DamageVignette requires an Image component!");
            return;
        }
        
        // Start fully transparent
        SetAlpha(0f);
    }
    
    void Update()
    {
        if (!isFading || vignetteImage == null) return;
        
        fadeTimer += Time.deltaTime;
        float t = fadeTimer / fadeDuration;
        
        currentAlpha = Mathf.Lerp(maxAlpha, 0f, t);
        SetAlpha(currentAlpha);
        
        if (t >= 1f)
        {
            isFading = false;
            SetAlpha(0f);
        }
    }
    
    /// <summary>
    /// Triggers the damage vignette flash effect.
    /// </summary>
    public void Flash()
    {
        if (vignetteImage == null) return;
        
        currentAlpha = maxAlpha;
        fadeTimer = 0f;
        isFading = true;
        SetAlpha(maxAlpha);
    }
    
    /// <summary>
    /// Triggers a flash with custom intensity (0-1).
    /// </summary>
    public void Flash(float intensity)
    {
        if (vignetteImage == null) return;
        
        currentAlpha = maxAlpha * Mathf.Clamp01(intensity);
        fadeTimer = 0f;
        isFading = true;
        SetAlpha(currentAlpha);
    }
    
    private void SetAlpha(float alpha)
    {
        Color c = vignetteColor;
        c.a = alpha;
        vignetteImage.color = c;
    }
}
