using System.Collections;
using UnityEngine;

public class MuzzleLight : MonoBehaviour
{
    private Light light_;
    private Coroutine hideCoroutine;

    private void EnsureLight()
    {
        if (light_ != null) return;

        GameObject lightObject = new GameObject("MuzzleLight");
        lightObject.transform.SetParent(transform, false);
        light_ = lightObject.AddComponent<Light>();
        light_.type = LightType.Point;
        light_.color = new Color(1f, 0.55f, 0.2f);
        light_.intensity = 8f;
        light_.range = 3.5f;
        light_.shadows = LightShadows.None;
        light_.enabled = false;
    }

    public void Flash()
    {
        EnsureLight();
        ShowAt(transform.position);
    }

    public void FlashAt(Vector3 position)
    {
        EnsureLight();
        ShowAt(position);
    }

    private void ShowAt(Vector3 position)
    {
        light_.transform.position = position;
        light_.enabled = true;

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }
        hideCoroutine = StartCoroutine(HideAfter(0.03f));
    }

    private IEnumerator HideAfter(float delay)
    {
        yield return new WaitForSeconds(delay);
        light_.enabled = false;
        hideCoroutine = null;
    }
}
