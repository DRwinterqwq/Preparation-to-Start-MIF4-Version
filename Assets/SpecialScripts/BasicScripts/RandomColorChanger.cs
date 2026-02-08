using UnityEngine;
using System.Collections;

public class RandomColorChanger : MonoBehaviour
{
    [Header("时间设置")]
    [Tooltip("颜色变换一次所需的时间（秒）")]
    public float transitionDuration = 1.0f; 
    
    [Tooltip("变换完成后的停留时间")]
    public float waitTime = 0.2f;

    [Header("控制")]
    public bool autoStart = true;

    private Renderer objRenderer;
    private Coroutine colorRoutine;

    void Start()
    {
        objRenderer = GetComponent<Renderer>();
        if (autoStart) StartChanging();
    }

    public void StartChanging()
    {
        if (colorRoutine != null) StopCoroutine(colorRoutine);
        colorRoutine = StartCoroutine(SmoothChangeRoutine());
    }

    IEnumerator SmoothChangeRoutine()
    {
        while (true)
        {
            Color startColor = objRenderer.material.color;
            Color endColor = new Color(Random.value, Random.value, Random.value);
            float elapsed = 0f;

            // 核心部分：在指定持续时间内平滑插值
            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                // 计算进度百分比 (0 到 1)
                float t = elapsed / transitionDuration; 
                // 应用颜色插值
                objRenderer.material.color = Color.Lerp(startColor, endColor, t);
                yield return null; // 等待下一帧
            }

            // 确保达到最终颜色并停留
            objRenderer.material.color = endColor;
            yield return new WaitForSeconds(waitTime);
        }
    }
}