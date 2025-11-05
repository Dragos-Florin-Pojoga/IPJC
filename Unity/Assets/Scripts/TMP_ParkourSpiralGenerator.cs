using UnityEngine;

public class TMP_ParkourSpiralGenerator : MonoBehaviour
{
    [Header("Platform Parameters")]
    [SerializeField] private GameObject _platformPrefab;
    [SerializeField] private int _numberOfPlatforms = 20;
    [SerializeField] private float _spiralRadius = 5f;

    [Header("Platform Size Randomness")]
    [SerializeField] private float _platformSizeMin = 2f;
    [SerializeField] private float _platformSizeMax = 4f;

    [Header("Spiral Geometry")]
    [SerializeField] private float _angleIncrement = 30f;
    [SerializeField] private float _radiusIncrement = 0.5f;

    [Header("Player Jump Constraints")]
    [SerializeField] private float _singleJumpHeight = 1.8f;
    [SerializeField] private float _maxSprintSpeed = 8f;

    private float _safeVerticalStep;
    private float _safeHorizontalGap;

    private void Start()
    {
        _safeVerticalStep = _singleJumpHeight * 0.8f;
        _safeHorizontalGap = 4.0f;

        if (_platformPrefab == null)
        {
            Debug.LogError("Platform Prefab is not assigned in the Inspector!");
            return;
        }

        Vector3 currentPosition = transform.position;
        float currentAngle = 0f;
        float currentRadius = _spiralRadius;

        for (int i = 1; i < _numberOfPlatforms; i++)
        {
            currentAngle += _angleIncrement;
            currentRadius += _radiusIncrement;

            float verticalOffset = _safeVerticalStep * Random.Range(0.8f, 1.0f);
            float horizontalOffset = _safeHorizontalGap * Random.Range(0.5f, 1.0f);

            float angleRad = currentAngle * Mathf.Deg2Rad;

            float x = currentRadius * Mathf.Cos(angleRad);
            float z = currentRadius * Mathf.Sin(angleRad);

            currentPosition.y += verticalOffset;

            currentPosition.x = x;
            currentPosition.z = z;

            float platformSize = Random.Range(_platformSizeMin, _platformSizeMax);

            InstantiatePlatform(currentPosition, new Vector3(platformSize, 0.5f, platformSize));
        }
    }

    private void InstantiatePlatform(Vector3 position, Vector3 scale)
    {
        GameObject platform = Instantiate(_platformPrefab, position, Quaternion.identity, this.transform);

        platform.transform.localScale = scale;

        platform.name = "SpiralPlatform_" + (transform.childCount - 1);
    }
}
