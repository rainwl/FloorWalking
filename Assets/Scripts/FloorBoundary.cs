using System;
using System.Collections.Generic;
using UnityEngine;

public class FloorBoundary : MonoBehaviour
{
    private List<Vector2> _boundaryVertices = new(); // 地板的边界顶点

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
            Start = Mathf.Min(start, end); // 确保边的方向一致
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
