using UnityEngine;
using System.Collections;

public class DisappearController : MonoBehaviour
{
    [Header("消失效果设置")]
    public float delay = 0f;
    public float duration = 2.0f;
    public AnimationCurve disappearCurve = AnimationCurve.Linear(0, 0, 1, 1);
    public bool useWorldSpace = true;
    public bool destroyOnComplete = false;
    public bool fadeOut = true;
    public bool topToBottom = true;
    [Range(0.01f, 5f)] public float fadeRange = 0.5f;
    public bool smoothTransition = true; // 平滑过渡

    [Header("调试")]
    public bool startOnEnable = false;
    public KeyCode debugKey = KeyCode.Space;

    [Header("事件")]
    public UnityEngine.Events.UnityEvent onDisappearStart;
    public UnityEngine.Events.UnityEvent onDisappearComplete;

    // 私有变量
    private Material originalMaterial;
    private Material disappearMaterial;
    private Renderer objectRenderer;
    private TextMesh textMesh;

    private Coroutine disappearCoroutine;
    private bool isInitialized = false;
    private bool isDisappearing = false;
    private Color originalTextColor;

    // Shader属性ID
    private static readonly int CutoffID = Shader.PropertyToID("_Cutoff");
    private static readonly int UseWorldSpaceID = Shader.PropertyToID("_UseWorldSpace");
    private static readonly int DirectionID = Shader.PropertyToID("_Direction");
    private static readonly int FadeRangeID = Shader.PropertyToID("_FadeRange");
    private static readonly int OffsetID = Shader.PropertyToID("_Offset");
    private static readonly int ColorID = Shader.PropertyToID("_Color");

    void Start()
    {
        if (!isInitialized)
            Initialize();
    }

    void OnEnable()
    {
        if (startOnEnable)
        {
            if (!isInitialized)
                Initialize();
            StartDisappearing();
        }
    }

    void OnDisable()
    {
        StopDisappearing();
        ResetMaterial();
    }

    void Update()
    {
        if (Input.GetKeyDown(debugKey))
        {
            if (!isDisappearing)
                StartDisappearing();
            else
                StopDisappearing();
        }
    }

    public void Initialize()
    {
        if (isInitialized) return;

        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer == null)
        {
            Debug.LogError("No Renderer found on object: " + gameObject.name);
            return;
        }

        textMesh = GetComponent<TextMesh>();
        originalMaterial = objectRenderer.material;

        if (textMesh != null)
        {
            originalTextColor = textMesh.color;
        }

        disappearMaterial = new Material(originalMaterial);
        disappearMaterial.name = originalMaterial.name + "_Disappear";

        Shader disappearShader = Shader.Find("Custom/DisappearMask");
        if (disappearShader != null)
        {
            disappearMaterial.shader = disappearShader;
        }
        else
        {
            Debug.LogError("Shader 'Custom/DisappearMask' not found!");
        }

        disappearMaterial.SetFloat(UseWorldSpaceID, useWorldSpace ? 1 : 0);
        disappearMaterial.SetFloat(DirectionID, topToBottom ? 1 : -1);
        disappearMaterial.SetFloat(FadeRangeID, fadeRange);

        if (textMesh != null)
        {
            disappearMaterial.SetColor(ColorID, originalTextColor);
        }

        UpdateCutoffPosition(GetInitialCutoff());
        isInitialized = true;
    }

    float GetInitialCutoff()
    {
        if (objectRenderer == null) return 0;
        Bounds bounds = objectRenderer.bounds;
        if (useWorldSpace)
        {
            return topToBottom ? bounds.max.y + fadeRange : bounds.min.y - fadeRange;
        }
        else
        {
            return topToBottom ? 10f : -10f;
        }
    }

    void UpdateCutoffPosition(float cutoff)
    {
        if (disappearMaterial != null)
        {
            disappearMaterial.SetFloat(CutoffID, cutoff);
        }
    }

    public void StartDisappearing()
    {
        if (!isInitialized) Initialize();
            
        if (objectRenderer == null || disappearMaterial == null) return;

        if (disappearCoroutine != null)
        {
            StopCoroutine(disappearCoroutine);
        }

        disappearCoroutine = StartCoroutine(DisappearRoutine());
    }

    public void StopDisappearing()
    {
        if (disappearCoroutine != null)
        {
            StopCoroutine(disappearCoroutine);
            disappearCoroutine = null;
        }
        isDisappearing = false;
    }

    public void ResetMaterial()
    {
        if (objectRenderer != null && originalMaterial != null)
        {
            objectRenderer.material = originalMaterial;
        }
    }

    IEnumerator DisappearRoutine()
    {
        isDisappearing = true;

        // 1. 处理延迟
        if (delay > 0)
        {
            yield return new WaitForSeconds(delay);
        }

        // 2. 延迟结束后开始准备切换材质
        if (smoothTransition)
        {
            if (textMesh != null)
            {
                disappearMaterial.SetColor(ColorID, textMesh.color);
                originalTextColor = textMesh.color;
            }
            UpdateCutoffPosition(GetInitialCutoff());
            objectRenderer.material = disappearMaterial;
        }
        else
        {
            objectRenderer.material = disappearMaterial;
        }

        // 触发动画正式开始事件
        onDisappearStart?.Invoke();

        // 3. 计算动画起止点
        float startCutoff = GetInitialCutoff();
        float endCutoff = useWorldSpace ? 
            (topToBottom ? objectRenderer.bounds.min.y - fadeRange : objectRenderer.bounds.max.y + fadeRange) :
            (topToBottom ? -10f : 10f);
        
        float elapsed = 0;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalizedTime = elapsed / duration;
            float curveValue = disappearCurve.Evaluate(normalizedTime);
            
            float currentCutoff = Mathf.Lerp(startCutoff, endCutoff, curveValue);
            UpdateCutoffPosition(currentCutoff);

            if (fadeOut && normalizedTime > 0.7f)
            {
                float alphaFade = Mathf.Lerp(1, 0, (normalizedTime - 0.7f) / 0.3f);
                Color currentColor = disappearMaterial.GetColor(ColorID);
                currentColor.a = originalTextColor.a * alphaFade;
                disappearMaterial.SetColor(ColorID, currentColor);
            }
            yield return null;
        }

        UpdateCutoffPosition(endCutoff);
        disappearCoroutine = null;
        isDisappearing = false;
        
        onDisappearComplete?.Invoke();

        if (destroyOnComplete)
        {
            Destroy(gameObject);
        }
    }

    public void SetCutoffPosition(float cutoff)
    {
        if (!isInitialized) Initialize();
        UpdateCutoffPosition(cutoff);
    }

    public void SetYOffset(float offset)
    {
        if (!isInitialized) Initialize();
        if (disappearMaterial != null) disappearMaterial.SetFloat(OffsetID, offset);
    }

    void OnDestroy()
    {
        if (disappearMaterial != null)
        {
            if (Application.isPlaying) Destroy(disappearMaterial);
            else DestroyImmediate(disappearMaterial);
        }
    }
}