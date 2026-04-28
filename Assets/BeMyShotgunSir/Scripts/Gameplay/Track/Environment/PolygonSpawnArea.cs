using UnityEngine;
using System.Collections.Generic;

namespace BeMyShotgunSir.Scripts.Gameplay.Track.Environment
{
    public class PolygonSpawnArea : MonoBehaviour
    {
        [SerializeField] public List<Vector3> _points = new List<Vector3>()
        {
            new Vector3(-5, 0, -5),
            new Vector3(5, 0, -5),
            new Vector3(5, 0, 5),
            new Vector3(-5, 0, 5),
        };
        [SerializeField] public Color _gizmoColor = new Color(0.0f, 1.0f, 0.4f, 0.25f);
        [SerializeField] public Color _gizmoOutlineColor = new Color(0.0f, 1.0f, 0.4f, 1.00f);

        private List<Vector3> _worldPoints;

        public List<Vector3> GetWorldPoints()
        {
            var world = new List<Vector3>(_points.Count);
            foreach (var p in _points)
            {
                world.Add(transform.TransformPoint(p));
            }
            return world;
        }

        public void InvalidateWorldPointsCache()
        {
            _worldPoints = null;
        }

        public Vector3 GetDirectionToClosestEdge(Vector3 point)
        {
            if (_worldPoints == null || _worldPoints.Count < 2) return Vector3.forward;

            float minDist = float.MaxValue;
            Vector3 closestPoint = point;
            int n = _worldPoints.Count;

            for (int i = 0; i < n; i++)
            {
                Vector3 a = _worldPoints[i];
                Vector3 b = _worldPoints[(i + 1) % n];

                float abx = b.x - a.x, abz = b.z - a.z;
                float len2 = abx * abx + abz * abz;

                float t = Mathf.Clamp01(((point.x - a.x) * abx + (point.z - a.z) * abz) / len2);
                Vector3 closestOnSegment = new Vector3(a.x + t * abx, point.y, a.z + t * abz);

                float dist = Vector3.Distance(point, closestOnSegment);
                if (dist < minDist)
                {
                    minDist = dist;
                    closestPoint = closestOnSegment;
                }
            }

            Vector3 dir = (closestPoint - point).normalized;
            return dir != Vector3.zero ? dir : Vector3.forward;
        }

        public Bounds ComputePolygonBounds()
        {
            if (_worldPoints == null || _worldPoints.Count == 0)
            {
                _worldPoints = GetWorldPoints();
            }
            Bounds b = new Bounds(new Vector3(_worldPoints[0].x, 0f, _worldPoints[0].z),
                Vector3.zero);
            foreach (var p in _worldPoints)
                b.Encapsulate(new Vector3(p.x, 0f, p.z));
            return b;
        }

        public bool IsBoundsFullyInsidePolygon(Bounds b, float offset = 0)
        {
            Vector3 min = b.min;
            Vector3 max = b.max;

            Vector3[] corners = new Vector3[]
            {
                new Vector3(min.x, 0f, min.z),
                new Vector3(max.x, 0f, min.z),
                new Vector3(max.x, 0f, max.z),
                new Vector3(min.x, 0f, max.z),
            };

            foreach (var corner in corners)
            {
                if (!IsPointInsidePolygon(corner)) return false;
                if (DistanceToPolygonEdge(corner) < offset) return false;
            }

            Vector3[][] boxEdges = new Vector3[][]
            {
                new[] { corners[0], corners[1] },
                new[] { corners[1], corners[2] },
                new[] { corners[2], corners[3] },
                new[] { corners[3], corners[0] },
            };

            int n = _worldPoints.Count;
            for (int i = 0; i < n; i++)
            {
                Vector3 polyA = _worldPoints[i];
                Vector3 polyB = _worldPoints[(i + 1) % n];

                foreach (var edge in boxEdges)
                {
                    if (SegmentsIntersect2D(edge[0], edge[1], polyA, polyB))
                        return false;
                }
            }

            return true;
        }

        //Note this is not the actual center of the polygon but the center of its bounding box.
        //It works well enough for ItemSpawnArea but if you need the center of a spawnArea for a generic purpose is going to fuck you up
        public Vector3 GetPolygonBoundsCenter()
        {
            return ComputePolygonBounds().center;
        }

        private bool SegmentsIntersect2D(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4)
        {
            float d1X = p2.x - p1.x, d1Z = p2.z - p1.z;
            float d2X = p4.x - p3.x, d2Z = p4.z - p3.z;

            float denom = d1X * d2Z - d1Z * d2X;
            if (Mathf.Abs(denom) < 1e-10f) return false; // parallel

            float t = ((p3.x - p1.x) * d2Z - (p3.z - p1.z) * d2X) / denom;
            float u = ((p3.x - p1.x) * d1Z - (p3.z - p1.z) * d1X) / denom;

            return t >= 0f && t <= 1f && u >= 0f && u <= 1f;
        }

        private bool IsPointInsidePolygon(Vector3 point)
        {
            int n = _worldPoints.Count;
            bool inside = false;
            float px = point.x, pz = point.z;

            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                float xi = _worldPoints[i].x, zi = _worldPoints[i].z;
                float xj = _worldPoints[j].x, zj = _worldPoints[j].z;

                bool intersect = ((zi > pz) != (zj > pz)) &&
                    (px < (xj - xi) * (pz - zi) / (zj - zi) + xi);
                if (intersect)
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        private float DistanceToPolygonEdge(Vector3 point)
        {
            float minDist = float.MaxValue;
            int n = _worldPoints.Count;

            for (int i = 0; i < n; i++)
            {
                Vector3 a = _worldPoints[i];
                Vector3 b = _worldPoints[(i + 1) % n];

                float dist = PointToSegmentDistance2D(point, a, b);
                if (dist < minDist) minDist = dist;
            }
            return minDist;
        }

        private float PointToSegmentDistance2D(Vector3 p, Vector3 a, Vector3 b)
        {
            float abx = b.x - a.x, abz = b.z - a.z;
            float len2 = abx * abx + abz * abz;

            if (len2 < 1e-10f)
                return Vector2.Distance(new Vector2(p.x, p.z), new Vector2(a.x, a.z));

            float t = Mathf.Clamp01(((p.x - a.x) * abx + (p.z - a.z) * abz) / len2);
            float closestX = a.x + t * abx;
            float closestZ = a.z + t * abz;

            float dx = p.x - closestX, dz = p.z - closestZ;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private void OnDrawGizmos()
        {
            if (_points == null || _points.Count < 2)
            {
                Debug.LogError("PolygonSpawnArea should have at least 3 point!");
                return;
            }

            float y = transform.position.y;
            Gizmos.color = _gizmoColor;
            Vector3 centroid = Vector3.zero;
            //todo use cached world points
            var wp = GetWorldPoints();
            foreach (var p in wp)
            {
                centroid += p;
            }
            centroid /= wp.Count;
            centroid.y = y;

            for (int i = 0; i < wp.Count; i++)
            {
                Vector3 a = new Vector3(wp[i].x, y, wp[i].z);
                Vector3 b = new Vector3(wp[(i + 1) % wp.Count].x, y, wp[(i + 1) % wp.Count].z);
                Gizmos.DrawLine(centroid, a);
                Gizmos.DrawLine(centroid, b);
            }

            Gizmos.color = _gizmoOutlineColor;
            for (int i = 0; i < wp.Count; i++)
            {
                Vector3 a = new Vector3(wp[i].x, y, wp[i].z);
                Vector3 b = new Vector3(wp[(i + 1) % wp.Count].x, y, wp[(i + 1) % wp.Count].z);
                Gizmos.DrawLine(a, b);
                Gizmos.DrawSphere(a, 0.18f);
            }
        }
    }
}
