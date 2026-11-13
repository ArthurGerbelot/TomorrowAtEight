// ARCH-001.a | 2026-11-10 | Procedural providers are the initial asset source.
// Decision: .agents/architecture/ARCH-001.a-content-registry.md
// Journal: .agents/days/2026-11-12.md
using UnityEngine;

namespace TomorrowAtEight.Definitions
{
    /// <summary>Original primitive meshes generated in code. Provisional placeholders.</summary>
    public static class ProceduralMeshes
    {
        /// <summary>Unit cube centered on the origin with flat-shaded faces.</summary>
        public static Mesh CreateCube(string name)
        {
            Vector3[] normals =
            {
                Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back,
            };

            var vertices = new Vector3[24];
            var vertexNormals = new Vector3[24];
            var triangles = new int[36];
            for (int face = 0; face < 6; face++)
            {
                Vector3 normal = normals[face];
                // Two axes spanning the face, chosen so (u x v) points along the normal.
                Vector3 u = Mathf.Abs(normal.y) > 0.5f ? Vector3.right : Vector3.up;
                Vector3 v = Vector3.Cross(normal, u);
                u = Vector3.Cross(v, normal);

                int start = face * 4;
                vertices[start + 0] = (normal - u - v) * 0.5f;
                vertices[start + 1] = (normal + u - v) * 0.5f;
                vertices[start + 2] = (normal + u + v) * 0.5f;
                vertices[start + 3] = (normal - u + v) * 0.5f;
                for (int i = 0; i < 4; i++)
                    vertexNormals[start + i] = normal;

                int t = face * 6;
                AddTriangle(vertices, triangles, t, start + 0, start + 1, start + 2, normal);
                AddTriangle(vertices, triangles, t + 3, start + 0, start + 2, start + 3, normal);
            }

            return Build(name, vertices, vertexNormals, triangles);
        }

        /// <summary>Flat-shaded octahedron with unit height and width.</summary>
        public static Mesh CreateOctahedron(string name)
        {
            Vector3[] corners =
            {
                new Vector3(0f, 0.5f, 0f), new Vector3(0f, -0.5f, 0f),
                new Vector3(0.5f, 0f, 0f), new Vector3(0f, 0f, 0.5f),
                new Vector3(-0.5f, 0f, 0f), new Vector3(0f, 0f, -0.5f),
            };

            var vertices = new Vector3[24];
            var vertexNormals = new Vector3[24];
            var triangles = new int[24];
            int index = 0;
            for (int cap = 0; cap < 2; cap++)
            {
                for (int side = 0; side < 4; side++)
                {
                    Vector3 a = corners[cap];
                    Vector3 b = corners[2 + side];
                    Vector3 c = corners[2 + (side + 1) % 4];
                    Vector3 outward = (a + b + c).normalized;

                    vertices[index] = a;
                    vertices[index + 1] = b;
                    vertices[index + 2] = c;
                    vertexNormals[index] = vertexNormals[index + 1] = vertexNormals[index + 2] = outward;
                    AddTriangle(vertices, triangles, index, index, index + 1, index + 2, outward);
                    index += 3;
                }
            }

            return Build(name, vertices, vertexNormals, triangles);
        }

        /// <summary>Unit square on the XZ plane facing up.</summary>
        public static Mesh CreatePlane(string name)
        {
            Vector3[] vertices =
            {
                new Vector3(-0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, -0.5f),
                new Vector3(0.5f, 0f, 0.5f), new Vector3(-0.5f, 0f, 0.5f),
            };
            Vector3[] vertexNormals = { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            var triangles = new int[6];
            AddTriangle(vertices, triangles, 0, 0, 1, 2, Vector3.up);
            AddTriangle(vertices, triangles, 3, 0, 2, 3, Vector3.up);
            return Build(name, vertices, vertexNormals, triangles);
        }

        // Writes the triangle with the winding that faces along the given outward normal
        // (Unity front faces are clockwise when seen from outside).
        static void AddTriangle(Vector3[] vertices, int[] triangles, int offset, int a, int b, int c, Vector3 outward)
        {
            Vector3 facing = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
            bool alreadyOutward = Vector3.Dot(facing, outward) > 0f;
            triangles[offset] = a;
            triangles[offset + 1] = alreadyOutward ? b : c;
            triangles[offset + 2] = alreadyOutward ? c : b;
        }

        static Mesh Build(string name, Vector3[] vertices, Vector3[] normals, int[] triangles)
        {
            var mesh = new Mesh { name = name };
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
