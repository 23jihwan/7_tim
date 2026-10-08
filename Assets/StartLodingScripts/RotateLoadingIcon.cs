using UnityEngine;

public class RotateLoadingIcon : MonoBehaviour
{
    [Header("회전 속도 (도/초)")]
    public float rotateSpeed = -180f; // 음수: 시계 방향 회전

    void Update()
    {
        // Z축 기준 회전
        transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);
    }
}