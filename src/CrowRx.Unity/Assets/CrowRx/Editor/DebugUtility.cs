using UnityEngine;


namespace CrowRx.Editor
{
    public class Debug : UnityEngine.Debug
    {
        public enum DrawingPlane
        {
            XY,
            XZ,
            YZ
        }

        public static void DrawCircle(Vector3 position, Quaternion rotation, float radius, Color color, int segments, float duration = 0f)
        {
            // If either radius or number of segments are less or equal to 0, skip drawing
            if (radius <= 0.0f || segments <= 0)
            {
                return;
            }

            // Single segment of the circle covers (360 / number of segments) degrees
            float angleStep = (360.0f / segments);

            // Result is multiplied by Mathf.Deg2Rad constant which transforms degrees to radians
            // which are required by Unity's Mathf class trigonometry methods

            angleStep *= Mathf.Deg2Rad;

            // lineStart and lineEnd variables are declared outside of the following for loop
            Vector3 lineStart = Vector3.zero;
            Vector3 lineEnd = Vector3.zero;

            for (int i = 0; i < segments; i++)
            {
                // Line start is defined as starting angle of the current segment (i)
                lineStart.x = Mathf.Cos(angleStep * i);
                lineStart.y = Mathf.Sin(angleStep * i);
                lineStart.z = 0.0f;

                // Line end is defined by the angle of the next segment (i+1)
                lineEnd.x = Mathf.Cos(angleStep * (i + 1));
                lineEnd.y = Mathf.Sin(angleStep * (i + 1));
                lineEnd.z = 0.0f;

                // Results are multiplied so they match the desired radius
                lineStart *= radius;
                lineEnd *= radius;

                // Results are multiplied by the rotation quaternion to rotate them 
                // since this operation is not commutative, result needs to be
                // reassigned, instead of using multiplication assignment operator (*=)
                lineStart = rotation * lineStart;
                lineEnd = rotation * lineEnd;

                // Results are offset by the desired position/origin 
                lineStart += position;
                lineEnd += position;

                // Points are connected using DrawLine method and using the passed color
                DrawLine(lineStart, lineEnd, color, duration);
            }
        }

        public static void DrawArc(float startAngle, float endAngle,
            Vector3 position, Quaternion orientation, float radius,
            Color color, bool drawChord = false, bool drawSector = false,
            int arcSegments = 32, float duration = 0f)
        {
            float arcSpan = Mathf.DeltaAngle(startAngle, endAngle);

            // Since Mathf.DeltaAngle returns a signed angle of the shortest path between two angles, it 
            // is necessary to offset it by 360.0 degrees to get a positive value
            if (arcSpan <= 0)
            {
                arcSpan += 360.0f;
            }

            // angle step is calculated by dividing the arc span by number of approximation segments
            float angleStep = (arcSpan / arcSegments) * Mathf.Deg2Rad;
            float stepOffset = startAngle * Mathf.Deg2Rad;

            // stepStart, stepEnd, lineStart and lineEnd variables are declared outside of the following for loop
            float stepStart = 0.0f;
            float stepEnd = 0.0f;
            Vector3 lineStart = Vector3.zero;
            Vector3 lineEnd = Vector3.zero;

            // arcStart and arcEnd need to be stored to be able to draw segment chord
            Vector3 arcStart = Vector3.zero;
            Vector3 arcEnd = Vector3.zero;

            // arcOrigin represents an origin of a circle which defines the arc
            Vector3 arcOrigin = position;

            for (int i = 0; i < arcSegments; i++)
            {
                // Calculate approximation segment start and end, and offset them by start angle
                stepStart = angleStep * i + stepOffset;
                stepEnd = angleStep * (i + 1) + stepOffset;

                lineStart.x = Mathf.Cos(stepStart);
                lineStart.y = Mathf.Sin(stepStart);
                lineStart.z = 0.0f;

                lineEnd.x = Mathf.Cos(stepEnd);
                lineEnd.y = Mathf.Sin(stepEnd);
                lineEnd.z = 0.0f;

                // Results are multiplied so they match the desired radius
                lineStart *= radius;
                lineEnd *= radius;

                // Results are multiplied by the orientation quaternion to rotate them 
                // since this operation is not commutative, result needs to be
                // reassigned, instead of using multiplication assignment operator (*=)
                lineStart = orientation * lineStart;
                lineEnd = orientation * lineEnd;

                // Results are offset by the desired position/origin 
                lineStart += position;
                lineEnd += position;

                // If this is the first iteration, set the chordStart
                if (i == 0)
                {
                    arcStart = lineStart;
                }

                // If this is the last iteration, set the chordEnd
                if (i == arcSegments - 1)
                {
                    arcEnd = lineEnd;
                }

                DrawLine(lineStart, lineEnd, color, duration);
            }

            if (drawChord)
            {
                DrawLine(arcStart, arcEnd, color, duration);
            }

            if (drawSector)
            {
                DrawLine(arcStart, arcOrigin, color, duration);
                DrawLine(arcEnd, arcOrigin, color, duration);
            }
        }

        public static void DrawArcXY(float startAngle, float endAngle,
            Vector3 position, float radius, Color color, bool drawChord = false,
            bool drawSector = false, int arcSegments = 16, float duration = 0f)
        {
            DrawArc(startAngle, endAngle, position, Quaternion.identity, radius,
                color, drawChord, drawSector, arcSegments, duration);
        }

        public static void DrawArcXZ(float startAngle, float endAngle,
            Vector3 position, float radius, Color color, bool drawChord = false,
            bool drawSector = false, int arcSegments = 16, float duration = 0f)
        {
            DrawArc(startAngle, endAngle, position, Quaternion.Euler(0, -90, 0), radius,
                color, drawChord, drawSector, arcSegments, duration);
        }

        public static void DrawArcYZ(float startAngle, float endAngle,
            Vector3 position, float radius, Color color, bool drawChord = false,
            bool drawSector = false, int arcSegments = 16, float duration = 0f)
        {
            DrawArc(startAngle, endAngle, position, Quaternion.Euler(90, 0, 0), radius,
                color, drawChord, drawSector, arcSegments, duration);
        }

        public static void DrawArc(float startAngle, float endAngle,
            Vector3 position, DrawingPlane drawingPlane, float radius,
            Color color, bool drawChord = false, bool drawSector = false,
            int arcSegments = 16, float duration = 0f)
        {
            switch (drawingPlane)
            {
                case DrawingPlane.XY:
                    DrawArcXY(startAngle, endAngle, position, radius, color,
                        drawChord, drawSector, arcSegments, duration);
                    break;

                case DrawingPlane.XZ:
                    DrawArcXZ(startAngle, endAngle, position, radius, color,
                        drawChord, drawSector, arcSegments, duration);
                    break;

                case DrawingPlane.YZ:
                    DrawArcYZ(startAngle, endAngle, position, radius, color,
                        drawChord, drawSector, arcSegments, duration);
                    break;

                default:
                    DrawArcXY(startAngle, endAngle, position, radius, color,
                        drawChord, drawSector, arcSegments, duration);
                    break;
            }
        }

        public static void DrawTriangle(Vector3 pointA, Vector3 pointB, Vector3 pointC, Color color, float duration = 0f)
        {
            // Connect pointA and pointB
            DrawLine(pointA, pointB, color, duration);

            // Connect pointB and pointC
            DrawLine(pointB, pointC, color, duration);

            // Connect pointC and pointA
            DrawLine(pointC, pointA, color, duration);
        }

        public static void DrawTriangle(Vector3 pointA, Vector3 pointB, Vector3 pointC, Vector3 offset, Quaternion orientation, Color color, float duration = 0f)
        {
            pointA = offset + orientation * pointA;
            pointB = offset + orientation * pointB;
            pointC = offset + orientation * pointC;

            DrawTriangle(pointA, pointB, pointC, color, duration);
        }

        public static void DrawTriangle(Vector3 origin, Quaternion orientation, float baseLength, float height, Color color, float duration = 0f)
        {
            Vector3 pointA = Vector3.right * baseLength * 0.5f;
            Vector3 pointC = Vector3.left * baseLength * 0.5f;
            Vector3 pointB = Vector3.up * height;

            DrawTriangle(pointA, pointB, pointC, origin, orientation, color, duration);
        }

        public static void DrawTriangle(float length, Vector3 center, Quaternion orientation, Color color, float duration = 0f)
        {
            float radius = length / Mathf.Cos(30.0f * Mathf.Deg2Rad) * 0.5f;
            Vector3 pointA = new Vector3(Mathf.Cos(330.0f * Mathf.Deg2Rad), Mathf.Sin(330.0f * Mathf.Deg2Rad), 0.0f) * radius;
            Vector3 pointB = new Vector3(Mathf.Cos(90.0f * Mathf.Deg2Rad), Mathf.Sin(90.0f * Mathf.Deg2Rad), 0.0f) * radius;
            Vector3 pointC = new Vector3(Mathf.Cos(210.0f * Mathf.Deg2Rad), Mathf.Sin(210.0f * Mathf.Deg2Rad), 0.0f) * radius;

            DrawTriangle(pointA, pointB, pointC, center, orientation, color, duration);
        }

        public static void DrawRectangle(Vector3 position, Vector2 extent, Color color, float duration = 0f)
        {
            Vector3 rightOffset = Vector3.right * extent.x * 0.5f;
            Vector3 upOffset = Vector3.up * extent.y * 0.5f;

            Vector3 offsetA = rightOffset + upOffset;
            Vector3 offsetB = -rightOffset + upOffset;
            Vector3 offsetC = -rightOffset - upOffset;
            Vector3 offsetD = rightOffset - upOffset;

            DrawLine(position + offsetA, position + offsetB, color, duration);
            DrawLine(position + offsetB, position + offsetC, color, duration);
            DrawLine(position + offsetC, position + offsetD, color, duration);
            DrawLine(position + offsetD, position + offsetA, color, duration);
        }

        public static void DrawQuad(Vector3 pointA, Vector3 pointB, Vector3 pointC, Vector3 pointD, Color color, float duration = 0f)
        {
            // Draw lines between the points
            DrawLine(pointA, pointB, color, duration);
            DrawLine(pointB, pointC, color, duration);
            DrawLine(pointC, pointD, color, duration);
            DrawLine(pointD, pointA, color, duration);
        }

        public static void DrawRectangle(Vector3 position, Quaternion orientation, Vector2 extent, Color color, float duration = 0f)
        {
            Vector3 rightOffset = Vector3.right * extent.x * 0.5f;
            Vector3 upOffset = Vector3.up * extent.y * 0.5f;

            Vector3 offsetA = orientation * (rightOffset + upOffset);
            Vector3 offsetB = orientation * (-rightOffset + upOffset);
            Vector3 offsetC = orientation * (-rightOffset - upOffset);
            Vector3 offsetD = orientation * (rightOffset - upOffset);

            DrawQuad(position + offsetA,
                position + offsetB,
                position + offsetC,
                position + offsetD,
                color,
                duration);
        }

        public static void DrawRectangle(Vector2 point1, Vector2 point2, Vector3 origin, Quaternion orientation, Color color, float duration = 0f)
        {
            // Calculate extent as a distance between point1 and point2
            float extentX = Mathf.Abs(point1.x - point2.x);
            float extentY = Mathf.Abs(point1.y - point2.y);

            // Calculate rotated axes
            Vector3 rotatedRight = orientation * Vector3.right;
            Vector3 rotatedUp = orientation * Vector3.up;

            // Calculate each rectangle point
            Vector3 pointA = origin + rotatedRight * point1.x + rotatedUp * point1.y;
            Vector3 pointB = pointA + rotatedRight * extentX;
            Vector3 pointC = pointB + rotatedUp * extentY;
            Vector3 pointD = pointA + rotatedUp * extentY;

            DrawQuad(pointA, pointB, pointC, pointD, color, duration);
        }

        public static void DrawSphere(Vector3 position, Quaternion orientation, float radius, Color color, int segments = 4, float duration = 0f)
        {
            if (segments < 2)
            {
                segments = 2;
            }

            int doubleSegments = segments * 2;

            // Draw meridians

            float meridianStep = 180.0f / segments;

            for (int i = 0; i < segments; i++)
            {
                DrawCircle(position, orientation * Quaternion.Euler(0, meridianStep * i, 0), radius, color, doubleSegments, duration);
            }

            // Draw parallels

            Vector3 verticalOffset = Vector3.zero;
            float parallelAngleStep = Mathf.PI / segments;
            float stepRadius = 0.0f;
            float stepAngle = 0.0f;

            for (int i = 1; i < segments; i++)
            {
                stepAngle = parallelAngleStep * i;
                verticalOffset = (orientation * Vector3.up) * Mathf.Cos(stepAngle) * radius;
                stepRadius = Mathf.Sin(stepAngle) * radius;

                DrawCircle(position + verticalOffset, orientation * Quaternion.Euler(90.0f, 0, 0), stepRadius, color, doubleSegments, duration);
            }
        }

        public static void DrawBox(Vector3 position, Quaternion orientation, Vector3 size, Color color, float duration = 0f)
        {
            Vector3 offsetX = orientation * Vector3.right * size.x * 0.5f;
            Vector3 offsetY = orientation * Vector3.up * size.y * 0.5f;
            Vector3 offsetZ = orientation * Vector3.forward * size.z * 0.5f;

            Vector3 pointA = -offsetX + offsetY;
            Vector3 pointB = offsetX + offsetY;
            Vector3 pointC = offsetX - offsetY;
            Vector3 pointD = -offsetX - offsetY;

            DrawRectangle(position - offsetZ, orientation, new Vector2(size.x, size.y), color, duration);
            DrawRectangle(position + offsetZ, orientation, new Vector2(size.x, size.y), color, duration);

            DrawLine(pointA - offsetZ, pointA + offsetZ, color, duration);
            DrawLine(pointB - offsetZ, pointB + offsetZ, color, duration);
            DrawLine(pointC - offsetZ, pointC + offsetZ, color, duration);
            DrawLine(pointD - offsetZ, pointD + offsetZ, color, duration);
        }

        public static void DrawCube(Vector3 position, Quaternion orientation, float size, Color color, float duration = 0f)
        {
            DrawBox(position, orientation, Vector3.one * size, color, duration);
        }

        public static void DrawCylinder(Vector3 position, Quaternion orientation, float height, float radius, Color color, bool drawFromBase = true, float duration = 0f)
        {
            Vector3 localUp = orientation * Vector3.up;
            Vector3 localRight = orientation * Vector3.right;
            Vector3 localForward = orientation * Vector3.forward;

            Vector3 basePositionOffset = drawFromBase ? Vector3.zero : (localUp * height * 0.5f);
            Vector3 basePosition = position - basePositionOffset;
            Vector3 topPosition = basePosition + localUp * height;

            Quaternion circleOrientation = orientation * Quaternion.Euler(90, 0, 0);

            Vector3 pointA = basePosition + localRight * radius;
            Vector3 pointB = basePosition + localForward * radius;
            Vector3 pointC = basePosition - localRight * radius;
            Vector3 pointD = basePosition - localForward * radius;

            DrawRay(pointA, localUp * height, color, duration);
            DrawRay(pointB, localUp * height, color, duration);
            DrawRay(pointC, localUp * height, color, duration);
            DrawRay(pointD, localUp * height, color, duration);

            DrawCircle(basePosition, circleOrientation, radius, color, 32, duration);
            DrawCircle(topPosition, circleOrientation, radius, color, 32, duration);
        }

        public static void DrawCapsule(Vector3 position, Quaternion orientation, float height, float radius, Color color, bool drawFromBase, float duration = 0f)
        {
            // Clamp the radius to a half of the capsule's height
            radius = Mathf.Clamp(radius, 0, height * 0.5f);
            Vector3 localUp = orientation * Vector3.up;
            Quaternion arcOrientation = orientation * Quaternion.Euler(0, 90, 0);

            Vector3 basePositionOffset = drawFromBase ? Vector3.zero : (localUp * height * 0.5f);
            Vector3 baseArcPosition = position + localUp * radius - basePositionOffset;
            DrawArc(180, 360, baseArcPosition, orientation, radius, color, duration: duration);
            DrawArc(180, 360, baseArcPosition, arcOrientation, radius, color, duration: duration);

            float cylinderHeight = height - radius * 2.0f;
            DrawCylinder(baseArcPosition, orientation, cylinderHeight, radius, color, true, duration);

            Vector3 topArcPosition = baseArcPosition + localUp * cylinderHeight;

            DrawArc(0, 180, topArcPosition, orientation, radius, color, duration: duration);
            DrawArc(0, 180, topArcPosition, arcOrientation, radius, color, duration: duration);
        }
    }
}