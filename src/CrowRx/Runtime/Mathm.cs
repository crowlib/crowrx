using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;
#if USING_ZLINQ
using ZLinq;

#else
using System.Linq;
#endif


namespace CrowRx.Utility
{
    public static class Mathm
    {
        // return: -180 ~ 180 degree (for unity)
        public static float GetEulerAngle(Vector2 p1, Vector2 p2)
        {
            Vector2 diff = p2 - p1;

            return Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        }

        public static float GetEulerAngleXZ(Vector3 p1, Vector3 p2)
        {
            Vector3 diff = p2 - p1;

            return Mathf.Atan2(diff.z, diff.x) * Mathf.Rad2Deg;
        }

        public static bool IsArrival(Vector3 movingDirection, Vector3 currentPosition, Vector3 destination)
        {
            Vector3 to = (destination - currentPosition).normalized;

            return Vector3.Dot(movingDirection, to) < 0.0f;
        }

        public static void Matrix4X4Lerp(ref Matrix4x4 result, Matrix4x4 org, Matrix4x4 target, float t)
        {
            for (int i = 0; i < 16; ++i)
            {
                result[i] = Mathf.Lerp(org[i], target[i], t);
            }
        }

        public static void Lerp(this Matrix4x4 org, Matrix4x4 target, float t) => Matrix4X4Lerp(ref org, org, target, t);

        public static float Cross(Vector2 a, Vector2 b) => Vector3.Cross(a, b).z;

        public static float Cross(Vector2 p, Vector2 a, Vector2 b) => Cross(a - p, b - p);

        private static bool Intersect(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
        {
            double ab = Cross(a, b, c) * Cross(a, b, d);
            double cd = Cross(c, d, a) * Cross(c, d, b);

            return ab <= 0 && cd <= 0;
        }

        public static bool Intersect(this Rect rect, Vector2 p0, Vector2 p1)
        {
            if (rect.Contains(p0) || rect.Contains(p1))
            {
                return true;
            }

            if (Mathf.Max(p0.x, p1.x) < rect.xMin || Mathf.Min(p0.x, p1.x) > rect.xMax ||
                Mathf.Max(p0.y, p1.y) < rect.yMin || Mathf.Min(p0.y, p1.y) > rect.yMax)
            {
                return false;
            }

            Vector2[] rectPoints = new Vector2[4];
            rectPoints[0] = rect.min;
            rectPoints[1] = new Vector2(rect.xMax, rect.yMin);
            rectPoints[2] = rect.max;
            rectPoints[3] = new Vector2(rect.xMin, rect.yMax);

            return
                Intersect(p0, p1, rectPoints[0], rectPoints[1]) ||
                Intersect(p0, p1, rectPoints[1], rectPoints[2]) ||
                Intersect(p0, p1, rectPoints[2], rectPoints[3]) ||
                Intersect(p0, p1, rectPoints[3], rectPoints[0]);
        }

        public static bool GetIntersectPosition(Vector3 source1, Vector3 source2, Vector3 target1, Vector3 target2, out Vector3 intersectPos)
        {
            intersectPos = Vector3.zero;

            float sourceX = source2.x - source1.x;
            float sourceY = source2.y - source1.y;
            float targetX = target2.x - target1.x;
            float targetY = target2.y - target1.y;

            float under = (targetY * sourceX) - (targetX * sourceY);

            if (under == 0)
            {
                float angle = Vector3.Angle(source2 - source1, target1 - source1);
                if (angle is > 0 and < 180)
                {
                    return false;
                }

                intersectPos = Vector3.Distance(source1, target1) > Vector3.Distance(source1, target2) ? target2 : target1;
            }
            else
            {
                float originalT = (targetX * (source1.y - target1.y)) - (targetY * (source1.x - target1.x));
                float originalS = (sourceX * (source1.y - target1.y)) - (sourceY * (source1.x - target1.x));

                float t = originalT / under;
                float s = originalS / under;

                if (t < 0 || t > 1 || s < 0 || s > 1)
                {
                    return false;
                }

                if (originalT == 0 && originalS == 0)
                {
                    return false;
                }

                intersectPos.x = source1.x + (t * sourceX);
                intersectPos.y = source1.y + (t * sourceY);
            }

            return true;
        }

        //2D 좌표용(X,Z)
        public static bool RectContains2D(Vector3[] p, Vector2 pos)
        {
            //crosses는 점q와 오른쪽 반직선과 다각형과의 교점의 개수
            int crosses = 0;

            for (int i = 0; i < p.Length; i++)
            {
                int j = (i + 1) % p.Length;
                //점 pos (p[i], p[j])의 y좌표 사이에 있음
                if ((p[i].z > pos.y) != (p[j].z > pos.y))
                {
                    //atX는 점 pos 지나는 수평선과 선분 (p[i], p[j])의 교점
                    double atX = (p[j].x - p[i].x) * (pos.y - p[i].z) / (p[j].z - p[i].z) + p[i].x;

                    //atX가 오른쪽 반직선과의 교점이 맞으면 교점의 개수를 증가시킨다.
                    if (pos.x < atX)
                    {
                        crosses++;
                    }
                }
            }

            return crosses % 2 > 0;
        }

        //3D 좌표용(X,Y,Z)
        public static bool RectContains3D(Vector3[] rect, Vector3 pos)
        {
            //crosses는 점q와 오른쪽 반직선과 다각형과의 교점의 개수
            int crosses = 0;

            for (int i = 0; i < rect.Length; i++)
            {
                int j = (i + 1) % rect.Length;
                //점 pos (p[i], p[j])의 y좌표 사이에 있음

                if ((rect[i].y <= pos.y && rect[j].y > pos.y) || // an upward crossing
                    (rect[i].y > pos.y && rect[j].y <= pos.y)) // a downward crossing
                {
                    //atX는 점 pos 지나는 수평선과 선분 (p[i], p[j])의 교점
                    double vt = (double)(pos.y - rect[i].y) / (rect[j].y - rect[i].y);

                    if (pos.x < rect[i].x + vt * (rect[j].x - rect[i].x)) // P.x < intersect
                    {
                        crosses++;
                    }
                }
            }

            return crosses % 2 > 0;
        }

        public static bool RectIntersect(Vector3[] rect, Vector2 p0, Vector2 p1)
        {
            if (p1.x < p0.x)
            {
                (p0, p1) = (p1, p0);
            }

            if (RectContains3D(rect, p0) || RectContains3D(rect, p1))
            {
                return true;
            }
#if USING_ZLINQ
            float rectXMin = rect.AsValueEnumerable().Min(p => p.x);
            float rectYMin = rect.AsValueEnumerable().Min(p => p.y);
            float rectXMax = rect.AsValueEnumerable().Max(p => p.x);
            float rectYMax = rect.AsValueEnumerable().Max(p => p.y);
#else
            float rectXMin = rect.Min(p => p.x);
            float rectYMin = rect.Min(p => p.y);
            float rectXMax = rect.Max(p => p.x);
            float rectYMax = rect.Max(p => p.y);
#endif
            if (Mathf.Max(p0.x, p1.x) < rectXMin || Mathf.Min(p0.x, p1.x) > rectXMax ||
                Mathf.Max(p0.y, p1.y) < rectYMin || Mathf.Min(p0.y, p1.y) > rectYMax)
            {
                return false;
            }

            return
                Intersect(p0, p1, rect[0], rect[1]) ||
                Intersect(p0, p1, rect[1], rect[2]) ||
                Intersect(p0, p1, rect[2], rect[3]) ||
                Intersect(p0, p1, rect[3], rect[0]);
        }

        /// <summary>
        /// This code is not framerate-independent:
        /// 
        /// target.position += velocity;
        /// velocity = Vector3.Lerp(velocity, Vector3.zero, Time.deltaTime * 9f);
        /// 
        /// But this code is:
        /// 
        /// target.position += NGUIMath.SpringDampen(ref velocity, 9f, Time.deltaTime);
        /// </summary>
        //@@by ps2 from NGUIMath
        public static Vector3 SpringDampen(ref Vector3 velocity, float strength, float deltaTime)
        {
            if (deltaTime > 1f)
            {
                deltaTime = 1f;
            }

            float dampeningFactor = 1f - strength * 0.001f;
            int ms = Mathf.RoundToInt(deltaTime * 1000f);
            float totalDampening = Mathf.Pow(dampeningFactor, ms);
            Vector3 vTotal = velocity * ((totalDampening - 1f) / Mathf.Log(dampeningFactor));
            velocity *= totalDampening;

            return vTotal * 0.06f;
        }

        /// <summary>
        /// Same as the Vector3 version, it's a framerate-independent Lerp.
        /// </summary>
        //@@by ps2 from NGUIMath
        public static Vector2 SpringDampen(ref Vector2 velocity, float strength, float deltaTime)
        {
            if (deltaTime > 1f)
            {
                deltaTime = 1f;
            }

            float dampeningFactor = 1f - strength * 0.001f;
            int ms = Mathf.RoundToInt(deltaTime * 1000f);
            float totalDampening = Mathf.Pow(dampeningFactor, ms);
            Vector2 vTotal = velocity * ((totalDampening - 1f) / Mathf.Log(dampeningFactor));
            velocity *= totalDampening;

            return vTotal * 0.06f;
        }

        // for float
        public static float SpringDampen(ref float velocity, float strength, float deltaTime)
        {
            if (deltaTime > 1f)
            {
                deltaTime = 1f;
            }

            float dampeningFactor = 1f - strength * 0.001f;
            int ms = Mathf.RoundToInt(deltaTime * 1000f);
            float totalDampening = Mathf.Pow(dampeningFactor, ms);
            float total = velocity * ((totalDampening - 1f) / Mathf.Log(dampeningFactor));
            velocity *= totalDampening;

            return total * 0.06f;
        }

        public static async UniTask LerpAsync(Action<float> setter, Func<float> getter, float startValue, float endValue, float lerpDuration, bool reset, bool ignoreTimeScale, CancellationToken cancellationToken)
        {
            if (lerpDuration <= 0f)
            {
                setter(endValue);

                return;
            }

            float originalDeltaValue = endValue - startValue;
            if (originalDeltaValue == 0f)
            {
                setter(endValue);

                return;
            }

            float duration;

            if (reset)
            {
                setter(startValue);

                duration = lerpDuration;
            }
            else
            {
                startValue = getter();

                float deltaValue = endValue - startValue;
                if (deltaValue == 0f)
                {
                    setter(endValue);

                    return;
                }

                duration = Mathf.Abs(deltaValue / originalDeltaValue) * lerpDuration;
            }

            float startTime = ignoreTimeScale ? Time.realtimeSinceStartup : Time.time;

            while (!cancellationToken.IsCancellationRequested)
            {
                await UniTask.Yield(cancellationToken: cancellationToken);

                float currentTime = ignoreTimeScale ? Time.realtimeSinceStartup : Time.time;
                float rate = (currentTime - startTime) / duration;

                setter(Mathf.Lerp(startValue, endValue, rate));

                if (rate >= 1f)
                {
                    break;
                }
            }
        }

        public static async UniTask LerpAsync(
            Action<Vector3> setter,
            Func<Vector3> getter,
            Vector3 startValue,
            Vector3 endValue,
            float lerpDuration,
            bool reset,
            bool ignoreTimeScale,
            CancellationToken cancellationToken) => await LerpAsync(null, setter, getter, startValue, endValue, lerpDuration, reset, ignoreTimeScale, cancellationToken);

        public static async UniTask LerpAsync(
            AnimationCurve curve,
            Action<Vector3> setter,
            Func<Vector3> getter,
            Vector3 startValue,
            Vector3 endValue,
            float lerpDuration,
            bool reset,
            bool ignoreTimeScale,
            CancellationToken cancellationToken)
        {
            if (lerpDuration <= 0f)
            {
                setter(endValue);

                return;
            }

            Vector3 originalDeltaValue = endValue - startValue;
            if (originalDeltaValue == Vector3.zero)
            {
                setter(endValue);

                return;
            }

            float duration;

            if (reset)
            {
                setter(startValue);

                duration = lerpDuration;
            }
            else
            {
                startValue = getter();

                Vector3 deltaValue = endValue - startValue;
                if (deltaValue == Vector3.zero)
                {
                    setter(endValue);

                    return;
                }

                duration = Mathf.Abs(deltaValue.magnitude / originalDeltaValue.magnitude) * lerpDuration;
            }

            float startTime = ignoreTimeScale ? Time.realtimeSinceStartup : Time.time;

            while (!cancellationToken.IsCancellationRequested)
            {
                await UniTask.Yield(cancellationToken: cancellationToken);

                float currentTime = ignoreTimeScale ? Time.realtimeSinceStartup : Time.time;
                float rate = (currentTime - startTime) / duration;

                if (curve is not null)
                {
                    rate = curve.Evaluate(rate);
                }

                setter(Vector3.Lerp(startValue, endValue, rate));

                if (rate >= 1f)
                {
                    break;
                }
            }
        }

        public static bool Contains(this Collider collider, Vector3 worldPoint, bool useRigidbody = true)
        {
            Vector3 closest = collider.ClosestPoint(worldPoint);

            Vector3 originToContact;

            Rigidbody rigidbody = useRigidbody ? collider.attachedRigidbody : null;
            // If you're checking if a worldPoint is within a moving rigidbody and want to use it instead (ideally a single collider rigidbody; multiple could move the center of mass between the colliders, placing it "outside" and returning false positives).
            if (rigidbody)
            {
                // The benefit of this is the use of the center of mass for a more accurate physics origin;
                // we multiply by rotation to convert it from it's local-space to a world offset.
                originToContact = closest - (rigidbody.position + rigidbody.rotation * rigidbody.centerOfMass);
            }
            else
            {
                Transform colliderTransform = collider.transform;
                originToContact = closest - (colliderTransform.position + colliderTransform.rotation * collider.bounds.center);
            }

            Vector3 pointToContact = closest - worldPoint;

            // Here we make the magic, originToContact points from the center to the closest worldPoint.
            // So if the angle between it and the pointToContact is less than 90, pointToContact is also looking from the inside-out.
            // The angle will probably be 180 or 0, but it's bad to compare exact floats and the rigidbody centerOfMass calculation could add some potential wiggle to the angle, so we use "< 90" to account for any of that.
            return Vector3.Angle(originToContact, pointToContact) < 90;
        }
    }
}