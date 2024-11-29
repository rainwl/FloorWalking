using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public class MeshInsidePoint : MonoBehaviour
{
    private Mesh _elevatedMesh;
    private Vector3[] _elevatedVertices;
    private int[] _triangles;

    public float yOffset = 1.0f;
    public Transform targetModel;
    public float movementSpeed = 5f;

    private void Start()
    {
        // 获取原始Mesh并生成抬高后的Mesh
        var originalMesh = GetComponent<MeshFilter>().mesh;
        _elevatedMesh = CreateElevatedMesh(originalMesh, yOffset);

        // 缓存顶点和三角形
        _elevatedVertices = _elevatedMesh.vertices;
        _triangles = _elevatedMesh.triangles;
    }

    private void Update()
    {
        if (targetModel == null) return;

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        if (Mathf.Approximately(horizontal, 0) && Mathf.Approximately(vertical, 0)) return;

        Vector3 movement = new Vector3(horizontal, 0, vertical) * movementSpeed * Time.deltaTime;
        Vector3 targetPosition = targetModel.position + movement;

        Vector3[] cubeCorners = GetCubeCorners(targetModel, targetPosition);

        // 使用Job System进行点内外判断
        NativeArray<Vector3> nativeCorners = new NativeArray<Vector3>(cubeCorners, Allocator.TempJob);
        NativeArray<Vector3> nativeVertices = new NativeArray<Vector3>(_elevatedVertices, Allocator.TempJob);
        NativeArray<int> nativeTriangles = new NativeArray<int>(_triangles, Allocator.TempJob);
        NativeArray<bool> results = new NativeArray<bool>(cubeCorners.Length, Allocator.TempJob);

        IsPointInsideMeshJob job = new IsPointInsideMeshJob
        {
            points = nativeCorners,
            vertices = nativeVertices,
            triangles = nativeTriangles,
            results = results
        };

        JobHandle handle = job.Schedule(cubeCorners.Length, 1);
        handle.Complete();

        // 检查结果
        bool isInside = true;
        for (int i = 0; i < results.Length; i++)
        {
            if (!results[i])
            {
                isInside = false;
                break;
            }
        }

        nativeCorners.Dispose();
        nativeVertices.Dispose();
        nativeTriangles.Dispose();
        results.Dispose();

        if (isInside)
        {
            targetModel.position = targetPosition;
        }
    }

    private Mesh CreateElevatedMesh(Mesh originalMesh, float yOffset)
    {
        var vertices = originalMesh.vertices;
        var triangles = originalMesh.triangles;

        float maxY = float.MinValue;
        foreach (var vertex in vertices)
        {
            if (vertex.y > maxY)
            {
                maxY = vertex.y;
            }
        }

        var elevatedVertices = new Vector3[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            if (Mathf.Approximately(vertices[i].y, maxY))
            {
                elevatedVertices[i] = vertices[i] + new Vector3(0, yOffset, 0);
            }
            else
            {
                elevatedVertices[i] = vertices[i];
            }
        }

        Mesh elevatedMesh = new Mesh
        {
            vertices = elevatedVertices,
            triangles = triangles
        };

        elevatedMesh.RecalculateNormals();
        elevatedMesh.RecalculateBounds();

        return elevatedMesh;
    }

    private Vector3[] GetCubeCorners(Transform cube, Vector3 position)
    {
        var bounds = cube.GetComponent<Renderer>().bounds;
        var size = bounds.size;

        return new Vector3[]
        {
            new Vector3(position.x - size.x / 2, position.y - size.y / 2, position.z - size.z / 2),
            new Vector3(position.x + size.x / 2, position.y - size.y / 2, position.z - size.z / 2),
            new Vector3(position.x + size.x / 2, position.y - size.y / 2, position.z + size.z / 2),
            new Vector3(position.x - size.x / 2, position.y - size.y / 2, position.z + size.z / 2),
            // new Vector3(position.x - size.x / 2, position.y + size.y / 2, position.z - size.z / 2),
            // new Vector3(position.x + size.x / 2, position.y + size.y / 2, position.z - size.z / 2),
            // new Vector3(position.x + size.x / 2, position.y + size.y / 2, position.z + size.z / 2),
            // new Vector3(position.x - size.x / 2, position.y + size.y / 2, position.z + size.z / 2)
        };
    }

    [BurstCompile]
    private struct IsPointInsideMeshJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Vector3> points;
        [ReadOnly] public NativeArray<Vector3> vertices;
        [ReadOnly] public NativeArray<int> triangles;
        [WriteOnly] public NativeArray<bool> results;

        public void Execute(int index)
        {
            Vector3 point = points[index];
            int intersectCount = 0;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 v1 = vertices[triangles[i]];
                Vector3 v2 = vertices[triangles[i + 1]];
                Vector3 v3 = vertices[triangles[i + 2]];

                if (RayTriangleIntersection(new Ray(point, Vector3.down), v1, v2, v3))
                {
                    intersectCount++;
                }
            }

            results[index] = intersectCount % 2 == 1;
        }

        private static bool RayTriangleIntersection(Ray ray, Vector3 v1, Vector3 v2, Vector3 v3)
        {
            Vector3 e1 = v2 - v1;
            Vector3 e2 = v3 - v1;

            Vector3 p = Vector3.Cross(ray.direction, e2);
            float det = Vector3.Dot(e1, p);

            if (det > -0.00001f && det < 0.00001f) return false;

            float invDet = 1 / det;
            Vector3 t = ray.origin - v1;

            float u = Vector3.Dot(t, p) * invDet;
            if (u < 0 || u > 1) return false;

            Vector3 q = Vector3.Cross(t, e1);
            float v = Vector3.Dot(ray.direction, q) * invDet;
            if (v < 0 || u + v > 1) return false;

            float tValue = Vector3.Dot(e2, q) * invDet;
            return !(tValue < 0) && !(tValue > 1000);
        }
    }
}
