using UnityEngine;
using UnityEngine.EventSystems;

public class ImageHoverAnimation : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("引用设置")]
    public Animator targetAnimator;

    [Header("动画配置")]
    [SerializeField] private string stateName = "HoverState";
    [SerializeField] private float transitionSpeed = 2f;

    private float currentProgress = 0f;
    private float targetProgress = 0f;
    private int stateHash;

    void Awake()
    {
        stateHash = Animator.StringToHash(stateName);

        #if !UNITY_STANDALONE_WIN && !UNITY_EDITOR
            this.enabled = false;
            return;
        #endif

        if (targetAnimator == null) targetAnimator = GetComponent<Animator>();
        
        if (targetAnimator != null)
        {
            targetAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
        }
    }

    void OnEnable()
    {
        if (targetAnimator != null)
        {
            targetAnimator.speed = 0;
            targetAnimator.Play(stateHash, 0, currentProgress);
            targetAnimator.Update(0);
        }
    }

    void Update()
    {
        if (targetAnimator == null) return;

        if (Mathf.Abs(currentProgress - targetProgress) > 0.0001f)
        {
            currentProgress = Mathf.MoveTowards(currentProgress, targetProgress, Time.unscaledDeltaTime * transitionSpeed);
            
            targetAnimator.Play(stateHash, 0, currentProgress);
            
            targetAnimator.Update(0); 
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetProgress = 1f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetProgress = 0f;
    }
}