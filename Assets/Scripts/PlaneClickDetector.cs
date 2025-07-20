using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneClickDetector : MonoBehaviour
{
    [SerializeField]
    NetworkManager _networkManager;

    [SerializeField]
    FakeNetworkManager _fakeNetworkManager;

    public LayerMask planeLayer;

    UIManager _uiManager;
    public void Start()
    {
        _uiManager = FindAnyObjectByType<UIManager>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePosition = Input.mousePosition;


            if (mousePosition.x < 0 || mousePosition.y < 0 ||
                mousePosition.x > Screen.width || mousePosition.y > Screen.height)
            {
                Debug.Log("Click outside screen bounds – ignored");
                return;
            }

            Ray ray = Camera.main.ScreenPointToRay(mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 100f, planeLayer))
            {
                Vector3 point = hit.point;

                int x = Mathf.RoundToInt(point.x);
                int z = Mathf.RoundToInt(point.z);

                Debug.Log($"Clicked on rounded position: ({x}, {z})");

                Vector2 gridPosition = new Vector2(x, z);
                OnGridTap(gridPosition);
            }
        }
    }

    void OnGridTap(Vector2 coord)
    {
        _networkManager?.OnGridTap(coord);
        Debug.Log($"Tapped at cell: {coord}");
    }
}
