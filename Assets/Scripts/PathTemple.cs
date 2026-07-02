using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;






public class PathToTemple : MonoBehaviour
{
    [Header("Path Target")]
    [SerializeField] private Transform templeTransform;

    [Header("Visual Settings")]
    [SerializeField] private GameObject pathVisualPrefab;
    [SerializeField] private float pathHeight = 0.1f;
    [SerializeField] private float pathWidth = 0.5f;
    [SerializeField] private Color pathColor = new Color(1, 0.5f, 0, 0.5f); 
    [SerializeField] private Material pathMaterial;

    [Header("Update Settings")]
    [SerializeField] private float updateFrequency = 0.5f;
    [SerializeField] private float fadeInDuration = 1.5f;
    [SerializeField] private float fadeOutDuration = 1.0f;
    [SerializeField] private float hideDistance = 5f; 

    private NavMeshPath _navPath;
    private Transform _playerTransform;
    private GameObject _pathParent;
    private LineRenderer _pathLine;
    private bool _isPathVisible;
    private bool _wasPathCreated;
    private int _lastEssenceCount;
    private WaitForSeconds _updateWait;
    private Coroutine _fadeCoroutine;

    
    public event Action<bool> OnPathVisibilityChanged;

    private void Awake()
    {
        _navPath = new NavMeshPath();
        _updateWait = new WaitForSeconds(updateFrequency);

        
        InitializePath();

        
        SetPathVisibility(false, false);
    }

    private void InitializePath()
    {
        
        _pathParent = new GameObject("PathToTemple_Visual");
        _pathParent.transform.SetParent(transform);

        GameObject pathObject;

        
        if (pathVisualPrefab != null)
        {
            pathObject = Instantiate(pathVisualPrefab, _pathParent.transform);
            _pathLine = pathObject.GetComponent<LineRenderer>();

            if (_pathLine == null)
            {
                _pathLine = pathObject.AddComponent<LineRenderer>();
            }
        }
        else
        {
            
            pathObject = new GameObject("PathLine");
            pathObject.transform.SetParent(_pathParent.transform);
            _pathLine = pathObject.AddComponent<LineRenderer>();
        }

        SetupLineRenderer();
    }

    private void SetupLineRenderer()
    {
        _pathLine.startWidth = pathWidth;
        _pathLine.endWidth = pathWidth;

        
        _pathLine.material = pathMaterial != null ?
            pathMaterial : new Material(Shader.Find("Sprites/Default"));

        _pathLine.startColor = pathColor;
        _pathLine.endColor = pathColor;
        _pathLine.positionCount = 0;
        _pathLine.useWorldSpace = true;

        
        _pathLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        _pathLine.receiveShadows = false;
    }

    private void Start()
    {
        
        if (GlobalReferences.Instance == null)
        {
            Debug.LogError("GlobalReferences not found!");
            enabled = false;
            return;
        }

        _playerTransform = GlobalReferences.Instance.player.transform;

        
        if (templeTransform == null)
        {
            
            GameObject temple = GameObject.FindGameObjectWithTag("Temple");
            if (temple != null)
            {
                templeTransform = temple.transform;
            }
            else
            {
                Debug.LogWarning("Temple not found! Please tag your temple object with 'Temple' or assign it directly.");
                enabled = false;
                return;
            }
        }

        
        StartCoroutine(UpdatePathRoutine());
    }

    private IEnumerator UpdatePathRoutine()
    {
        while (true)
        {
            if (GlobalReferences.Instance != null)
            {
                int currentEssenceCount = GlobalReferences.Instance.essenceCount;
                int requiredEssences = GlobalReferences.Instance.essencesPerPotion;

                
                if (_isPathVisible && _playerTransform != null && templeTransform != null)
                {
                    float distanceToTemple = Vector3.Distance(_playerTransform.position, templeTransform.position);

                    if (distanceToTemple < hideDistance)
                    {
                        
                        ShowPath(false);
                    }
                    else if (currentEssenceCount < requiredEssences)
                    {
                        
                        ShowPath(false);
                    }
                    else
                    {
                        
                        UpdatePath();
                    }
                }
                else if (currentEssenceCount >= requiredEssences && !_isPathVisible)
                {
                    
                    ShowPath(true);
                }

                _lastEssenceCount = currentEssenceCount;
            }

            yield return _updateWait;
        }
    }

    private void UpdatePath()
    {
        
        NavMesh.CalculatePath(_playerTransform.position, templeTransform.position, NavMesh.AllAreas, _navPath);

        
        if (_navPath.corners.Length > 0)
        {
            Vector3[] pathPoints = new Vector3[_navPath.corners.Length];

            
            for (int i = 0; i < _navPath.corners.Length; i++)
            {
                pathPoints[i] = _navPath.corners[i] + new Vector3(0, pathHeight, 0);
            }

            _pathLine.positionCount = pathPoints.Length;
            _pathLine.SetPositions(pathPoints);
            _wasPathCreated = true;
        }
        else if (_wasPathCreated)
        {
            
            _pathLine.positionCount = 2;
            _pathLine.SetPosition(0, _playerTransform.position + new Vector3(0, pathHeight, 0));
            _pathLine.SetPosition(1, templeTransform.position + new Vector3(0, pathHeight, 0));
        }
    }

    
    
    
    public void ShowPath(bool show)
    {
        
        if (_fadeCoroutine != null)
        {
            StopCoroutine(_fadeCoroutine);
        }

        _fadeCoroutine = StartCoroutine(show ? FadeInPath() : FadeOutPath());
    }

    private IEnumerator FadeInPath()
    {
        float startTime = Time.time;
        Color transparent = new Color(pathColor.r, pathColor.g, pathColor.b, 0);
        Color opaque = pathColor;

        
        SetPathVisibility(true, false);
        _pathLine.startColor = transparent;
        _pathLine.endColor = transparent;

        
        while (Time.time < startTime + fadeInDuration)
        {
            float t = (Time.time - startTime) / fadeInDuration;
            Color currentColor = Color.Lerp(transparent, opaque, t);

            _pathLine.startColor = currentColor;
            _pathLine.endColor = currentColor;

            yield return null;
        }

        
        _pathLine.startColor = opaque;
        _pathLine.endColor = opaque;

        _fadeCoroutine = null;
    }

    private IEnumerator FadeOutPath()
    {
        float startTime = Time.time;
        Color opaque = pathColor;
        Color transparent = new Color(pathColor.r, pathColor.g, pathColor.b, 0);

        
        while (Time.time < startTime + fadeOutDuration)
        {
            float t = (Time.time - startTime) / fadeOutDuration;
            Color currentColor = Color.Lerp(opaque, transparent, t);

            _pathLine.startColor = currentColor;
            _pathLine.endColor = currentColor;

            yield return null;
        }

        
        SetPathVisibility(false, false);

        _fadeCoroutine = null;
    }

    private void SetPathVisibility(bool visible, bool animate)
    {
        if (_isPathVisible != visible)
        {
            _isPathVisible = visible;

            if (_pathParent != null)
            {
                _pathParent.SetActive(visible);
            }

            if (!visible)
            {
                _pathLine.positionCount = 0;
            }

            
            OnPathVisibilityChanged?.Invoke(visible);

            
            if (animate)
            {
                ShowPath(visible);
            }
        }
    }

    
    public void HidePath()
    {
        ShowPath(false);
    }

    private void OnDrawGizmosSelected()
    {
        if (templeTransform != null && _playerTransform != null)
        {
            Gizmos.color = pathColor;
            Gizmos.DrawLine(_playerTransform.position, templeTransform.position);

            
            Gizmos.color = new Color(pathColor.r, pathColor.g, pathColor.b, 0.3f);
            Gizmos.DrawWireSphere(templeTransform.position, hideDistance);
        }
    }

    
    
    
    
    public void ForceShowPath(float duration)
    {
        ShowPath(true);

        
        if (duration > 0)
        {
            StartCoroutine(AutoHideAfterDelay(duration));
        }
    }

    private IEnumerator AutoHideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowPath(false);
    }
}
