using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotationScript : MonoBehaviour
{
    public Vector3 rotationAxis = Vector3.up;

    public float rotationSpeed = 45f;

    void Update()
    {
        transform.Rotate(rotationAxis.normalized * rotationSpeed * Time.deltaTime);
    }
}
