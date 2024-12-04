using UnityEngine;

public class GeneratePerfectBoxColliders : MonoBehaviour
{
    private void Start()
    {
        var mesh = GetComponent<MeshFilter>().mesh;
        if (mesh == null)
        {
            return;
        }
        GeneratePerfectFitColliders(mesh);
    }

    private void GeneratePerfectFitColliders(Mesh mesh)
    {
        var vertices = mesh.vertices;
        var triangles = mesh.triangles;

        for (var i = 0; i < triangles.Length; i += 3)
        {
            var v1 = vertices[triangles[i]];
            var v2 = vertices[triangles[i + 1]];
            var v3 = vertices[triangles[i + 2]];

            var bounds = new Bounds(v1, Vector3.zero);
            bounds.Encapsulate(v2);
            bounds.Encapsulate(v3);

            var boxColliderObject = new GameObject($"BoxCollider_{i / 3}")
            {
                transform =
                {
                    parent = transform
                }
            };

            var boxCollider = boxColliderObject.AddComponent<BoxCollider>();
            boxCollider.center = bounds.center;
            boxCollider.size = bounds.size;

            boxColliderObject.transform.localPosition = Vector3.zero;
            boxColliderObject.transform.localRotation = Quaternion.identity;
            boxColliderObject.transform.localScale = Vector3.one;
        }

        Debug.Log($"Generate {triangles.Length / 3} BoxColliders");
    }
}