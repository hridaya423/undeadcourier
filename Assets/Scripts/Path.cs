using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;





public class PathToScientist : MonoBehaviour
{
    [Header("Path Target")]
    [SerializeField] private Transform scientistTransform;
    
    [Header("Visual Settings")]
    [SerializeField] private GameObject pathVisualPrefab;
    [SerializeField] private float pathHeight = 0.1f;
    [SerializeField] private float pathWidth = 0.5f;
    [SerializeField] private Color pathColor = new Color(0, 1, 0, 0.5f);
    [SerializeField] private Material pathMaterial;
    
    [Header("Update Settings")]
    [SerializeField] private float updateFrequency = 0.5f;
    [SerializeField] private float fadeInDuration = 1.5f;
    [SerializeField] private float fadeOutDuration = 1.0f;
    
    private NavMeshPath _navPath;
    private Transform _playerTransform;
    private GameObject _pathParent;
    private LineRenderer _pathLine;
    private bool _isPathVisible;
    private bool _wasPathCreated;
    private int _lastPotionCount;
    private ScientistNPC _scientistNPC;
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
        
        _pathParent = new GameObject("PathToScientist_Visual");
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

        
        if (scientistTransform == null)
        {
            _scientistNPC = FindFirstObjectByType<ScientistNPC>();
            if (_scientistNPC != null)
            {
                scientistTransform = _scientistNPC.transform;
            }
            else
            {
                Debug.LogWarning("ScientistNPC not found! Please assign it manually.");
                enabled = false;
                return;
            }
        }
        else
        {
            
            _scientistNPC = scientistTransform.GetComponent<ScientistNPC>();
        }
        
        if (_scientistNPC != null)
        {
            _scientistNPC.OnPotionGiven += HandlePotionGiven;
        }
        else
        {
            Debug.LogWarning("ScientistNPC component not found on the scientist transform!");
        }

        
        StartCoroutine(UpdatePathRoutine());
    }

    private void OnDestroy()
    {
        
        if (_scientistNPC != null)
        {
            _scientistNPC.OnPotionGiven -= HandlePotionGiven;
        }
    }

    private void HandlePotionGiven()
    {
        
        if (_isPathVisible)
        {
            ShowPath(false);
        }
    }

    private IEnumerator UpdatePathRoutine()
    {
        while (true)
        {
            
            if (GlobalReferences.Instance != null)
            {
                int currentPotionCount = GlobalReferences.Instance.potionCount;

                
                if (currentPotionCount > 0 && _lastPotionCount == 0)
                {
                    ShowPath(true);
                }

                _lastPotionCount = currentPotionCount;
            }

            
            if (_isPathVisible && _playerTransform != null && scientistTransform != null)
            {
                UpdatePath();
            }

            yield return _updateWait;
        }
    }

    private void UpdatePath()
    {
        
        if (Vector3.Distance(_playerTransform.position, scientistTransform.position) < 3f)
        {
            _pathLine.positionCount = 0;
            return;
        }

        
        NavMesh.CalculatePath(_playerTransform.position, scientistTransform.position, NavMesh.AllAreas, _navPath);

        
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
            _pathLine.SetPosition(1, scientistTransform.position + new Vector3(0, pathHeight, 0));
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
        if (scientistTransform != null && _playerTransform != null)
        {
            Gizmos.color = pathColor;
            Gizmos.DrawLine(_playerTransform.position, scientistTransform.position);
        }
    }
}
