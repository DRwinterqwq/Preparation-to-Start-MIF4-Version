using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

[System.Serializable]
public class TextWithExactProbability
{
    [Tooltip("显示的文本")]
    public string text = "新文本";
    
    [Tooltip("概率百分比 (0-100)")]
    [Range(0, 100)]
    public float probabilityPercent = 10f;
}

public class RandomTextSelector : MonoBehaviour
{
    [Header("文本设置")]
    public Text targetText;
    
    [Header("带概率的文本列表")]
    [Tooltip("所有概率的总和应该接近100%")]
    public TextWithExactProbability[] textOptions;
    
    [Header("随机设置")]
    public bool setOnAwake = true;
    public bool setOnStart = false;

    void Awake()
    {
        if (setOnAwake)
        {
            SetPercentRandomText();
        }
    }

    void Start()
    {
        if (setOnStart)
        {
            SetPercentRandomText();
        }
    }

    /// <summary>
    /// 根据百分比概率随机选择文本
    /// </summary>
    public void SetPercentRandomText()
    {
        if (targetText == null)
        {
            Debug.LogWarning("Text组件未分配！");
            return;
        }

        if (textOptions == null || textOptions.Length == 0)
        {
            Debug.LogWarning("没有可选的文本！");
            return;
        }

        // 计算总概率并归一化
        float totalPercent = 0f;
        foreach (var option in textOptions)
        {
            totalPercent += option.probabilityPercent;
        }

        if (totalPercent <= 0)
        {
            Debug.LogWarning("所有概率的总和必须大于0！");
            return;
        }

        // 生成0-100之间的随机数
        float randomValue = UnityEngine.Random.Range(0f, 100f);
        float accumulatedPercent = 0f;

        // 按概率选择文本
        for (int i = 0; i < textOptions.Length; i++)
        {
            // 计算归一化的实际概率
            float actualProbability = textOptions[i].probabilityPercent / totalPercent * 100f;
            accumulatedPercent += actualProbability;
            
            if (randomValue < accumulatedPercent)
            {
                targetText.text = textOptions[i].text;
                return;
            }
        }

        // 备选方案
        targetText.text = textOptions[textOptions.Length - 1].text;
    }

    /// <summary>
    /// 验证概率总和并调整
    /// </summary>
    public void ValidateAndAdjustProbabilities()
    {
        if (textOptions == null || textOptions.Length == 0)
            return;

        float totalPercent = 0f;
        foreach (var option in textOptions)
        {
            totalPercent += option.probabilityPercent;
        }
        
        if (Mathf.Abs(totalPercent - 100f) > 0.1f)
        {
            Debug.LogWarning($"概率总和为 {totalPercent:F1}%，不是100%！");
        }
    }

    /// <summary>
    /// 自动归一化概率，使总和为100%
    /// </summary>
    public void NormalizeProbabilities()
    {
        if (textOptions == null || textOptions.Length == 0)
            return;

        float totalPercent = 0f;
        foreach (var option in textOptions)
        {
            totalPercent += option.probabilityPercent;
        }

        if (totalPercent == 0)
        {
            // 如果总和为0，平均分配
            float average = 100f / textOptions.Length;
            for (int i = 0; i < textOptions.Length; i++)
            {
                textOptions[i].probabilityPercent = average;
            }
        }
        else
        {
            // 按比例调整
            for (int i = 0; i < textOptions.Length; i++)
            {
                textOptions[i].probabilityPercent = 
                    textOptions[i].probabilityPercent / totalPercent * 100f;
            }
        }

        Debug.Log("已自动归一化概率，总和为100%");
    }

    /// <summary>
    /// 获取实际概率百分比（归一化后）
    /// </summary>
    public float[] GetActualProbabilities()
    {
        if (textOptions == null || textOptions.Length == 0)
            return new float[0];

        float totalPercent = 0f;
        foreach (var option in textOptions)
        {
            totalPercent += option.probabilityPercent;
        }

        float[] actualProbs = new float[textOptions.Length];
        for (int i = 0; i < textOptions.Length; i++)
        {
            actualProbs[i] = textOptions[i].probabilityPercent / totalPercent * 100f;
        }

        return actualProbs;
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // 在编辑器中更新时验证概率
        ValidateAndAdjustProbabilities();
    }
#endif
}