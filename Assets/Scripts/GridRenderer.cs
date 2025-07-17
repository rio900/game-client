using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridRenderer : MonoBehaviour
{
    public float cellSize = 1f;
    public Material lineMaterial;

    void Start()
    {
    }

    public void Draw(int gridSize)
    {
        for (int i = 0; i <= gridSize; i++)
        {
            // Горизонтальные линии
            CreateLine(new Vector3(0, 0, i * cellSize), new Vector3(gridSize * cellSize, 0, i * cellSize));

            // Вертикальные линии
            CreateLine(new Vector3(i * cellSize, 0, 0), new Vector3(i * cellSize, 0, gridSize * cellSize));
        }
    }

    void CreateLine(Vector3 start, Vector3 end)
    {
        GameObject lineObj = new GameObject("GridLine");
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.material = lineMaterial;
        lr.startWidth = 0.01f;
        lr.endWidth = 0.01f;
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
    }
}