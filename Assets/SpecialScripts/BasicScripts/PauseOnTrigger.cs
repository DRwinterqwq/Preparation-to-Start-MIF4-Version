using UnityEngine;
using UnityEngine.Events;

public class PauseOnTrigger : MonoBehaviour
{
    [Header("目标物体设置")]
    [SerializeField] private GameObject targetObject; // 直接指定目标物体
    
    [Header("触发设置")]
    [SerializeField] private bool requireKeyPress = true; // 是否需要按键恢复
    [SerializeField] private KeyCode resumeKey = KeyCode.Space; // 恢复按键
    [SerializeField] private bool disableTargetOnPause = true; // 暂停时禁用目标物体组件
    [SerializeField] private bool freezeTargetPhysics = true; // 暂停时冻结目标物理
    
    [Header("触发限制")]
    [SerializeField] private bool triggerOnlyOnce = true; // 只触发一次
    [SerializeField] private bool disableTriggerAfterUse = true; // 使用后禁用触发器
    
    [Header("事件")]
    public UnityEvent onPauseTriggered;
    public UnityEvent onResumeTriggered;

    [Header("UI参考")]
    [SerializeField] private GameObject pauseUI; // 可选的暂停UI

    private bool isPaused = false;
    private bool hasTriggered = false; // 记录是否已经触发过
    private MonoBehaviour[] targetComponents; // 存储目标物体的组件
    private Vector3 originalVelocity = Vector3.zero;
    private Vector3 originalAngularVelocity = Vector3.zero;
    private Rigidbody targetRigidbody;
    private Rigidbody2D targetRigidbody2D;

    #if UNITY_EDITOR
    private void OnValidate()
    {
        if (targetObject == null)
        {
            Debug.LogWarning("请指定目标物体！", this);
        }
    }
    #endif

    private void OnTriggerEnter(Collider other)
    {
        TryTriggerPause(other.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryTriggerPause(other.gameObject);
    }

    // 也可以通过碰撞触发
    private void OnCollisionEnter(Collision collision)
    {
        TryTriggerPause(collision.gameObject);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryTriggerPause(collision.gameObject);
    }

    // 统一的触发检测方法
    void TryTriggerPause(GameObject obj)
    {
        // 检查是否可以触发
        if (!CanTrigger())
            return;
        
        // 检查是否是目标物体
        if (targetObject != null && obj == targetObject)
        {
            PauseGame();
            MarkAsTriggered();
        }
    }

    // 检查是否可以触发
    bool CanTrigger()
    {
        // 如果已经在暂停状态，不触发
        if (isPaused)
            return false;
        
        // 如果设置了只触发一次且已经触发过，不触发
        if (triggerOnlyOnce && hasTriggered)
            return false;
        
        return true;
    }

    // 标记为已触发
    void MarkAsTriggered()
    {
        hasTriggered = true;
        
        // 如果需要，禁用触发器组件
        if (disableTriggerAfterUse)
        {
            Collider collider = GetComponent<Collider>();
            Collider2D collider2D = GetComponent<Collider2D>();
            
            if (collider != null)
                collider.enabled = false;
            
            if (collider2D != null)
                collider2D.enabled = false;
        }
    }

    void PauseGame()
    {
        isPaused = true;
        
        // 暂停游戏时间
        Time.timeScale = 0f;
        
        // 禁用目标物体的相关组件
        if (disableTargetOnPause && targetObject != null)
        {
            targetComponents = targetObject.GetComponents<MonoBehaviour>();
            foreach (var component in targetComponents)
            {
                if (component != null && component.enabled && component != this)
                {
                    component.enabled = false;
                }
            }
        }
        
        // 处理物理冻结
        if (freezeTargetPhysics && targetObject != null)
        {
            targetRigidbody = targetObject.GetComponent<Rigidbody>();
            targetRigidbody2D = targetObject.GetComponent<Rigidbody2D>();
            
            if (targetRigidbody != null && !targetRigidbody.isKinematic)
            {
                originalVelocity = targetRigidbody.velocity;
                originalAngularVelocity = targetRigidbody.angularVelocity;
                targetRigidbody.isKinematic = true;
            }
            
            if (targetRigidbody2D != null && targetRigidbody2D.bodyType != RigidbodyType2D.Kinematic)
            {
                originalVelocity = targetRigidbody2D.velocity;
                originalAngularVelocity = new Vector3(0, 0, targetRigidbody2D.angularVelocity);
                targetRigidbody2D.bodyType = RigidbodyType2D.Kinematic;
                targetRigidbody2D.velocity = Vector2.zero;
                targetRigidbody2D.angularVelocity = 0f;
            }
        }
        
        // 暂停音频
        AudioListener.pause = true;
        
        // 显示暂停UI
        if (pauseUI != null)
        {
            pauseUI.SetActive(true);
        }
        
        // 触发事件
        onPauseTriggered?.Invoke();
        
        Debug.Log("游戏已暂停");
    }

    void Update()
    {
        if (isPaused && requireKeyPress && Input.GetKeyDown(resumeKey))
        {
            ResumeGame();
        }
    }

    void ResumeGame()
    {
        isPaused = false;
        
        // 恢复游戏时间
        Time.timeScale = 1f;
        
        // 恢复目标物体的组件
        if (disableTargetOnPause && targetComponents != null)
        {
            foreach (var component in targetComponents)
            {
                if (component != null)
                {
                    component.enabled = true;
                }
            }
        }
        
        // 恢复物理状态
        if (freezeTargetPhysics)
        {
            if (targetRigidbody != null)
            {
                targetRigidbody.isKinematic = false;
                targetRigidbody.velocity = originalVelocity;
                targetRigidbody.angularVelocity = originalAngularVelocity;
            }
            
            if (targetRigidbody2D != null)
            {
                targetRigidbody2D.bodyType = RigidbodyType2D.Dynamic;
                targetRigidbody2D.velocity = new Vector2(originalVelocity.x, originalVelocity.y);
                targetRigidbody2D.angularVelocity = originalAngularVelocity.z;
            }
        }
        
        // 恢复音频
        AudioListener.pause = false;
        
        // 隐藏暂停UI
        if (pauseUI != null)
        {
            pauseUI.SetActive(false);
        }
        
        // 触发事件
        onResumeTriggered?.Invoke();
        
        Debug.Log("游戏已继续");
    }

    // 添加一个公共方法来手动触发暂停（如果需要）
    public void TriggerPauseManually()
    {
        if (!isPaused && targetObject != null)
        {
            if (!CanTrigger())
            {
                Debug.LogWarning("已经触发过暂停，或者不满足触发条件");
                return;
            }
            
            PauseGame();
            MarkAsTriggered();
        }
    }
    
    // 添加一个公共方法来手动恢复游戏
    public void TriggerResumeManually()
    {
        if (isPaused)
        {
            ResumeGame();
        }
    }
    
    // 检查是否在暂停状态
    public bool IsPaused()
    {
        return isPaused;
    }
    
    // 检查是否已经触发过
    public bool HasTriggered()
    {
        return hasTriggered;
    }
    
    // 重置触发状态（可以重新触发）
    public void ResetTrigger()
    {
        hasTriggered = false;
        
        // 重新启用触发器组件
        Collider collider = GetComponent<Collider>();
        Collider2D collider2D = GetComponent<Collider2D>();
        
        if (collider != null)
            collider.enabled = true;
        
        if (collider2D != null)
            collider2D.enabled = true;
        
        Debug.Log("触发器已重置");
    }
    
    // 获取当前目标物体
    public GameObject GetTargetObject()
    {
        return targetObject;
    }
    
    // 设置目标物体（可以在运行时动态更改）
    public void SetTargetObject(GameObject newTarget)
    {
        if (isPaused)
        {
            Debug.LogWarning("无法在游戏暂停时更改目标物体");
            return;
        }
        
        targetObject = newTarget;
        
        if (targetObject == null)
        {
            Debug.LogWarning("目标物体已设置为null");
        }
        else
        {
            Debug.Log($"目标物体已设置为：{targetObject.name}");
        }
    }

    void OnDestroy()
    {
        // 确保游戏时间被恢复
        if (isPaused)
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }
    }
}