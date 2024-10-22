using System;
using System.Collections.Generic;
using Editor.MeshEditor;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using Random = UnityEngine.Random;

public class MeshEditor : EditorWindow
{
    private float selectionRadius = 2f;
    private float dotSize = 0.05f;
    private Patch patch = null;
    private int tessellation = 4;

    private List<Vector2Int> selectedVertices = new List<Vector2Int>();

    // plane
    private float planeWidth = 10f;
    private float planeHeight = 10f;
    private int numWidthPlane = 6;
    private int numHeightPlane = 6;

    [MenuItem("Window/Mesh Editor")]
    public static void ShowWindow()
    {
        GetWindow<MeshEditor>("Mesh Editor");
    }

    public MeshEditor()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    public void OnEnable()
    {
    }

    private void OnGUI()
    {
        tessellation = EditorGUILayout.IntField("Tessellation", tessellation);

        GUILayout.Label("Plane", EditorStyles.boldLabel);
        planeWidth = EditorGUILayout.FloatField("Plane Width", planeWidth);
        planeHeight = EditorGUILayout.FloatField("Plane Height", planeHeight);
        numWidthPlane = EditorGUILayout.IntField("Number Width", numWidthPlane);
        numHeightPlane = EditorGUILayout.IntField("Number Height", numHeightPlane);
        if (GUILayout.Button("Generate Plane"))
        {
            GeneratePlane();
            SceneView.RepaintAll();
        }
    }

    private void ClearupPreviousGeneration()
    {
        if (patch != null)
        {
            patch.Destroy();
            patch = null;
        }

        selectedVertices.Clear();
    }

    private void GeneratePlane()
    {
        var controlPoints = new Vector3[numHeightPlane, numWidthPlane];
        float hStep = planeHeight / (numHeightPlane - 1);
        float wStep = planeWidth / (numWidthPlane - 1);
        for (int i = 0; i < numHeightPlane; i++)
        {
            for (int j = 0; j < numWidthPlane; j++)
            {
                controlPoints[i, j] = new Vector3(j * wStep, Random.value * 2, i * hStep);
            }
        }

        ClearupPreviousGeneration();

        patch = new CatmullRomPatch(numWidthPlane, numHeightPlane, tessellation, controlPoints);
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (patch == null)
            return;

        Handles.color = Color.green;
        for (int i = 0; i < patch.ControlPoints.GetLength(0); i++)
        {
            for (int j = 0; j < patch.ControlPoints.GetLength(1); j++)
            {
                Handles.DotHandleCap(0, patch.ControlPoints[i, j], Quaternion.identity, dotSize, EventType.Repaint);
            }
        }

        Handles.color = Color.red;
        foreach (var selectedVertex in selectedVertices)
        {
            Handles.DotHandleCap(0, patch.ControlPoints[
                selectedVertex.x, selectedVertex.y
            ], Quaternion.identity, dotSize, EventType.Repaint);
        }

        if (selectedVertices.Count > 0)
        {
            EditorGUI.BeginChangeCheck();
            Vector3 newPosition = Handles.PositionHandle(
                patch.ControlPoints[selectedVertices[0].x, selectedVertices[0].y],
                Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                patch.ControlPoints[selectedVertices[0].x, selectedVertices[0].y] = newPosition;
                patch.RegenerateMesh();
                SceneView.RepaintAll();
            }
        }

        HandleMouseClick(sceneView);
    }

    private void HandleMouseClick(SceneView sceneView)
    {
        Event e = Event.current;

        if (e.type == EventType.MouseDown && e.button == 0)
        {
            selectedVertices.Clear();

            float nearestDistance = Mathf.Infinity;
            Vector2Int nearestVertexIdx = new Vector2Int(-1, -1);

            for (int i = 0; i < patch.ControlPoints.GetLength(0); i++)
            {
                for (int j = 0; j < patch.ControlPoints.GetLength(1); j++)
                {
                    float distance = HandleUtility.DistanceToCircle(patch.ControlPoints[i, j], dotSize);
                    if (distance < selectionRadius && distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestVertexIdx = new Vector2Int(i, j);
                    }
                }
            }

            if (nearestVertexIdx.x != -1)
            {
                Debug.Assert(nearestVertexIdx.y != -1);

                selectedVertices.Add(nearestVertexIdx);
                e.Use();
                SceneView.RepaintAll(); // Repaint the Scene view to update the selection
            }
        }
    }
}