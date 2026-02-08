using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Image))]
public class HoverAnimationController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("动画配置")]
    [SerializeField] private string hoverStateName = "Hover";
    [SerializeField] private string idleStateName = "Idle";
    
    [Header("倒放设置")]
    [SerializeField] private bool useRewind = true;
    [SerializeField] [Range(0.5f, 3f)] private float forwardSpeed = 1f;
    [SerializeField] [Range(0.5f, 3f)] private float rewindSpeed = 1f;
    
    [Header("悬停响应")]
    [SerializeField] private bool allowMultipleHovers = true;
    [SerializeField] private float hoverCooldown = 0.1f; // 防止快速重复悬停
    
    private Animator animator;
    private bool isHovering = false;
    private bool isAnimating = false;
    private float lastHoverTime = 0f;
    private int hoverStateHash;
    private int idleStateHash;
    
    void Start()
    {
        animator = GetComponent<Animator>();
        
        if (animator == null)
        {
            Debug.LogError($"Animator组件未找到在 {gameObject.name} 上!");
            enabled = false;
            return;
        }
        
        // 缓存动画状态哈希值以提高性能
        hoverStateHash = Animator.StringToHash(hoverStateName);
        idleStateHash = Animator.StringToHash(idleStateName);
        
        // 确保动画控制器有正确的状态
        if (!HasAnimationState(hoverStateHash))
        {
            Debug.LogWarning($"动画状态 '{hoverStateName}' 未找到，请检查Animator Controller!");
        }
        
        // 设置初始状态
        animator.Play(idleStateHash, 0, 0f);
        animator.speed = forwardSpeed;
    }
    
    // 鼠标悬停进入
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!allowMultipleHovers && Time.time - lastHoverTime < hoverCooldown)
            return;
            
        if (isHovering) return;
        
        isHovering = true;
        lastHoverTime = Time.time;
        
        // 如果正在倒放动画，从中断处继续正向播放
        if (useRewind && isAnimating && animator.speed < 0)
        {
            // 获取当前归一化时间
            float currentTime = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            
            // 切换到悬停状态，从当前时间开始播放
            animator.Play(hoverStateHash, 0, currentTime);
            animator.speed = forwardSpeed;
        }
        else
        {
            // 从头开始播放悬停动画
            animator.Play(hoverStateHash, 0, 0f);
            animator.speed = forwardSpeed;
        }
        
        isAnimating = true;
    }
    
    // 鼠标悬停离开
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isHovering) return;
        
        isHovering = false;
        
        if (useRewind && isAnimating)
        {
            // 获取当前动画状态和时间
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            float currentTime = stateInfo.normalizedTime;
            
            // 确保时间在有效范围内
            if (currentTime < 0f) currentTime = 0f;
            if (currentTime > 1f) currentTime = 1f;
            
            // 倒放动画
            animator.speed = -rewindSpeed;
            animator.Play(stateInfo.fullPathHash, 0, currentTime);
        }
        else
        {
            // 直接切换到空闲状态
            animator.speed = forwardSpeed;
            animator.Play(idleStateHash, 0, 0f);
            isAnimating = false;
        }
    }
    
    void Update()
    {
        if (!useRewind || !isAnimating) return;
        
        // 检查倒放是否完成
        if (!isHovering && animator.speed < 0)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            
            // 如果倒放完成（时间<=0），切换到空闲状态
            if (stateInfo.normalizedTime <= 0f)
            {
                animator.speed = 0f; // 暂停动画
                animator.Play(idleStateHash, 0, 0f);
                animator.speed = forwardSpeed;
                isAnimating = false;
            }
        }
        
        // 检查正向播放是否完成
        if (isHovering && animator.speed > 0)
        {
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            
            // 如果动画播放完成但仍在悬停，保持在最后一帧
            if (stateInfo.normalizedTime >= 1f)
            {
                animator.speed = 0f; // 暂停在最后一帧
            }
        }
    }
    
    // 检查动画状态是否存在
    private bool HasAnimationState(int stateHash)
    {
        if (animator == null || !animator.isActiveAndEnabled) return false;
        
        // 检查是否有对应的状态
        var controller = animator.runtimeAnimatorController;
        if (controller == null) return false;
        
        foreach (var state in controller.animationClips)
        {
            if (Animator.StringToHash(state.name) == stateHash)
                return true;
        }
        
        return false;
    }
    
    // 手动触发悬停（可在代码中调用）
    public void TriggerHover()
    {
        OnPointerEnter(null);
    }
    
    // 手动取消悬停（可在代码中调用）
    public void TriggerUnhover()
    {
        OnPointerExit(null);
    }
    
    // 重置到初始状态
    public void ResetToIdle()
    {
        isHovering = false;
        isAnimating = false;
        animator.speed = forwardSpeed;
        animator.Play(idleStateHash, 0, 0f);
    }
    
}