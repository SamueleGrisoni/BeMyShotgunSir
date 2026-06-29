using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using BeMyShotgunSir.Scripts.Gameplay.Track.Environment;

namespace BeMyShotgunSir.Scripts.Editor
{

    [CustomEditor(typeof(PolygonSpawnArea))]
    public class PolygonSpawnAreaEditor : UnityEditor.Editor
    {
        // -----------------------------------------------------------------------
        // State
        // -----------------------------------------------------------------------
        private PolygonSpawnArea _area;
        private bool _editMode = false;
        private int _hoveredSeg = -1;   // segment index mouse is near
        private int _hoveredPt = -1;   // point index mouse is near

        private const float _pointRadius = 0.35f;
        private const float _deleteRadius = 0.45f;
        private const float _insertDistSq = 1.5f * 1.5f;   // snap distance² for insertion preview

        // -----------------------------------------------------------------------
        // Colors
        // -----------------------------------------------------------------------
        private static readonly Color _colHandle = new Color(0.15f, 1.00f, 0.50f, 1.00f);
        private static readonly Color _colHandleHover = new Color(1.00f, 0.85f, 0.10f, 1.00f);
        private static readonly Color _colInsert = new Color(0.20f, 0.80f, 1.00f, 1.00f);
        private static readonly Color _colOutline = new Color(0.15f, 1.00f, 0.50f, 0.90f);
        private static readonly Color _colFill = new Color(0.15f, 1.00f, 0.50f, 0.10f);

        // -----------------------------------------------------------------------
        // Inspector GUI
        // -----------------------------------------------------------------------
        public override void OnInspectorGUI()
        {
            _area = (PolygonSpawnArea)target;

            EditorGUILayout.Space(4);

            // Toggle button
            GUI.backgroundColor = _editMode
                ? new Color(0.4f, 1f, 0.5f)
                : new Color(0.9f, 0.9f, 0.9f);

            if (GUILayout.Button(_editMode ? "✔  Exit Edit Mode" : "✏  Enter Edit Mode",
                                 GUILayout.Height(28)))
            {
                _editMode = !_editMode;
                SceneView.RepaintAll();
            }
            GUI.backgroundColor = Color.white;

            if (_editMode)
            {
                EditorGUILayout.HelpBox(
                    "• Drag green handles to move points\n" +
                    "• Click on an edge (blue dot) to insert a point\n" +
                    "• Right-click a point to delete it",
                    MessageType.Info);
            }

            EditorGUILayout.Space(6);
            DrawDefaultInspector();
        }

        // -----------------------------------------------------------------------
        // Scene GUI
        // -----------------------------------------------------------------------
        private void OnSceneGUI()
        {
            _area = (PolygonSpawnArea)target;
            if (_area._points == null || _area._points.Count == 0) return;

            DrawPolygonOverlay();

            if (!_editMode) return;

            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));

            DetectHover();
            DrawEdgeInsertPreview();
            DrawPointHandles();
            HandleInsertOnClick();
            HandleDeleteOnRightClick();

            // Repaint continuously while in edit mode so hover highlights update
            if (Event.current.type == EventType.MouseMove)
                SceneView.RepaintAll();
        }

        // -----------------------------------------------------------------------
        // Filled polygon overlay
        // -----------------------------------------------------------------------
        private void DrawPolygonOverlay()
        {
            List<Vector3> wp = _area.GetWorldPoints();
            if (wp.Count < 3) return;

            float y = _area.transform.position.y;

            // Outline
            Handles.color = _colOutline;
            for (int i = 0; i < wp.Count; i++)
            {
                Vector3 a = Flat(wp[i], y);
                Vector3 b = Flat(wp[(i + 1) % wp.Count], y);
                Handles.DrawLine(a, b, 2f);
            }

            // Fill (fan from centroid)
            Handles.color = _colFill;
            Vector3 centroid = Vector3.zero;
            foreach (Vector3 p in wp) centroid += p;
            centroid = Flat(centroid / wp.Count, y);

            for (int i = 0; i < wp.Count; i++)
            {
                Vector3 a = Flat(wp[i], y);
                Vector3 b = Flat(wp[(i + 1) % wp.Count], y);
                Handles.DrawAAConvexPolygon(centroid, a, b);
            }
        }

        // -----------------------------------------------------------------------
        // Detect which point or segment is closest to mouse
        // -----------------------------------------------------------------------
        private void DetectHover()
        {
            _hoveredPt = -1;
            _hoveredSeg = -1;

            float y = _area.transform.position.y;
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

            // Intersect ray with Y plane
            if (Mathf.Abs(ray.direction.y) < 0.0001f) return;
            float t = (y - ray.origin.y) / ray.direction.y;
            if (t < 0) return;
            Vector3 mouse3D = ray.origin + ray.direction * t;

            // Check proximity to existing points first
            for (int i = 0; i < _area._points.Count; i++)
            {
                Vector3 wp = _area.transform.TransformPoint(_area._points[i]);
                if (Vector3.SqrMagnitude(new Vector3(mouse3D.x - wp.x, 0, mouse3D.z - wp.z))
                    < _deleteRadius * _deleteRadius)
                {
                    _hoveredPt = i;
                    return;
                }
            }

            // Check proximity to segments
            List<Vector3> wps = _area.GetWorldPoints();
            float bestSq = _insertDistSq;
            for (int i = 0; i < wps.Count; i++)
            {
                Vector3 a = Flat(wps[i], y);
                Vector3 b = Flat(wps[(i + 1) % wps.Count], y);
                float d = PointToSegmentDistSq(mouse3D, a, b);
                if (d < bestSq)
                {
                    bestSq = d;
                    _hoveredSeg = i;
                }
            }
        }

        // -----------------------------------------------------------------------
        // Blue dot on edge preview
        // -----------------------------------------------------------------------
        private void DrawEdgeInsertPreview()
        {
            if (_hoveredSeg < 0 || _hoveredPt >= 0) return;

            float y = _area.transform.position.y;
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            if (Mathf.Abs(ray.direction.y) < 0.0001f) return;
            float t = (y - ray.origin.y) / ray.direction.y;
            Vector3 mouse3D = ray.origin + ray.direction * t;

            List<Vector3> wp = _area.GetWorldPoints();
            Vector3 a = Flat(wp[_hoveredSeg], y);
            Vector3 b = Flat(wp[(_hoveredSeg + 1) % wp.Count], y);
            Vector3 closest = ClosestPointOnSegment(mouse3D, a, b);

            Handles.color = _colInsert;
            Handles.DrawSolidDisc(closest, Vector3.up, _pointRadius * 0.8f);
            Handles.Label(closest + Vector3.up * 0.5f,
                          " + insert", EditorStyles.boldLabel);
        }

        // -----------------------------------------------------------------------
        // Move handles for existing points
        // -----------------------------------------------------------------------
        private void DrawPointHandles()
        {
            Undo.RecordObject(_area, "Move Polygon Point");

            float y = _area.transform.position.y;

            for (int i = 0; i < _area._points.Count; i++)
            {
                Vector3 worldPos = _area.transform.TransformPoint(_area._points[i]);
                worldPos.y = y;

                bool isHovered = (i == _hoveredPt);
                Handles.color = isHovered ? _colHandleHover : _colHandle;

                // Label
                Handles.Label(worldPos + Vector3.up * 0.6f,
                              $" {i}", EditorStyles.miniLabel);

                // Draw disc
                Handles.DrawSolidDisc(worldPos, Vector3.up, _pointRadius);

                // Drag handle (free-move on XZ)
                EditorGUI.BeginChangeCheck();
                Vector3 newWorld = Handles.FreeMoveHandle(
                    worldPos,
                    _pointRadius * 0.9f,
                    Vector3.zero,
                    Handles.CircleHandleCap);

                if (EditorGUI.EndChangeCheck())
                {
                    newWorld.y = y;   // lock Y
                    _area._points[i] = _area.transform.InverseTransformPoint(newWorld);
                    EditorUtility.SetDirty(_area);
                }
            }
        }

        // -----------------------------------------------------------------------
        // Left-click on edge → insert point
        // -----------------------------------------------------------------------
        private void HandleInsertOnClick()
        {
            if (_hoveredSeg < 0 || _hoveredPt >= 0) return;
            if (Event.current.type != EventType.MouseDown) return;
            if (Event.current.button != 0) return;

            float y = _area.transform.position.y;
            Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            if (Mathf.Abs(ray.direction.y) < 0.0001f) return;
            float t = (y - ray.origin.y) / ray.direction.y;
            Vector3 mouse3D = ray.origin + ray.direction * t;

            List<Vector3> wp = _area.GetWorldPoints();
            Vector3 a = Flat(wp[_hoveredSeg], y);
            Vector3 b = Flat(wp[(_hoveredSeg + 1) % wp.Count], y);
            Vector3 insertWorld = ClosestPointOnSegment(mouse3D, a, b);

            Undo.RecordObject(_area, "Insert Polygon Point");
            _area._points.Insert(_hoveredSeg + 1,
                                _area.transform.InverseTransformPoint(insertWorld));
            EditorUtility.SetDirty(_area);

            Event.current.Use();
        }

        // -----------------------------------------------------------------------
        // Right-click on point → delete
        // -----------------------------------------------------------------------
        private void HandleDeleteOnRightClick()
        {
            if (_hoveredPt < 0) return;
            if (Event.current.type != EventType.MouseDown) return;
            if (Event.current.button != 1) return;
            if (_area._points.Count <= 3)
            {
                Debug.LogWarning("PolygonSpawnArea: cannot delete — minimum 3 points.");
                Event.current.Use();
                return;
            }

            Undo.RecordObject(_area, "Delete Polygon Point");
            _area._points.RemoveAt(_hoveredPt);
            EditorUtility.SetDirty(_area);
            _hoveredPt = -1;

            Event.current.Use();
        }

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------
        private static Vector3 Flat(Vector3 v, float y) =>
            new Vector3(v.x, y, v.z);

        private static float PointToSegmentDistSq(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude);
            Vector3 closest = a + t * ab;
            return Vector3.SqrMagnitude(new Vector3(p.x - closest.x, 0, p.z - closest.z));
        }

        private static Vector3 ClosestPointOnSegment(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / ab.sqrMagnitude);
            return a + t * ab;
        }
    }
}
