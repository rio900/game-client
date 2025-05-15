using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneClickDetector : MonoBehaviour
{
    [SerializeField]
    NetworkManager _networkManager;

    public LayerMask planeLayer;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

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
        _networkManager.LounchStarship(coord);
        Debug.Log($"Tapped at cell: {coord}");
    }
}
