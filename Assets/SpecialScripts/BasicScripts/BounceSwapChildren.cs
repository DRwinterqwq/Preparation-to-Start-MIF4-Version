using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BounceSwapChildren : MonoBehaviour
{
    public List<Transform> childrenToSwap = new List<Transform>();
    public float bounceHeight = 2f;
    public float bounceDuration = 0.8f;
    public bool startOnAwake = false;
    public float swapInterval = 1f;
    public bool matchByRotation = false; // 新增：按旋转角度匹配交换
    
    private List<Vector3> originalPositions = new List<Vector3>();
    private List<Quaternion> originalRotations = new List<Quaternion>();
    private bool isSwapping = false;
    private Coroutine autoSwapCoroutine;
    
    void Awake()
    {
        SaveOriginalStates();
        
        if (startOnAwake && childrenToSwap.Count >= 2)
        {
            StartAutoSwapping();
        }
    }
    
    public void SaveOriginalStates()
    {
        originalPositions.Clear();
        originalRotations.Clear();
        
        foreach (Transform child in childrenToSwap)
        {
            if (child != null)
            {
                originalPositions.Add(child.position);
                originalRotations.Add(child.rotation);
            }
        }
    }
    
    public void StartAutoSwapping()
    {
        if (autoSwapCoroutine != null)
        {
            StopCoroutine(autoSwapCoroutine);
        }
        autoSwapCoroutine = StartCoroutine(AutoSwapRoutine());
    }
    
    public void StopAutoSwapping()
    {
        if (autoSwapCoroutine != null)
        {
            StopCoroutine(autoSwapCoroutine);
            autoSwapCoroutine = null;
        }
    }
    
    private IEnumerator AutoSwapRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(swapInterval);
            if (childrenToSwap.Count >= 2)
            {
                PerformBounceSwap();
            }
        }
    }
    
    public void PerformBounceSwap()
    {
        if (childrenToSwap.Count < 2 || isSwapping) return;
        
        StartCoroutine(BounceSwapCoroutine());
    }
    
    private IEnumerator BounceSwapCoroutine()
    {
        isSwapping = true;
        
        // 获取目标位置
        List<Vector3> targetPositions = GetTargetPositions();
        
        // 执行弹跳动画
        List<Vector3> startPositions = new List<Vector3>();
        List<Vector3> peakPoints = new List<Vector3>();
        
        // 计算起始位置和最高点
        for (int i = 0; i < childrenToSwap.Count; i++)
        {
            if (childrenToSwap[i] == null) continue;
            
            startPositions.Add(childrenToSwap[i].position);
            Vector3 midPoint = (startPositions[i] + targetPositions[i]) / 2;
            peakPoints.Add(midPoint + Vector3.up * bounceHeight);
        }
        
        float elapsedTime = 0f;
        
        // 弹起到最高点
        while (elapsedTime < bounceDuration / 2)
        {
            float t = elapsedTime / (bounceDuration / 2);
            
            for (int i = 0; i < childrenToSwap.Count; i++)
            {
                if (childrenToSwap[i] != null)
                {
                    childrenToSwap[i].position = Vector3.Lerp(startPositions[i], peakPoints[i], t);
                }
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // 落到目标位置
        elapsedTime = 0f;
        while (elapsedTime < bounceDuration / 2)
        {
            float t = elapsedTime / (bounceDuration / 2);
            
            for (int i = 0; i < childrenToSwap.Count; i++)
            {
                if (childrenToSwap[i] != null)
                {
                    childrenToSwap[i].position = Vector3.Lerp(peakPoints[i], targetPositions[i], t);
                }
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // 确保位置精确
        for (int i = 0; i < childrenToSwap.Count; i++)
        {
            if (childrenToSwap[i] != null)
            {
                childrenToSwap[i].position = targetPositions[i];
            }
        }
        
        isSwapping = false;
    }
    
    private List<Vector3> GetTargetPositions()
    {
        List<Vector3> targetPositions = new List<Vector3>();
        
        if (matchByRotation)
        {
            // 按旋转角度分组
            Dictionary<Quaternion, List<int>> rotationGroups = new Dictionary<Quaternion, List<int>>();
            
            for (int i = 0; i < childrenToSwap.Count; i++)
            {
                if (childrenToSwap[i] == null) continue;
                
                Quaternion rotation = childrenToSwap[i].rotation;
                Quaternion normalizedRotation = NormalizeRotation(rotation);
                
                if (!rotationGroups.ContainsKey(normalizedRotation))
                {
                    rotationGroups[normalizedRotation] = new List<int>();
                }
                rotationGroups[normalizedRotation].Add(i);
            }
            
            // 初始目标位置是当前位置（为没有匹配的物体准备的）
            for (int i = 0; i < childrenToSwap.Count; i++)
            {
                if (childrenToSwap[i] != null)
                {
                    targetPositions.Add(childrenToSwap[i].position);
                }
                else
                {
                    targetPositions.Add(Vector3.zero);
                }
            }
            
            // 在每个旋转组内随机交换
            foreach (var group in rotationGroups)
            {
                if (group.Value.Count >= 2)
                {
                    List<int> indices = new List<int>(group.Value);
                    List<int> shuffledIndices = new List<int>(indices);
                    
                    // 洗牌
                    for (int i = shuffledIndices.Count - 1; i > 0; i--)
                    {
                        int randomIndex = Random.Range(0, i + 1);
                        int temp = shuffledIndices[i];
                        shuffledIndices[i] = shuffledIndices[randomIndex];
                        shuffledIndices[randomIndex] = temp;
                    }
                    
                    // 分配目标位置
                    for (int i = 0; i < indices.Count; i++)
                    {
                        int originalIndex = indices[i];
                        int targetIndex = shuffledIndices[i];
                        
                        if (originalIndex < childrenToSwap.Count && 
                            targetIndex < childrenToSwap.Count &&
                            childrenToSwap[originalIndex] != null &&
                            childrenToSwap[targetIndex] != null)
                        {
                            targetPositions[originalIndex] = childrenToSwap[targetIndex].position;
                        }
                    }
                }
                // 如果组内只有一个物体，它保持原地
            }
        }
        else
        {
            // 完全随机交换
            List<Transform> shuffledList = new List<Transform>(childrenToSwap);
            
            for (int i = shuffledList.Count - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);
                Transform temp = shuffledList[i];
                shuffledList[i] = shuffledList[randomIndex];
                shuffledList[randomIndex] = temp;
            }
            
            for (int i = 0; i < childrenToSwap.Count; i++)
            {
                if (shuffledList[i] != null)
                {
                    targetPositions.Add(shuffledList[i].position);
                }
                else
                {
                    targetPositions.Add(Vector3.zero);
                }
            }
        }
        
        return targetPositions;
    }
    
    private Quaternion NormalizeRotation(Quaternion rotation)
    {
        // 将四元数归一化，使相近的旋转被视为相同
        // 可以将旋转四舍五入到最近的90度或使用阈值
        Vector3 euler = rotation.eulerAngles;
        
        // 四舍五入到最近的90度
        float x = Mathf.Round(euler.x / 90f) * 90f;
        float y = Mathf.Round(euler.y / 90f) * 90f;
        float z = Mathf.Round(euler.z / 90f) * 90f;
        
        return Quaternion.Euler(x, y, z);
    }
    
    public void ResetToOriginalStates()
    {
        StopAllCoroutines();
        isSwapping = false;
        autoSwapCoroutine = null;
        
        for (int i = 0; i < childrenToSwap.Count; i++)
        {
            if (childrenToSwap[i] != null && i < originalPositions.Count && i < originalRotations.Count)
            {
                childrenToSwap[i].position = originalPositions[i];
                childrenToSwap[i].rotation = originalRotations[i];
            }
        }
    }
    
    void OnDestroy()
    {
        StopAutoSwapping();
    }
}