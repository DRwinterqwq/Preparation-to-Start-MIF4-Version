using UnityEngine;
using DG.Tweening;

public class ColliderMove : MonoBehaviour
{
    [Header("移动设置")]
    public GameObject moveObject;       // 要移动的物体
    
    [Header("触发检测")]
    public GameObject triggerObject;    // 进入此物体时触发移动
    
    [Header("目标设置")]
    public GameObject targetObject;     // 要移动到的目标物体
    
    [Header("移动参数")]
    public float moveDuration = 1f;
    public bool shouldReturn = true; 
    public float returnDuration = 0.8f;
    
    [Header("Ease曲线")]
    public Ease moveEase = Ease.OutBack;
    public Ease returnEase = Ease.InOutSine;
    
    [Header("行为设置")]
    public bool triggerOnce = false;    // 是否只触发一次
    public bool autoGetMoveObject = true; // 是否自动获取移动物体
    
    private Vector3 originalPosition;
    private bool isMoving = false;
    private bool hasTriggered = false;
    private Sequence movementSequence;
    
    void Start()
    {
        // 自动获取移动物体
        if (autoGetMoveObject || moveObject == null)
        {
            moveObject = gameObject;
        }
        
        originalPosition = moveObject.transform.position;
        DOTween.Init();
        
        SetupSelfTrigger();
    }
    
    void SetupSelfTrigger()
    {
        var selfCollider = GetComponent<Collider>();
        if (selfCollider == null)
        {
            selfCollider = gameObject.AddComponent<BoxCollider>();
        }
        selfCollider.isTrigger = true;
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject == triggerObject)
        {
            TriggerMovement();
        }
    }
    
    void TriggerMovement()
    {
        if (triggerOnce && hasTriggered) return;
        
        // 如果正在移动中，且不希望打断，可以保留 isMoving 判断
        // 或者如果需要重新触发，可以在此处 Kill 掉之前的补间
        if (!isMoving && targetObject != null && moveObject != null)
        {
            StartMoveToTarget();
            hasTriggered = true;
        }
    }
    
    void StartMoveToTarget()
    {
        isMoving = true;
        StopCurrentMovement();
        
        Vector3 targetPosition = targetObject.transform.position;
        movementSequence = DOTween.Sequence();
        
        // 1. 移动到目标
        movementSequence.Append(
            moveObject.transform.DOMove(targetPosition, moveDuration)
                .SetEase(moveEase)
        );
        
        // 2. 根据 bool 值决定是否执行返回逻辑
        if (shouldReturn)
        {
            movementSequence.AppendInterval(0.1f);
            movementSequence.Append(
                moveObject.transform.DOMove(originalPosition, returnDuration)
                    .SetEase(returnEase)
            );
        }
        
        movementSequence.OnComplete(() => 
        {
            isMoving = false;
            movementSequence = null;
        });
        
        movementSequence.Play();
    }
    
    void StopCurrentMovement()
    {
        if (movementSequence != null && movementSequence.IsActive())
        {
            movementSequence.Kill();
            movementSequence = null;
        }
    }
}