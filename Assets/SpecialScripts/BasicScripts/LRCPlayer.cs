using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using DancingLineFanmade.Level;

public class LRCPlayer : MonoBehaviour
{
    [Header("UI 绑定")]
    public Text lrcDisplay;         // 原文字幕
    public Text translationDisplay; // 译文字幕
    public TextAsset lrcFile;

    [Header("功能开关")]
    public bool isMoving = true;         
    public bool isChangingColor = true; 

    [Header("移动设置")]
    public float moveSpeed = 200f;
    
    [Header("颜色设置")]
    public float colorChangeInterval = 1.5f; 
    public float colorTransitionSpeed = 4f;

    [Header("双语解析设置")]
    [Tooltip("如果原文和译文在同一时间戳，用什么字符分隔内容？")]
    public string splitChar = "/"; 

    [Header("Player")]
    [Tooltip("如果不拖入，将自动通过 Player.Instance 获取")]
    public Player playerScript;

    private List<LrcLine> lrcLines = new List<LrcLine>();
    private int lastIndex = -1;

    // 运动相关
    private Vector2 dirOri, dirTrans;
    private Color targetColor;
    private float colorTimer;
    private RectTransform rectOri, rectTrans, canvasRect;

    private class LrcLine
    {
        public float time; // 秒为单位
        public string original;
        public string translation;
    }

    void Start()
    {
        // 1. 自动获取 Player 引用
        if (playerScript == null)
        {
            playerScript = Player.Instance; // 尝试单例获取
            if (playerScript == null)
            {
                playerScript = FindObjectOfType<Player>(); // 尝试场景搜索
            }
        }

        // 2. UI 基础初始化
        if (lrcDisplay) rectOri = lrcDisplay.GetComponent<RectTransform>();
        if (translationDisplay) rectTrans = translationDisplay.GetComponent<RectTransform>();
        
        // 自动获取 Canvas 边界
        canvasRect = GetComponentInParent<Canvas>()?.GetComponent<RectTransform>();

        dirOri = GetRandomDirection();
        dirTrans = GetRandomDirection();

        // 3. 解析歌词文件
        if (lrcFile != null) ParseLrc(lrcFile.text);
        
        if (lrcDisplay) targetColor = lrcDisplay.color;
    }

    void Update()
    {
        // --- 核心同步：使用 Player.gameTime ---
        if (playerScript != null)
        {
            // 在你的 Player 脚本中，gameTime 随 Time.deltaTime 累加
            // 且在 Update 中计算进度，我们直接同步此时间
            UpdateLrcByTime(playerScript.gameTime);
        }

        // 运动逻辑
        if (isMoving)
        {
            if (rectOri && lrcDisplay) BounceMotion(rectOri, lrcDisplay, ref dirOri);
            if (rectTrans && translationDisplay) BounceMotion(rectTrans, translationDisplay, ref dirTrans);
        }

        // 变色逻辑
        if (isChangingColor) HandleColorLogic();
    }

    // --- 核心解析逻辑 ---
    void ParseLrc(string text)
    {
        lrcLines.Clear();
        // 匹配格式: [00:00.00]内容
        string pattern = @"\[(?<time>\d{2}:\d{2}(?:\.\d{2,3})?)\](?<content>.*?)(?=\[|$)";
        MatchCollection matches = Regex.Matches(text, pattern, RegexOptions.Singleline);

        Dictionary<float, LrcLine> lineMap = new Dictionary<float, LrcLine>();

        foreach (Match match in matches)
        {
            float time = ParseTime(match.Groups["time"].Value);
            string content = match.Groups["content"].Value.Trim(); 

            if (string.IsNullOrEmpty(content)) continue;

            string orig = content;
            string trans = "";

            if (content.Contains(splitChar))
            {
                string[] parts = content.Split(new[] { splitChar }, 2, StringSplitOptions.None);
                orig = parts[0].Trim();
                trans = parts[1].Trim();
            }

            if (lineMap.ContainsKey(time))
            {
                lineMap[time].translation = content;
            }
            else
            {
                lineMap[time] = new LrcLine { time = time, original = orig, translation = trans };
            }
        }
        lrcLines = lineMap.Values.OrderBy(l => l.time).ToList();
    }

    void UpdateLrcByTime(float currentTime)
    {
        int targetIndex = -1;
        // 倒序查找当前时间对应的歌词行
        for (int i = lrcLines.Count - 1; i >= 0; i--)
        {
            if (currentTime >= lrcLines[i].time) { targetIndex = i; break; }
        }

        if (targetIndex != lastIndex)
        {
            lastIndex = targetIndex;
            if (targetIndex != -1)
            {
                lrcDisplay.text = lrcLines[targetIndex].original;
                if (translationDisplay) translationDisplay.text = lrcLines[targetIndex].translation;
            }
        }
    }

    // --- DVD 屏保弹跳逻辑 ---
    void BounceMotion(RectTransform rect, Text text, ref Vector2 direction)
    {
        if (string.IsNullOrEmpty(text.text) || canvasRect == null) return;

        Vector2 currentPos = rect.anchoredPosition + direction * moveSpeed * Time.deltaTime;

        // 计算文本缩放后的半宽半高
        float halfW = (text.preferredWidth / 2f) * rect.localScale.x;
        float halfH = (text.preferredHeight / 2f) * rect.localScale.y;
        float bW = canvasRect.rect.width / 2f;
        float bH = canvasRect.rect.height / 2f;

        // 碰撞检测与反弹
        if (currentPos.x + halfW > bW) { direction.x = -Mathf.Abs(direction.x); currentPos.x = bW - halfW; }
        else if (currentPos.x - halfW < -bW) { direction.x = Mathf.Abs(direction.x); currentPos.x = -bW + halfW; }

        if (currentPos.y + halfH > bH) { direction.y = -Mathf.Abs(direction.y); currentPos.y = bH - halfH; }
        else if (currentPos.y - halfH < -bH) { direction.y = Mathf.Abs(direction.y); currentPos.y = -bH + halfH; }

        rect.anchoredPosition = currentPos;
    }

    void HandleColorLogic() 
    {
        colorTimer += Time.deltaTime;
        if (colorTimer >= colorChangeInterval) {
            targetColor = Color.HSVToRGB(UnityEngine.Random.value, 0.7f, 0.9f);
            colorTimer = 0;
        }
        lrcDisplay.color = Color.Lerp(lrcDisplay.color, targetColor, Time.deltaTime * colorTransitionSpeed);
        if (translationDisplay) translationDisplay.color = Color.Lerp(translationDisplay.color, targetColor, Time.deltaTime * colorTransitionSpeed);
    }

    float ParseTime(string timeStr) 
    {
        try {
            string[] parts = timeStr.Split(':');
            return float.Parse(parts[0]) * 60f + float.Parse(parts[1]);
        } catch {
            return 0;
        }
    }

    Vector2 GetRandomDirection() 
    {
        float angle = UnityEngine.Random.Range(0, 360f) * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)).normalized;
    }
}