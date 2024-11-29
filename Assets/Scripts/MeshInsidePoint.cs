using System;
using UnityEngine;

public class MeshInsidePoint : MonoBehaviour
{
    private Mesh _originalMesh; // 原始Mesh
    private Mesh _elevatedMesh; // 抬高后的Mesh
    public float yOffset = 1.0f; // 顶点Y轴抬高的偏移量
    public Transform targetModel; // 需要限制运动的目标模型（Cube）
    public float movementSpeed = 5f; // 控制Cube移动速度

    private void Start()
    {
        // 获取原始Mesh
        _originalMesh = GetComponent<MeshFilter>().mesh;
        // 生成抬高Y值的Mesh
        _elevatedMesh = CreateElevatedMesh(_originalMesh, yOffset);
    }

    private void Update()
    {
        if (targetModel == null) return;

        // 获取用户输入
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        if (Mathf.Approximately(horizontal, 0) && Mathf.Approximately(vertical, 0)) return;

        // 计算移动矢量
        Vector3 movement = new Vector3(horizontal, 0, vertical) * movementSpeed * Time.deltaTime;
        Vector3 targetPosition = targetModel.position + movement;

        // 获取Cube的8个顶点
        Vector3[] cubeCorners = GetCubeCorners(targetModel, targetPosition);

        // 判断Cube的所有顶点是否在扩展后的Mesh内部
        bool isInside = true;
        foreach (var corner in cubeCorners)
        {
            if (!IsPointInsideMesh(corner, _elevatedMesh))
            {
                Debug.Log($"Corner {corner} is outside the mesh");
                isInside = false;
                break;
            }
        }

        // 如果Cube完全在Mesh内部，则允许移动
        if (isInside)
        {
            targetModel.position = targetPosition;
        }
        else
        {
            Debug.Log("Cube position is restricted due to boundary constraints.");
        }
    }

    private Mesh CreateElevatedMesh(Mesh originalMesh, float yOffset)
    {
        // 获取原始Mesh的顶点和三角形
        var vertices = originalMesh.vertices;
        var triangles = originalMesh.triangles;

        // 找到最大Y值
        float maxY = float.MinValue;
        foreach (var vertex in vertices)
        {
            if (vertex.y > maxY)
            {
                maxY = vertex.y;
            }
        }

        // 创建新的顶点数组
        var elevatedVertices = new Vector3[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            // 如果顶点是顶部顶点（Y值为最大值），抬高它的Y值
            if (Mathf.Approximately(vertices[i].y, maxY))
            {
                elevatedVertices[i] = vertices[i] + new Vector3(0, yOffset, 0);
            }
            else
            {
                // 否则保持原状
                elevatedVertices[i] = vertices[i];
            }
        }

        // 构建新的Mesh
        Mesh elevatedMesh = new Mesh
        {
            vertices = elevatedVertices,
            triangles = triangles
        };

        elevatedMesh.RecalculateNormals();
        elevatedMesh.RecalculateBounds();

        return elevatedMesh;
    }

    private bool IsPointInsideMesh(Vector3 point, Mesh mesh)
    {
        // 转换点到局部坐标系
        point = transform.InverseTransformPoint(point);

        // 获取Mesh三角形
        var triangles = mesh.triangles;

        // 从点向下发射射线
        var ray = new Ray(point, Vector3.down);
        Debug.DrawRay(ray.origin, ray.direction, Color.green);

        // 计算与三角形的交点
        var intersectCount = 0;

        for (var i = 0; i < triangles.Length; i += 3)
        {
            var v1 = mesh.vertices[triangles[i]];
            var v2 = mesh.vertices[triangles[i + 1]];
            var v3 = mesh.vertices[triangles[i + 2]];

            if (RayTriangleIntersection(ray, v1, v2, v3))
            {
                intersectCount++;
            }
        }

        // 判断点是否在Mesh内
        return intersectCount % 2 == 1;
    }

    private static bool RayTriangleIntersection(Ray ray, Vector3 v1, Vector3 v2, Vector3 v3)
    {
        // 计算射线与三角形的交点
        var e1 = v2 - v1;
        var e2 = v3 - v1;

        var p = Vector3.Cross(ray.direction, e2);
        var det = Vector3.Dot(e1, p);

        // 如果射线与平面平行，则无交点
        if (det > -0.00001f && det < 0.00001f)
            return false;

        var invDet = 1 / det;
        var t = ray.origin - v1;

        var u = Vector3.Dot(t, p) * invDet;

        // 如果交点在三角形外部
        if (u < 0 || u > 1)
            return false;

        var q = Vector3.Cross(t, e1);
        var v = Vector3.Dot(ray.direction, q) * invDet;

        // 如果交点在三角形外部
        if (v < 0 || u + v > 1)
            return false;

        var tValue = Vector3.Dot(e2, q) * invDet;

        // 判断交点是否在射线范围内
        return !(tValue < 0) && !(tValue > 1000);
    }

    private Vector3[] GetCubeCorners(Transform cube, Vector3 position)
    {
        var bounds = cube.GetComponent<Renderer>().bounds;
        var size = bounds.size;

        return new Vector3[]
        {
            new Vector3(position.x - size.x / 2, position.y - size.y / 2, position.z - size.z / 2), // 左下前
            new Vector3(position.x + size.x / 2, position.y - size.y / 2, position.z - size.z / 2), // 右下前
            new Vector3(position.x + size.x / 2, position.y - size.y / 2, position.z + size.z / 2), // 右下后
            new Vector3(position.x - size.x / 2, position.y - size.y / 2, position.z + size.z / 2), // 左下后
            new Vector3(position.x - size.x / 2, position.y + size.y / 2, position.z - size.z / 2), // 左上前
            new Vector3(position.x + size.x / 2, position.y + size.y / 2, position.z - size.z / 2), // 右上前
            new Vector3(position.x + size.x / 2, position.y + size.y / 2, position.z + size.z / 2), // 右上后
            new Vector3(position.x - size.x / 2, position.y + size.y / 2, position.z + size.z / 2)  // 左上后
        };
    }
}
