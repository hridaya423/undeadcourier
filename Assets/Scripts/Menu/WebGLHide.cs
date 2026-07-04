using UnityEngine;

public class WebGLHide : MonoBehaviour
{
    void Awake()
    {
#if UNITY_WEBGL
        gameObject.SetActive(false);
#endif
    }
}
