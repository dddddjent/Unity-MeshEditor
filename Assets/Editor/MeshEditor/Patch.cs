using System.Collections.Generic;
using UnityEngine;

namespace Editor.MeshEditor
{
    public abstract class Patch
    {
        public Vector3[,] ControlPoints;

        public int[] Indices;
        public Vector3[] Vertices;
        public Vector2[] UVs;

        protected readonly int Tessellation = 4;
        protected readonly int NumVerticesWidth;
        protected readonly int NumVerticesHeight;

        public GameObject GObject = null;

        public Patch()
        {
        }

        public Patch(int numWidth, int numHeight, int tessellation)
        {
            Debug.Assert(numWidth >= 4 && numHeight >= 4);
            Debug.Assert(tessellation >= 1);


            Tessellation = tessellation;

            int numWidthSegments = numWidth - 3;
            int numHeightSegments = numHeight - 3;
            NumVerticesWidth = numWidthSegments * Tessellation + 1;
            NumVerticesHeight = numHeightSegments * Tessellation + 1;

            ControlPoints = new Vector3[numHeight, numWidth];

            var indices = new List<int>();
            for (int i = 0; i < NumVerticesHeight - 1; i++)
            {
                for (int j = 0; j < NumVerticesWidth - 1; j++)
                {
                    indices.Add(i * NumVerticesWidth + j);
                    indices.Add((i + 1) * NumVerticesWidth + j + 1);
                    indices.Add(i * NumVerticesWidth + j + 1);

                    indices.Add(i * NumVerticesWidth + j);
                    indices.Add((i + 1) * NumVerticesWidth + j);
                    indices.Add((i + 1) * NumVerticesWidth + j + 1);
                }
            }

            Indices = indices.ToArray();

            Vertices = new Vector3[NumVerticesWidth * NumVerticesHeight];
            UVs = new Vector2[NumVerticesWidth * NumVerticesHeight];
        }

        public void Destroy()
        {
            if (GObject != null)
                GameObject.DestroyImmediate(GObject);
        }

        protected abstract Vector3 EvaluatePosition(int controlGroupI, int controlGroupJ, float t, float s);

        protected virtual Mesh GenerateMesh()
        {
            var idx = 0;
            float tStep = 1 / (float)Tessellation;
            float sStep = 1 / (float)Tessellation;
            for (int cgroupI = 0; cgroupI < ControlPoints.GetLength(0) - 3; cgroupI++)
            {
                for (int i = 0; i < Tessellation; i++)
                {
                    for (int cgroupJ = 0; cgroupJ < ControlPoints.GetLength(1) - 3; cgroupJ++)
                    {
                        for (int j = 0; j < Tessellation; j++)
                        {
                            Vertices[idx] = EvaluatePosition(cgroupI, cgroupJ, i * tStep, j * sStep);
                            UVs[idx] = new Vector2(i * tStep, j * sStep);
                            idx++;
                        }
                    }

                    Vertices[idx] = EvaluatePosition(
                        cgroupI,
                        ControlPoints.GetLength(1) - 4,
                        i * tStep,
                        1);
                    UVs[idx] = new Vector2(i * tStep, 1);
                    idx++;
                }
            }

            // the last row
            for (int cgroupJ = 0; cgroupJ < ControlPoints.GetLength(1) - 3; cgroupJ++)
            {
                for (int j = 0; j < Tessellation; j++)
                {
                    Vertices[idx] = EvaluatePosition(
                        ControlPoints.GetLength(0) - 4,
                        cgroupJ, 1, j * sStep);
                    UVs[idx] = new Vector2(1, j * sStep);
                    idx++;
                }
            }

            // the last corner
            Vertices[idx] = EvaluatePosition(
                ControlPoints.GetLength(0) - 4,
                ControlPoints.GetLength(1) - 4, 1, 1);
            UVs[idx] = new Vector2(1, 1);
            idx++;

            Debug.Assert(idx == Vertices.Length);

            var mesh = new Mesh();
            mesh.vertices = Vertices;
            mesh.triangles = Indices;
            mesh.uv = UVs;
            mesh.RecalculateNormals();
            return mesh;
        }

        public virtual void RegenerateMesh()
        {
            GObject.GetComponent<MeshFilter>().mesh = GenerateMesh();
        }

        public virtual void GenerateObject()
        {
            if (GObject != null)
            {
                GameObject.DestroyImmediate(GObject);
            }

            GObject = new GameObject("Patch " + Random.Range(0, 100000000));
            GObject.AddComponent<MeshRenderer>();
            Renderer rend = GObject.GetComponent<Renderer>();
            Material sharedMat = rend.sharedMaterial;
            if (sharedMat == null)
            {
                sharedMat = new Material(Shader.Find("Standard"));
                rend.sharedMaterial = sharedMat;
            }

            rend.sharedMaterial.color = new Color(1, 1, 1);

            GObject.AddComponent<MeshFilter>();
            GObject.GetComponent<MeshFilter>().mesh = GenerateMesh();
        }
    }

    public class CatmullRomPatch : Patch
    {
        public CatmullRomPatch(int numWidth, int numHeight, int tessellation, Vector3[,] controlPoints) :
            base(numWidth, numHeight, tessellation)
        {
            ControlPoints = controlPoints;
            GenerateObject();
        }

        private Vector3 CatmullRom(Vector3[] p, float t)
        {
            Debug.Assert(p.Length == 4);
            float t3 = t * t * t;
            float t2 = t * t;
            float t1 = t;
            float t0 = 1;

            float a = -t3 + 2 * t2 - t1;
            float b = 3 * t3 - 5 * t2 + 2 * t0;
            float c = -3 * t3 + 4 * t2 + t1;
            float d = 1 * t3 - 1 * t2;

            return 0.5f * (a * p[0] + b * p[1] + c * p[2] + d * p[3]);
        }

        protected override Vector3 EvaluatePosition(int controlGroupI, int controlGroupJ, float t, float s)
        {
            Debug.Assert(controlGroupI >= 0 && controlGroupI < ControlPoints.GetLength(0) - 3);
            Debug.Assert(controlGroupJ >= 0 && controlGroupJ < ControlPoints.GetLength(1) - 3);

            var tempControlPoints = new Vector3[4];
            for (int j = 0; j < 4; j++)
            {
                tempControlPoints[j] = CatmullRom(new[]
                {
                    ControlPoints[controlGroupI + 0, controlGroupJ + j],
                    ControlPoints[controlGroupI + 1, controlGroupJ + j],
                    ControlPoints[controlGroupI + 2, controlGroupJ + j],
                    ControlPoints[controlGroupI + 3, controlGroupJ + j],
                }, t);
            }

            return
                CatmullRom(new[]
                {
                    tempControlPoints[0], tempControlPoints[1], tempControlPoints[2], tempControlPoints[3],
                }, s);
        }
    }

    public class CatmullRomTubePatch : Patch
    {
        public CatmullRomTubePatch(int numWidth, int numHeight, int tessellation, float radius, float height)
        {
        }

        protected override Vector3 EvaluatePosition(int controlGroupI, int controlGroupJ, float t, float s)
        {
            throw new System.NotImplementedException();
        }
    }
}