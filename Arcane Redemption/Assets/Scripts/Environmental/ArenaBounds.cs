using UnityEngine;

/// <summary>
/// Defines and enforces arena boundaries for entities.
/// Follows Single Responsibility Principle - only handles spatial constraints.
/// </summary>
public class ArenaBounds : MonoBehaviour
{
    [Header("Boundary Settings")]
    [SerializeField] private BoundaryType boundaryType = BoundaryType.Circular;
    [SerializeField] private Vector3 center = Vector3.zero;
    [SerializeField] private float radius = 20f;
    [SerializeField] private Vector3 minBounds = new Vector3(-20f, 0f, -20f);
    [SerializeField] private Vector3 maxBounds = new Vector3(20f, 0f, 20f);

    [Header("Safety Margins")]
    [SerializeField] private float edgeWarningDistance = 2f;
    [SerializeField] private float hardBoundaryOffset = 0.5f;

    [Header("Visualization")]
    [SerializeField] private bool showBounds = true;
    [SerializeField] private Color boundsColor = Color.white;
    [SerializeField] private Color warningColor = Color.yellow;

    public Vector3 Center => center;
    public float Radius => radius;
    public BoundaryType Type => boundaryType;

    /// <summary>
    /// Clamps a position to within arena bounds.
    /// </summary>
    public Vector3 ClampPosition(Vector3 position)
    {
        return boundaryType == BoundaryType.Circular
            ? ClampToCircle(position)
            : ClampToRectangle(position);
    }

    /// <summary>
    /// Checks if a position is within safe bounds (with margin).
    /// </summary>
    public bool IsInSafeBounds(Vector3 position)
    {
        float safeRadius = radius - edgeWarningDistance;
        return boundaryType == BoundaryType.Circular
            ? IsWithinCircle(position, safeRadius)
            : IsWithinRectangle(position, edgeWarningDistance);
    }

    /// <summary>
    /// Gets direction vector pushing position towards safe area.
    /// Returns zero if already safe.
    /// </summary>
    public Vector3 GetSafePushDirection(Vector3 position)
    {
        if (IsInSafeBounds(position)) return Vector3.zero;

        if (boundaryType == BoundaryType.Circular)
        {
            Vector3 flatPos = new Vector3(position.x, center.y, position.z);
            Vector3 flatCenter = new Vector3(center.x, center.y, center.z);
            return (flatCenter - flatPos).normalized;
        }
        else
        {
            Vector3 pushDirection = Vector3.zero;

            if (position.x < minBounds.x + edgeWarningDistance)
                pushDirection.x = 1f;
            else if (position.x > maxBounds.x - edgeWarningDistance)
                pushDirection.x = -1f;

            if (position.z < minBounds.z + edgeWarningDistance)
                pushDirection.z = 1f;
            else if (position.z > maxBounds.z - edgeWarningDistance)
                pushDirection.z = -1f;

            return pushDirection.normalized;
        }
    }

    /// <summary>
    /// Gets normalized distance from edge (0 = at edge, 1 = at center).
    /// </summary>
    public float GetDistanceFromEdgeNormalized(Vector3 position)
    {
        if (boundaryType == BoundaryType.Circular)
        {
            Vector3 flatPos = new Vector3(position.x, center.y, position.z);
            Vector3 flatCenter = new Vector3(center.x, center.y, center.z);
            float distanceFromCenter = Vector3.Distance(flatPos, flatCenter);
            return 1f - Mathf.Clamp01(distanceFromCenter / radius);
        }
        else
        {
            float xDist = Mathf.Min(
                position.x - minBounds.x,
                maxBounds.x - position.x
            );
            float zDist = Mathf.Min(
                position.z - minBounds.z,
                maxBounds.z - position.z
            );
            float minDist = Mathf.Min(xDist, zDist);
            float maxPossibleDist = Mathf.Min(
                (maxBounds.x - minBounds.x) / 2f,
                (maxBounds.z - minBounds.z) / 2f
            );
            return Mathf.Clamp01(minDist / maxPossibleDist);
        }
    }

    private Vector3 ClampToCircle(Vector3 position)
    {
        Vector3 flatPos = new Vector3(position.x, center.y, position.z);
        Vector3 flatCenter = new Vector3(center.x, center.y, center.z);
        float distanceFromCenter = Vector3.Distance(flatPos, flatCenter);

        if (distanceFromCenter > radius - hardBoundaryOffset)
        {
            Vector3 directionToCenter = (flatCenter - flatPos).normalized;
            flatPos = flatCenter - directionToCenter * (radius - hardBoundaryOffset);
            position.x = flatPos.x;
            position.z = flatPos.z;
        }

        return position;
    }

    private Vector3 ClampToRectangle(Vector3 position)
    {
        position.x = Mathf.Clamp(position.x, minBounds.x + hardBoundaryOffset, maxBounds.x - hardBoundaryOffset);
        position.z = Mathf.Clamp(position.z, minBounds.z + hardBoundaryOffset, maxBounds.z - hardBoundaryOffset);
        return position;
    }

    private bool IsWithinCircle(Vector3 position, float checkRadius)
    {
        Vector3 flatPos = new Vector3(position.x, center.y, position.z);
        Vector3 flatCenter = new Vector3(center.x, center.y, center.z);
        return Vector3.Distance(flatPos, flatCenter) <= checkRadius;
    }

    private bool IsWithinRectangle(Vector3 position, float margin)
    {
        return position.x >= minBounds.x + margin &&
               position.x <= maxBounds.x - margin &&
               position.z >= minBounds.z + margin &&
               position.z <= maxBounds.z - margin;
    }

    private void OnDrawGizmos()
    {
        if (!showBounds) return;

        // Draw hard boundary
        Gizmos.color = boundsColor;
        if (boundaryType == BoundaryType.Circular)
        {
            DrawCircle(center, radius, 64);
        }
        else
        {
            DrawRectangle(minBounds, maxBounds);
        }

        // Draw warning zone
        Gizmos.color = warningColor;
        if (boundaryType == BoundaryType.Circular)
        {
            DrawCircle(center, radius - edgeWarningDistance, 64);
        }
        else
        {
            Vector3 warningMin = minBounds + Vector3.one * edgeWarningDistance;
            Vector3 warningMax = maxBounds - Vector3.one * edgeWarningDistance;
            warningMin.y = center.y;
            warningMax.y = center.y;
            DrawRectangle(warningMin, warningMax);
        }
    }

    private void DrawCircle(Vector3 circleCenter, float circleRadius, int segments)
    {
        float angleStep = 360f / segments;
        Vector3 prevPoint = circleCenter + new Vector3(circleRadius, 0f, 0f);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = circleCenter + new Vector3(Mathf.Cos(angle) * circleRadius, 0f, Mathf.Sin(angle) * circleRadius);
            Gizmos.DrawLine(prevPoint, newPoint);
            prevPoint = newPoint;
        }
    }

    private void DrawRectangle(Vector3 min, Vector3 max)
    {
        Vector3 corner1 = new Vector3(min.x, center.y, min.z);
        Vector3 corner2 = new Vector3(max.x, center.y, min.z);
        Vector3 corner3 = new Vector3(max.x, center.y, max.z);
        Vector3 corner4 = new Vector3(min.x, center.y, max.z);

        Gizmos.DrawLine(corner1, corner2);
        Gizmos.DrawLine(corner2, corner3);
        Gizmos.DrawLine(corner3, corner4);
        Gizmos.DrawLine(corner4, corner1);
    }

    [ContextMenu("Auto-Detect Arena Bounds")]
    private void AutoDetectBounds()
    {
        // Find all colliders in scene that might be the arena platform
        Collider[] colliders = FindObjectsByType<Collider>(FindObjectsSortMode.None);
        foreach (var col in colliders)
        {
            if (col.gameObject.name.ToLower().Contains("arena") ||
                col.gameObject.name.ToLower().Contains("platform"))
            {
                center = col.bounds.center;
                if (boundaryType == BoundaryType.Circular)
                {
                    radius = Mathf.Max(col.bounds.extents.x, col.bounds.extents.z) * 0.9f;
                }
                else
                {
                    minBounds = col.bounds.min;
                    maxBounds = col.bounds.max;
                }
                Debug.Log($"[ArenaBounds] Auto-detected bounds from {col.gameObject.name}");
                break;
            }
        }
    }
}

public enum BoundaryType
{
    Circular,
    Rectangular
}