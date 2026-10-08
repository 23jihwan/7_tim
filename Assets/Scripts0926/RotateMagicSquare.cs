using UnityEngine;

public class RotateMagicSquare : MonoBehaviour
{
    public float rotateSpeed = 20f;
    void Update()
    {
        transform.Rotate(Vector3.forward * rotateSpeed * Time.deltaTime);
    }
}