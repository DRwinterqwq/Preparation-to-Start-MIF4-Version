using UnityEngine;

public class RotateForever : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 90f; // 度/秒
    [SerializeField] private Vector3 rotationAxis = Vector3.up; // 旋转轴
    
    void Update()
    {
        // 每帧旋转
        transform.Rotate(rotationAxis * rotationSpeed * Time.deltaTime);
    }
}