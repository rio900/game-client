using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private float followSpeed = 5f;     // скорость слежения
    [SerializeField] private float height = 10f;         // высота над целью
    [SerializeField] private float distance = 5f;        // расстояние от цели по Z
    [SerializeField] private Vector3 offset = Vector3.zero; // дополнительный сдвиг

    private Transform _target;

    public void SetTarget(Transform target)
    {
        _target = target;
    }

    void LateUpdate()
    {
        if (_target == null) return;

        // Задаём фиксированное направление "сзади" относительно мира (например, по -Z)
        Vector3 desiredPosition = _target.position
                                - Vector3.forward * distance
                                + Vector3.up * height
                                + offset;

        // Плавное перемещение
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * followSpeed);

        // Смотрим в точку над целью, но без учёта её вращения
        Vector3 lookTarget = _target.position + Vector3.up * 2f; // чуть выше центра цели
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookTarget - transform.position), Time.deltaTime * followSpeed);
    }
}