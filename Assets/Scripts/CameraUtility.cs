using UnityEngine;

public static class CameraUtility {
    static readonly Vector3[] cubeCornerOffsets = {
        new Vector3 (1, 1, 1),
        new Vector3 (-1, 1, 1),
        new Vector3 (-1, -1, 1),
        new Vector3 (-1, -1, -1),
        new Vector3 (-1, 1, -1),
        new Vector3 (1, -1, -1),
        new Vector3 (1, 1, -1),
        new Vector3 (1, -1, 1),
    };

    // http://wiki.unity3d.com/index.php/IsVisibleFrom
    public static bool VisibleFromCamera (Renderer renderer, Camera camera) {
        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes (camera);
        return GeometryUtility.TestPlanesAABB (frustumPlanes, renderer.bounds);
    }

    public static bool BoundsOverlap (MeshFilter nearObject, MeshFilter farObject, Camera camera) {

        var near = GetScreenRectFromBounds (nearObject, camera);
        var far = GetScreenRectFromBounds (farObject, camera);

        // ensure far object is indeed further away than near object
        if (far.zMax > near.zMin) {
            // Doesn't overlap on x axis
            if (far.xMax < near.xMin || far.xMin > near.xMax) {
                return false;
            }
            // Doesn't overlap on y axis
            if (far.yMax < near.yMin || far.yMin > near.yMax) {
                return false;
            }
            // Overlaps
            return true;
        }
        return false;
    }

    // With thanks to http://www.turiyaware.com/a-solution-to-unitys-camera-worldtoscreenpoint-causing-ui-elements-to-display-when-object-is-behind-the-camera/
    public static MinMax3D GetScreenRectFromBounds (MeshFilter renderer, Camera mainCamera) {
        MinMax3D minMax = new MinMax3D (float.MaxValue, float.MinValue);

        Vector3[] screenBoundsExtents = new Vector3[8];
        var localBounds = renderer.sharedMesh.bounds;
        bool anyPointIsInFrontOfCamera = false;

        for (int i = 0; i < 8; i++) {
            Vector3 localSpaceCorner = localBounds.center + Vector3.Scale (localBounds.extents, cubeCornerOffsets[i]);
            Vector3 worldSpaceCorner = renderer.transform.TransformPoint (localSpaceCorner);
            Vector3 viewportSpaceCorner = mainCamera.WorldToViewportPoint (worldSpaceCorner);

            if (viewportSpaceCorner.z > 0) {
                anyPointIsInFrontOfCamera = true;
            } else {
                // If point is behind camera, it gets flipped to the opposite side
                // So clamp to opposite edge to correct for this
                viewportSpaceCorner.x = (viewportSpaceCorner.x <= 0.5f) ? 1 : 0;
                viewportSpaceCorner.y = (viewportSpaceCorner.y <= 0.5f) ? 1 : 0;
            }

            // Update bounds with new corner point
            minMax.AddPoint (viewportSpaceCorner);
        }

        // All points are behind camera so just return empty bounds
        if (!anyPointIsInFrontOfCamera) {
            return new MinMax3D ();
        }

        return minMax;
    }

    public struct MinMax3D {
        public float xMin;
        public float xMax;
        public float yMin;
        public float yMax;
        public float zMin;
        public float zMax;

        public MinMax3D (float min, float max) {
            this.xMin = min;
            this.xMax = max;
            this.yMin = min;
            this.yMax = max;
            this.zMin = min;
            this.zMax = max;
        }

        public void AddPoint (Vector3 point) {
            xMin = Mathf.Min (xMin, point.x);
            xMax = Mathf.Max (xMax, point.x);
            yMin = Mathf.Min (yMin, point.y);
            yMax = Mathf.Max (yMax, point.y);
            zMin = Mathf.Min (zMin, point.z);
            zMax = Mathf.Max (zMax, point.z);
        }
    }

    public static bool SegmentQuad(Vector3 p0, Vector3 p1, Transform quadT)
    {
        if (SegmentPlane(p0, p1, quadT.position, quadT.forward, out Vector3 intersect))
        {
            Vector3 v1 = quadT.position - quadT.right * quadT.localScale.x * 0.5f - quadT.up * quadT.localScale.y * 0.5f;
            Vector3 v2 = quadT.position + quadT.right * quadT.localScale.x * 0.5f - quadT.up * quadT.localScale.y * 0.5f;
            Vector3 v3 = quadT.position + quadT.right * quadT.localScale.x * 0.5f + quadT.up * quadT.localScale.y * 0.5f;
            Vector3 v4 = quadT.position - quadT.right * quadT.localScale.x * 0.5f + quadT.up * quadT.localScale.y * 0.5f;
            return IsPointInRectangle(intersect, v1, v2, v3, v4);
        }
        else
        {
            return false;
        }
    }

    public static bool SegmentPlane(Vector3 p0, Vector3 p1, Vector3 planeCenter, Vector3 planeNormal, out Vector3 intersectionPoint)
    {
        intersectionPoint = Vector3.zero;

        Vector3 segmentDirection = p1 - p0;
        Vector3 w = p0 - planeCenter;

        // The denominator (D) checks if the line is parallel to the plane.
        float D = Vector3.Dot(planeNormal, segmentDirection);
        // The numerator (N) is used to find the intersection parameter.
        float N = -Vector3.Dot(planeNormal, w);

        // Use an epsilon for floating point comparison to handle near-zero values.
        if (Mathf.Abs(D) < Mathf.Epsilon)
        {
            // The segment is parallel to the plane.
            // Counts as no intersection even if the segment lies entirly within the plane
            /*
            if (Mathf.Abs(N) < Mathf.Epsilon)
            {
                // The segment lies entirely within the plane.
                intersectionPoint = p0; // Or any point on the segment
                return true;
            }
            else
            {
                // The segment is parallel but not in the plane (no intersection).
                return false;
            }
            */
            return false;
        }

        // They are not parallel, calculate the intersection parameter (sI).
        // sI represents the position along the infinite line from p0 to p1.
        float sI = N / D;

        // Check if the intersection point lies within the *finite segment* (0 <= sI <= 1).
        if (sI >= 0 && sI <= 1)
        {
            // Intersection is on the segment. Calculate the exact point.
            intersectionPoint = p0 + sI * segmentDirection;
            return true;
        }
        else
        {
            // Intersection is outside the segment bounds (it crosses the infinite line, but not the segment).
            return false;
        }
    }

    // Helper function to check if a point is within the 3D rectangle
    public static bool IsPointInRectangle(Vector3 point, Vector3 v1, Vector3 v2, Vector3 v3, Vector3 v4)
    {
        // Calculate the rectangle's local axes
        Vector3 axis1 = v2 - v1;
        Vector3 axis2 = v4 - v1;

        // Project the intersection point onto these axes relative to v1
        Vector3 pointVec = point - v1;
        float dot1 = Vector3.Dot(pointVec, axis1);
        float dot2 = Vector3.Dot(pointVec, axis2);

        // Check if the projections are within the valid ranges
        // Range 0 to dot product of the axis with itself (squared length)
        return dot1 >= 0 && dot1 <= Vector3.Dot(axis1, axis1) && dot2 >= 0 && dot2 <= Vector3.Dot(axis2, axis2);
    }

}