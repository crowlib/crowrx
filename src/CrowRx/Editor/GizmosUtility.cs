using System;
using UnityEngine;
using UnityEditor;


namespace CrowRx.Editor
{
    public static class GizmosUtility
    {
        private static readonly Vector4[] s_unitCube =
        {
            new(-0.5f, 0.5f, -0.5f, 1),
            new(0.5f, 0.5f, -0.5f, 1),
            new(0.5f, -0.5f, -0.5f, 1),
            new(-0.5f, -0.5f, -0.5f, 1),

            new(-0.5f, 0.5f, 0.5f, 1),
            new(0.5f, 0.5f, 0.5f, 1),
            new(0.5f, -0.5f, 0.5f, 1),
            new(-0.5f, -0.5f, 0.5f, 1)
        };

        private static readonly Vector4[] s_unitSquare =
        {
            new(-0.5f, 0.5f, 0, 1),
            new(0.5f, 0.5f, 0, 1),
            new(0.5f, -0.5f, 0, 1),
            new(-0.5f, -0.5f, 0, 1),
        };

        private static readonly Vector4[] s_unitSphere = MakeUnitSphere(16);


        public static void DrawWireCapsule(Vector3 point1, Vector3 point2, float radius)
        {
            Vector3 upOffset = point2 - point1;
            Vector3 up = upOffset.Equals(default) ? Vector3.up : upOffset.normalized;
            Quaternion orientation = Quaternion.FromToRotation(Vector3.up, up);
            Vector3 forward = orientation * Vector3.forward;
            Vector3 right = orientation * Vector3.right;

            using (new Handles.DrawingScope(Gizmos.color, Gizmos.matrix))
            {
                // z axis
                Handles.DrawWireArc(point2, forward, right, 180, radius);
                Handles.DrawWireArc(point1, forward, right, -180, radius);
                Handles.DrawLine(point1 + right * radius, point2 + right * radius);
                Handles.DrawLine(point1 - right * radius, point2 - right * radius);

                // x axis
                Handles.DrawWireArc(point2, right, forward, -180, radius);
                Handles.DrawWireArc(point1, right, forward, 180, radius);
                Handles.DrawLine(point1 + forward * radius, point2 + forward * radius);
                Handles.DrawLine(point1 - forward * radius, point2 - forward * radius);

                // y axis
                Handles.DrawWireDisc(point2, up, radius);
                Handles.DrawWireDisc(point1, up, radius);
            }
        }

        public static void DrawBox(Vector4 pos, Vector3 size, Color color)
        {
            Vector4[] v = s_unitCube;
            Vector4 sz = new(size.x, size.y, size.z, 1);

            for (int i = 0; i < 4; i++)
            {
                Vector4 s = pos + Vector4.Scale(v[i], sz);
                Vector4 e = pos + Vector4.Scale(v[(i + 1) % 4], sz);
                Debug.DrawLine(s, e, color);
            }

            for (int i = 0; i < 4; i++)
            {
                Vector4 s = pos + Vector4.Scale(v[4 + i], sz);
                Vector4 e = pos + Vector4.Scale(v[4 + ((i + 1) % 4)], sz);
                Debug.DrawLine(s, e, color);
            }

            for (int i = 0; i < 4; i++)
            {
                Vector4 s = pos + Vector4.Scale(v[i], sz);
                Vector4 e = pos + Vector4.Scale(v[i + 4], sz);
                Debug.DrawLine(s, e, color);
            }
        }

        public static void DrawBox(Matrix4x4 transform, Color color)
        {
            Vector4[] v = s_unitCube;

            for (int i = 0; i < 4; i++)
            {
                Vector4 s = transform * v[i];
                Vector4 e = transform * v[(i + 1) % 4];

                Debug.DrawLine(s, e, color);
            }

            for (int i = 0; i < 4; i++)
            {
                Vector4 s = transform * v[4 + i];
                Vector4 e = transform * v[4 + ((i + 1) % 4)];

                Debug.DrawLine(s, e, color);
            }

            for (int i = 0; i < 4; i++)
            {
                Vector4 s = transform * v[i];
                Vector4 e = transform * v[i + 4];

                Debug.DrawLine(s, e, color);
            }
        }

        public static void DrawPoint(Vector4 pos, float scale, Color color)
        {
            Vector4 sX = pos + new Vector4(+scale, 0, 0);
            Vector4 eX = pos + new Vector4(-scale, 0, 0);
            Vector4 sY = pos + new Vector4(0, +scale, 0);
            Vector4 eY = pos + new Vector4(0, -scale, 0);
            Vector4 sZ = pos + new Vector4(0, 0, +scale);
            Vector4 eZ = pos + new Vector4(0, 0, -scale);

            Debug.DrawLine(sX, eX, color);
            Debug.DrawLine(sY, eY, color);
            Debug.DrawLine(sZ, eZ, color);
        }

        public static void DrawAxes(Vector4 pos, float scale = 1.0f)
        {
            Debug.DrawLine(pos, pos + new Vector4(scale, 0, 0), Color.red);
            Debug.DrawLine(pos, pos + new Vector4(0, scale, 0), Color.green);
            Debug.DrawLine(pos, pos + new Vector4(0, 0, scale), Color.blue);
        }

        public static void DrawAxes(Matrix4x4 transform, float scale = 1.0f)
        {
            Vector4 p = transform * new Vector4(0, 0, 0, 1);
            Vector4 x = transform * new Vector4(scale, 0, 0, 1);
            Vector4 y = transform * new Vector4(0, scale, 0, 1);
            Vector4 z = transform * new Vector4(0, 0, scale, 1);

            Debug.DrawLine(p, x, Color.red);
            Debug.DrawLine(p, y, Color.green);
            Debug.DrawLine(p, z, Color.blue);
        }

        public static void DrawQuad(Matrix4x4 transform, Color color)
        {
            Vector4[] v = s_unitSquare;

            for (int i = 0; i < 4; i++)
            {
                Vector4 s = transform * v[i];
                Vector4 e = transform * v[(i + 1) % 4];

                Debug.DrawLine(s, e, color);
            }
        }

        public static void DrawPlane(Plane plane, float scale, Color edgeColor, float normalScale, Color normalColor)
        {
            // Flip plane distance: Unity Plane distance is from plane to origin
            DrawPlane(new Vector4(plane.normal.x, plane.normal.y, plane.normal.z, -plane.distance), scale, edgeColor, normalScale, normalColor);
        }

        public static void DrawPlane(Vector4 plane, float scale, Color edgeColor, float normalScale, Color normalColor)
        {
            Vector3 n = Vector3.Normalize(plane);
            float d = plane.w;

            Vector3 u = Vector3.up;
            Vector3 r = Vector3.right;
            if (n == u)
            {
                u = r;
            }

            r = Vector3.Cross(n, u);
            u = Vector3.Cross(n, r);

            for (int i = 0; i < 4; i++)
            {
                Vector4 s = scale * s_unitSquare[i];
                Vector4 e = scale * s_unitSquare[(i + 1) % 4];
                s = s.x * r + s.y * u + n * d;
                e = e.x * r + e.y * u + n * d;
                Debug.DrawLine(s, e, edgeColor);
            }

            // Diagonals
            {
                Vector4 s = scale * s_unitSquare[0];
                Vector4 e = scale * s_unitSquare[2];
                s = s.x * r + s.y * u + n * d;
                e = e.x * r + e.y * u + n * d;
                Debug.DrawLine(s, e, edgeColor);
            }
            {
                Vector4 s = scale * s_unitSquare[1];
                Vector4 e = scale * s_unitSquare[3];
                s = s.x * r + s.y * u + n * d;
                e = e.x * r + e.y * u + n * d;
                Debug.DrawLine(s, e, edgeColor);
            }

            Debug.DrawLine(n * d, n * (d + 1 * normalScale), normalColor);
        }

        public static void DrawSphere(Vector4 pos, float radius, Color color, float duration = 1f)
        {
            Vector4[] v = s_unitSphere;

            int len = s_unitSphere.Length / 3;

            for (int i = 0; i < len; i++)
            {
                Vector4 sX = pos + radius * v[0 * len + i];
                Vector4 eX = pos + radius * v[0 * len + (i + 1) % len];
                Vector4 sY = pos + radius * v[1 * len + i];
                Vector4 eY = pos + radius * v[1 * len + (i + 1) % len];
                Vector4 sZ = pos + radius * v[2 * len + i];
                Vector4 eZ = pos + radius * v[2 * len + (i + 1) % len];

                Debug.DrawLine(sX, eX, color, duration);
                Debug.DrawLine(sY, eY, color, duration);
                Debug.DrawLine(sZ, eZ, color, duration);
            }
        }

        private static Vector4[] MakeUnitSphere(int len)
        {
            Debug.Assert(len > 2);

            Vector4[] v = new Vector4[len * 3];

            for (int i = 0; i < len; i++)
            {
                float f = i / (float)len;
                float c = Mathf.Cos(f * (float)(Math.PI * 2.0));
                float s = Mathf.Sin(f * (float)(Math.PI * 2.0));

                v[0 * len + i] = new Vector4(c, s, 0, 1);
                v[1 * len + i] = new Vector4(0, c, s, 1);
                v[2 * len + i] = new Vector4(s, 0, c, 1);
            }

            return v;
        }

        public static Vector3 DrawCollider(Collider colliderToDraw)
        {
            Vector3 center = colliderToDraw.bounds.center;

            switch (colliderToDraw)
            {
                case BoxCollider boxCollider:
                    center = boxCollider.center;
                    Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
                    break;
                case SphereCollider sphereCollider:
                    center = sphereCollider.center;
                    Gizmos.DrawWireSphere(sphereCollider.center, sphereCollider.radius);
                    break;
                case CapsuleCollider capsuleCollider:
                {
                    center = capsuleCollider.center;

                    Vector3 capsuleHalfDirection = Vector3.up * (capsuleCollider.height * 0.5f - capsuleCollider.radius);
                    Vector3 p1 = capsuleCollider.center - capsuleHalfDirection;
                    Vector3 p2 = capsuleCollider.center + capsuleHalfDirection;

                    DrawWireCapsule(p1, p2, capsuleCollider.radius);
                    break;
                }
                case MeshCollider meshCollider:
                    Gizmos.DrawWireMesh(meshCollider.sharedMesh);
                    break;
            }

            return center;
        }
    }
}