using System.Collections;
using System.Collections.Generic;
using DotStriker.NetApiExt.Generated.Model.pallet_dot_striker;
using UnityEngine;

public class ShipEntity : MonoBehaviour
{
    [SerializeField]
    GameObject _view;

    [SerializeField]
    GameObject _roketFlame;

    [SerializeField]
    List<GameObject> _skins;

    [SerializeField]
    LineRenderer _rangeCircle;

    Vector3 _from;
    public Vector3 From => _from;

    Vector3 _to;
    Vector3 _controlPoint;
    int _blockDifference;

    float _travelDuration;
    float _launchTime;

    bool _isLanding;
    float _landingStartTime;
    float _landingDuration = 0.4f;
    Quaternion _startRotation;
    Quaternion _targetRotation;

    UIManager _uiManager;
    FakeNetworkManager _fakeNetworkManager;

    bool _idle = true;
    public bool Idle => _idle;

    public bool LocalPlayer { get; internal set; }

    public void Start()
    {
        _uiManager = GetComponent<UIManager>();
        _fakeNetworkManager = FindAnyObjectByType<FakeNetworkManager>();
    }


    public void SetSkin(int id)
    {
        foreach (var skin in _skins)
        {
            skin.SetActive(false);
        }

        if (id == (int)AsteroidKind.Nft0)
        {
            _skins[1].SetActive(true);
        }
        else if (id == (int)AsteroidKind.Nft1)
        {
            _skins[2].SetActive(true);
        }
        else if (id == (int)AsteroidKind.Nft2)
        {
            _skins[3].SetActive(true);
        }
        else
        {
            _skins[0].SetActive(true);
        }

    }
    public void SetId(int id)
    {
        _view.GetComponent<RotationScript>().rotationSpeed = id * 10f;
        _view.GetComponent<RotationScript>().rotationAxis = new Vector3(id % 2, id % 3, id % 4);
    }

    public void Lounch(Vector3 from, Vector3 to, int currentBlock, int destinationBlock)
    {
        if (_to == to) return;

        if (_from == Vector3.zero)
        {
            transform.position = from;
        }

        _blockDifference = destinationBlock - currentBlock;
        _from = from;
        _to = to;

        _travelDuration = Mathf.Abs(_blockDifference) * 1f;
        _launchTime = Time.time;

        // Вычисляем контрольную точку для дуги вверх (можно изменить направление)
        _controlPoint = (_from + _to) / 2 + Vector3.up * 3f;

        Debug.Log($"[ShipEntity] Lounch ship from {from} to {to} with duration {_travelDuration} seconds {destinationBlock} - {currentBlock} = {_blockDifference}");
    }

    private void Update()
    {

        if (_to == Vector3.zero)
        {
            if (LocalPlayer)
            {
                if (!_rangeCircle.enabled)
                {
                    _rangeCircle.enabled = true;
                    DrawManhattanRange(4);
                    _rangeCircle.transform.position = transform.position;
                }
                else
                {
                    _rangeCircle.transform.position = transform.position; // на случай, если корабль двигается вручную
                }
            }

            _idle = true;
            return;
        }
        else
        {
            _idle = false;

            if (_rangeCircle.enabled)
                _rangeCircle.enabled = false;
        }

        float elapsed = Time.time - _launchTime;

        if (Vector3.Distance(transform.position, _to) < 0.3f && _fakeNetworkManager != null)
        {
            _fakeNetworkManager.TookAsteroid(_to);
        }

        if (elapsed >= _travelDuration)
        {
            transform.position = _to;

            // Плавно переходим в горизонтальное положение
            _isLanding = true;
            _landingStartTime = Time.time;
            _startRotation = transform.rotation;
            Vector3 euler = transform.rotation.eulerAngles;
            _targetRotation = Quaternion.Euler(0f, euler.y, 0f);

            // Reset пути
            _from = Vector3.zero;
            _to = Vector3.zero;
            _roketFlame.SetActive(false);

            _fakeNetworkManager?.CompleteFlight();
            return;
        }
        else
        {
            float t = elapsed / _travelDuration;
            Vector3 point = GetBezierPoint(_from, _controlPoint, _to, t);
            transform.position = point;

            // Поворот в сторону будущего положения
            Vector3 futurePoint = GetBezierPoint(_from, _controlPoint, _to, Mathf.Min(t + 0.01f, 1f));
            Vector3 dir = (futurePoint - point).normalized;

            if (dir != Vector3.zero)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 5f);
        }

        if (_isLanding)
        {
            float landingElapsed = Time.time - _landingStartTime;
            float t = landingElapsed / _landingDuration;

            transform.rotation = Quaternion.Slerp(_startRotation, _targetRotation, t);

            if (t >= 1f)
            {
                _isLanding = false;
            }
        }
    }

    public void ShowRoketFlame()
    {
        _roketFlame.SetActive(true);
    }

    Vector3 GetBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        return Vector3.Lerp(_from, _to, t);
        // return Mathf.Pow(1 - t, 2) * p0 +
        //        2 * (1 - t) * t * p1 +
        //        Mathf.Pow(t, 2) * p2;
    }

    void OnTriggerEnter(Collider other)
    {

    }

    void DrawManhattanRange(int range)
    {
        List<Vector3> points = new List<Vector3>();

        Vector3 center = transform.position;

        // Верхняя часть ромба
        for (int dx = -range; dx <= range; dx++)
        {
            int dz = range - Mathf.Abs(dx);
            points.Add(new Vector3(center.x + dx, center.y + 0.01f, center.z + dz));
        }

        // Нижняя часть ромба
        for (int dx = range; dx >= -range; dx--)
        {
            int dz = -range + Mathf.Abs(dx);
            points.Add(new Vector3(center.x + dx, center.y + 0.01f, center.z + dz));
        }

        // Замыкаем линию
        points.Add(points[0]);

        _rangeCircle.positionCount = points.Count;
        for (int i = 0; i < points.Count; i++)
        {
            _rangeCircle.SetPosition(i, points[i]);
        }
    }
}