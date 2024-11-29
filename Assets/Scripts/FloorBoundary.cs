using System;
using System.Collections.Generic;
using UnityEngine;

public class FloorBoundary : MonoBehaviour
{
    private readonly List<Vector2> _boundaryVertices = new();

    public Transform targetModel;

    private void Start()
    {
        var meshFilter = GetComponentInChildren<MeshFilter>();
        if (meshFilter == null)
        {
            Debug.LogError("The floor object needs to mount the MeshFilter component");
            return;
        }

        ExtractBoundary(meshFilter.mesh);
        Debug.Log("Boundary vertex count: " + _boundaryVertices.Count);
    }

    void Update()
    {
        if (targetModel != null)
        {
            Vector3 targetPosition = targetModel.position;
            Vector2 targetPosition2D = new Vector2(targetPosition.x, targetPosition.z);

            // 判断目标模型是否在边界内
            if (!IsPointInPolygon(_boundaryVertices, targetPosition2D))
            {
                // 如果目标超出边界，限制其位置
                Vector3 nearestPosition = GetNearestPointInPolygon(_boundaryVertices, targetPosition2D);
                targetModel.position = new Vector3(nearestPosition.x, targetPosition.y, nearestPosition.y);
            }
        }
    }

    // 提取地板边界顶点
    private void ExtractBoundary(Mesh mesh)
    {
        var vertices = mesh.vertices;
        var triangles = mesh.triangles;
        var edgeCounts = new Dictionary<Edge, int>();

        // Traverse all triangles, recording the number of side references
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

        // Find an edge that is only referenced once
        var boundaryVertexIndices = new HashSet<int>();
        foreach (var edge in edgeCounts)
        {
            if (edge.Value == 1)
            {
                boundaryVertexIndices.Add(edge.Key.Start);
                boundaryVertexIndices.Add(edge.Key.End);
            }
        }

        // Convert to 2D vertices of the XZ plane
        foreach (var index in boundaryVertexIndices)
        {
            var vertex = transform.TransformPoint(vertices[index]);
            _boundaryVertices.Add(new Vector2(vertex.x, vertex.z));
        }
    }

    // Determine whether a point is inside a polygon (ray method)
    private static bool IsPointInPolygon(List<Vector2> polygon, Vector2 point)
    {
        var intersections = 0;
        for (var i = 0; i < polygon.Count; i++)
        {
            var v1 = polygon[i];
            var v2 = polygon[(i + 1) % polygon.Count];

            // Whether the checkpoint is in the Y range of the line segment
            if ((point.y > Mathf.Min(v1.y, v2.y) && point.y <= Mathf.Max(v1.y, v2.y)) && point.x <= Mathf.Max(v1.x, v2.x))
            {
                // Calculate the point at which the ray intersects the edge
                var xIntersection = (point.y - v1.y) * (v2.x - v1.x) / (v2.y - v1.y) + v1.x;
                if (point.x <= xIntersection)
                {
                    intersections++;
                }
            }
        }

        // If the number of intersections is odd, the points are inside the polygon
        return (intersections % 2 != 0);
    }

    // Gets the closest point in the polygon
    private Vector3 GetNearestPointInPolygon(List<Vector2> polygon, Vector2 point)
    {
        var nearestPoint = point;
        var minDistance = float.MaxValue;

        for (var i = 0; i < polygon.Count; i++)
        {
            var v1 = polygon[i];
            var v2 = polygon[(i + 1) % polygon.Count];

            // Calculate the nearest point from a point to a line segment
            var closestPoint = GetClosestPointOnLineSegment(v1, v2, point);
            var distance = Vector2.Distance(point, closestPoint);

            if (distance < minDistance)
            {
                minDistance = distance;
                nearestPoint = closestPoint;
            }
        }

        return new Vector3(nearestPoint.x, 0, nearestPoint.y);
    }

    // Gets the nearest point from the point to the line segment
    private static Vector2 GetClosestPointOnLineSegment(Vector2 a, Vector2 b, Vector2 point)
    {
        var ab = b - a;
        var t = Vector2.Dot(point - a, ab) / ab.sqrMagnitude;
        t = Mathf.Clamp01(t);
        return a + t * ab;
    }

    // The structure of the edge
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
            if (obj is Edge other)
            {
                return Start == other.Start && End == other.End;
            }

            return false;
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