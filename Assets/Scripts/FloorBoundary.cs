using System;
using System.Collections.Generic;
using UnityEngine;

public class FloorBoundary : MonoBehaviour
{
    private List<Vector2> _boundaryVertices = new(); // 地板的边界点

    public Transform targetModel; // 需要限制运动的模型
    public float movementSpeed = 5f; // Cube的移动速度
    public bool debugDrawBoundary = true; // 是否绘制边界

    private void Start()
    {
        var meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            Debug.LogError("地板对象需要挂载MeshFilter组件！");
            return;
        }

        ExtractBoundary(meshFilter.mesh);
        Debug.Log("地板边界点数量: " + _boundaryVertices.Count);
    }

    private void Update()
    {
        if (targetModel == null) return;

        // 获取输入方向
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        // 如果没有输入，则不进行移动
        if (Mathf.Approximately(horizontal, 0) && Mathf.Approximately(vertical, 0)) return;

        // 计算预期的移动矢量
        Vector3 movement = new Vector3(horizontal, 0, vertical) * movementSpeed * Time.deltaTime;
        Vector3 targetPosition = targetModel.position + movement;

        // 初始化合法的运动矢量
        Vector3 adjustedMovement = movement;

        // 分别检测X方向和Z方向的越界情况
        // 检测X方向
        if (!CanMoveInDirection(targetModel.position, new Vector3(movement.x, 0, 0)))
        {
            adjustedMovement.x = 0; // 如果X方向越界，禁止X方向运动
        }

        // 检测Z方向
        if (!CanMoveInDirection(targetModel.position, new Vector3(0, 0, movement.z)))
        {
            adjustedMovement.z = 0; // 如果Z方向越界，禁止Z方向运动
        }

        // 更新Cube的位置
        targetModel.position += adjustedMovement;
    }
    private bool CanMoveInDirection(Vector3 currentPosition, Vector3 movement)
    {
        // 计算预期位置
        Vector3 targetPosition = currentPosition + movement;

        // 获取Cube在预期位置的四个顶点
        Vector3[] cubeCorners = GetCubeCornersXZ(targetModel, targetPosition);

        // 检查是否有任意顶点越界
        foreach (var corner in cubeCorners)
        {
            Vector2 corner2D = new Vector2(corner.x, corner.z);
            if (!IsPointInPolygon(_boundaryVertices, corner2D))
            {
                return false; // 如果任意顶点越界，返回false
            }
        }

        return true; // 如果所有顶点都在边界内，返回true
    }
    // 提取地板边界顶点
    private void ExtractBoundary(Mesh mesh)
    {
        var vertices = mesh.vertices;
        var triangles = mesh.triangles;

        // 用于记录每条边的引用次数
        var edgeCounts = new Dictionary<Edge, int>();

        // 遍历所有三角形
        for (var i = 0; i < triangles.Length; i += 3)
        {
            int[] face = { triangles[i], triangles[i + 1], triangles[i + 2] };
            for (var j = 0; j < 3; j++)
            {
                var start = face[j];
                var end = face[(j + 1) % 3];
                var edge = new Edge(start, end);

                if (!edgeCounts.TryAdd(edge, 1))
                    edgeCounts[edge]++;
            }
        }

        // 找到只被引用一次的边，确定边界顶点
        var boundaryEdges = new List<Edge>();
        foreach (var edge in edgeCounts)
        {
            if (edge.Value == 1) boundaryEdges.Add(edge.Key);
        }

        // 按顺序连接边界边，形成有序的边界顶点
        _boundaryVertices = OrderBoundaryEdges(boundaryEdges, vertices);
    }

    // 根据边界边连接成完整的多边形路径
    private List<Vector2> OrderBoundaryEdges(List<Edge> edges, Vector3[] vertices)
    {
        var orderedVertices = new List<Vector2>();

        // 从第一条边开始
        var currentEdge = edges[0];
        edges.RemoveAt(0);

        // 添加起点
        var startVertex = currentEdge.Start;
        var endVertex = currentEdge.End;
        orderedVertices.Add(new Vector2(vertices[startVertex].x, vertices[startVertex].z));
        orderedVertices.Add(new Vector2(vertices[endVertex].x, vertices[endVertex].z));

        // 依次找到连接的边
        while (edges.Count > 0)
        {
            var nextEdgeIndex = edges.FindIndex(e => e.Start == endVertex || e.End == endVertex);
            if (nextEdgeIndex == -1) break;

            var nextEdge = edges[nextEdgeIndex];
            edges.RemoveAt(nextEdgeIndex);

            // 添加顶点
            endVertex = nextEdge.Start == endVertex ? nextEdge.End : nextEdge.Start;
            orderedVertices.Add(new Vector2(vertices[endVertex].x, vertices[endVertex].z));
        }

        return orderedVertices;
    }

    // 判断点是否在多边形内（射线法）
    private static bool IsPointInPolygon(List<Vector2> polygon, Vector2 point)
    {
        var intersections = 0;
        for (var i = 0; i < polygon.Count; i++)
        {
            var v1 = polygon[i];
            var v2 = polygon[(i + 1) % polygon.Count];

            if ((point.y > Mathf.Min(v1.y, v2.y) && point.y <= Mathf.Max(v1.y, v2.y)) && point.x <= Mathf.Max(v1.x, v2.x))
            {
                var xIntersection = (point.y - v1.y) * (v2.x - v1.x) / (v2.y - v1.y) + v1.x;
                if (point.x <= xIntersection)
                {
                    intersections++;
                }
            }
        }

        return (intersections % 2 != 0);
    }

    // 获取Cube在XZ平面上指定位置的四个顶点
    private Vector3[] GetCubeCornersXZ(Transform cube, Vector3 position)
    {
        var bounds = cube.GetComponent<Renderer>().bounds;
        var size = bounds.size;
        var center = position;

        return new Vector3[]
        {
            new Vector3(center.x - size.x / 2, 0, center.z - size.z / 2), // 左下角
            new Vector3(center.x + size.x / 2, 0, center.z - size.z / 2), // 右下角
            new Vector3(center.x + size.x / 2, 0, center.z + size.z / 2), // 右上角
            new Vector3(center.x - size.x / 2, 0, center.z + size.z / 2) // 左上角
        };
    }

    // 在场景中绘制边界线
    private void OnDrawGizmos()
    {
        if (!debugDrawBoundary || _boundaryVertices.Count < 2) return;

        Gizmos.color = Color.green;
        for (var i = 0; i < _boundaryVertices.Count; i++)
        {
            Vector3 from = new Vector3(_boundaryVertices[i].x, transform.position.y, _boundaryVertices[i].y);
            Vector3 to = new Vector3(_boundaryVertices[(i + 1) % _boundaryVertices.Count].x, transform.position.y,
                _boundaryVertices[(i + 1) % _boundaryVertices.Count].y);

            Gizmos.DrawLine(from, to);
        }
    }

    // 边的结构体
    private readonly struct Edge : IEquatable<Edge>
    {
        public readonly int Start;
        public readonly int End;

        public Edge(int start, int end)
        {
            Start = Mathf.Min(start, end);
            End = Mathf.Max(start, end);
        }

        public override bool Equals(object obj)
        {
            return obj is Edge other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Start.GetHashCode() ^ End.GetHashCode();
        }

        public bool Equals(Edge other)
        {
            return Start == other.Start && End == other.End;
        }
    }
}