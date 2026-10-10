using System;
using System.Threading;
using UnityEngine;
using Cysharp.Threading.Tasks;

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
            Vector3 to = destination - currentPosition;
            float distanceSquared = to.sqrMagnitude;

            // 제곱 길이가 넘치는 경우에는 설치된 Unity의 normalized 처리 규칙을 그대로 사용합니다.
            if (float.IsInfinity(distanceSquared) || float.IsNaN(distanceSquared))
            {
                return Vector3.Dot(movingDirection, to.normalized) < 0.0f;
            }

            if (distanceSquared <= Vector3.kEpsilon * Vector3.kEpsilon)
            {
                return false;
            }

            float dot = Vector3.Dot(movingDirection, to);
            if (dot == 0f || float.IsNaN(dot) || float.IsInfinity(dot))
            {
                return Vector3.Dot(movingDirection, to.normalized) < 0.0f;
            }

            return dot < 0.0f;
        }

        public static void Matrix4X4Lerp(ref Matrix4x4 result, Matrix4x4 org, Matrix4x4 target, float t)
        {
            for (int i = 0; i < 16; ++i)
            {
                result[i] = Mathf.Lerp(org[i], target[i], t);
            }
        }

        public static Matrix4x4 Lerp(this Matrix4x4 org, Matrix4x4 target, float t)
        {
            Matrix4x4 result = default;
            Matrix4X4Lerp(ref result, org, target, t);
            return result;
        }

        public static float Cross(Vector2 a, Vector2 b) => Vector3.Cross(a, b).z;
        public static float Cross(Vector2 p, Vector2 a, Vector2 b) => Cross(a - p, b - p);

        private static bool Intersect(Vector2 a, Vector2 b, Vector2 c, Vector2 d) => TryGetIntersection(a, b, c, d, out _);

        private static bool TryGetIntersection(Vector2 a, Vector2 b, Vector2 c, Vector2 d, out Vector2 point)
        {
            point = default;

            double rx = (double)b.x - a.x, ry = (double)b.y - a.y;
            double sx = (double)d.x - c.x, sy = (double)d.y - c.y;
            double qx = (double)c.x - a.x, qy = (double)c.y - a.y;
            double rr = rx * rx + ry * ry, ss = sx * sx + sy * sy;

            if (rr == 0d)
            {
                if (ss == 0d)
                {
                    if (a.x != c.x || a.y != c.y)
                    {
                        return false;
                    }
                }
                else
                {
                    double along = -qx * sx - qy * sy;
                    if (qx * sy - qy * sx != 0d || !(along >= 0d && along <= ss))
                    {
                        return false;
                    }
                }

                point = a;

                return true;
            }

            if (ss == 0d)
            {
                double along = qx * rx + qy * ry;
                if (qx * ry - qy * rx != 0d || !(along >= 0d && along <= rr))
                {
                    return false;
                }

                point = c;

                return true;
            }

            double denominator = rx * sy - ry * sx;
            double t;
            if (denominator == 0d)
            {
                if (qx * ry - qy * rx != 0d)
                {
                    return false;
                }

                // 공선 중첩은 source의 시작점에서 처음 도달하는 점을 반환합니다.
                double t0 = Math.Abs(rx) >= Math.Abs(ry) ? qx / rx : qy / ry;
                double t1 = Math.Abs(rx) >= Math.Abs(ry) ? ((double)d.x - a.x) / rx : ((double)d.y - a.y) / ry;
                t = Math.Max(0d, Math.Min(t0, t1));
                if (t > Math.Min(1d, Math.Max(t0, t1)))
                {
                    return false;
                }
            }
            else
            {
                t = (qx * sy - qy * sx) / denominator;
                double u = (qx * ry - qy * rx) / denominator;
                if (!(t >= 0d && t <= 1d && u >= 0d && u <= 1d))
                {
                    return false;
                }
            }

            point = new Vector2((float)(a.x + t * rx), (float)(a.y + t * ry));

            return true;
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

            Vector2 bottomLeft = rect.min;
            Vector2 bottomRight = new(rect.xMax, rect.yMin);
            Vector2 topRight = rect.max;
            Vector2 topLeft = new(rect.xMin, rect.yMax);

            return
                Intersect(p0, p1, bottomLeft, bottomRight) ||
                Intersect(p0, p1, bottomRight, topRight) ||
                Intersect(p0, p1, topRight, topLeft) ||
                Intersect(p0, p1, topLeft, bottomLeft);
        }

        /// <summary>XY 선분의 끝점 접촉을 포함한 교차를 검사합니다. 공선 중첩은 source1에서 처음 만나는 점을 반환하며 결과 z는 0입니다.</summary>
        public static bool GetIntersectPosition(Vector3 source1, Vector3 source2, Vector3 target1, Vector3 target2, out Vector3 intersectPos)
        {
            intersectPos = Vector3.zero;
            if (!TryGetIntersection(source1, source2, target1, target2, out Vector2 point))
            {
                return false;
            }

            intersectPos = new Vector3(point.x, point.y, 0f);

            return true;
        }

        /// <summary>꼭짓점의 XZ 좌표와 pos의 XY 좌표로 다각형 내부를 검사합니다. 경계 포함은 보장하지 않습니다.</summary>
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

        /// <summary>꼭짓점과 위치를 XY에 투영해 다각형 내부를 검사합니다. Z는 무시하며 경계 포함은 보장하지 않습니다.</summary>
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

        /// <summary>둘레 순서의 네 꼭짓점을 XY에 투영해 선분과의 교차를 검사합니다. 끝점 접촉을 포함합니다.</summary>
        public static bool RectIntersect(Vector3[] rect, Vector2 p0, Vector2 p1)
        {
            if (rect is null)
            {
                throw new ArgumentNullException(nameof(rect));
            }

            if (rect.Length != 4)
            {
                throw new ArgumentException("Exactly four perimeter-ordered corners are required.", nameof(rect));
            }

            float rectXMin = rect[0].x, rectXMax = rect[0].x;
            float rectYMin = rect[0].y, rectYMax = rect[0].y;

            for (int i = 1; i < 4; i++)
            {
                Vector3 corner = rect[i];
                rectXMin = Mathf.Min(rectXMin, corner.x);
                rectXMax = Mathf.Max(rectXMax, corner.x);
                rectYMin = Mathf.Min(rectYMin, corner.y);
                rectYMax = Mathf.Max(rectYMax, corner.y);
            }

            if (Mathf.Max(p0.x, p1.x) < rectXMin || Mathf.Min(p0.x, p1.x) > rectXMax ||
                Mathf.Max(p0.y, p1.y) < rectYMin || Mathf.Min(p0.y, p1.y) > rectYMax)
            {
                return false;
            }

            if (RectContains3D(rect, p0) || RectContains3D(rect, p1))
            {
                return true;
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
        // 유한한 strength는 [0, 1000], deltaTime은 [0, 1]로 제한하며 기존 ms 단위와 0.06 계수를 유지합니다.
        public static Vector3 SpringDampen(ref Vector3 velocity, float strength, float deltaTime)
        {
            float displacement = GetSpringDampening(strength, deltaTime, out float attenuation);
            Vector3 total = velocity * displacement;
            velocity *= attenuation;
            return total;
        }

        /// <summary>
        /// Same as the Vector3 version, it's a framerate-independent Lerp.
        /// </summary>
        //@@by ps2 from NGUIMath
        public static Vector2 SpringDampen(ref Vector2 velocity, float strength, float deltaTime)
        {
            float displacement = GetSpringDampening(strength, deltaTime, out float attenuation);
            Vector2 total = velocity * displacement;
            velocity *= attenuation;
            return total;
        }

        // for float
        public static float SpringDampen(ref float velocity, float strength, float deltaTime)
        {
            float displacement = GetSpringDampening(strength, deltaTime, out float attenuation);
            float total = velocity * displacement;
            velocity *= attenuation;
            return total;
        }

        private static float GetSpringDampening(float strength, float deltaTime, out float attenuation)
        {
            if (float.IsNaN(strength) || float.IsInfinity(strength))
            {
                throw new ArgumentOutOfRangeException(nameof(strength));
            }

            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime))
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }

            attenuation = 1f;

            int ms = Mathf.RoundToInt(Mathf.Clamp01(deltaTime) * 1000f);
            if (ms == 0)
            {
                return 0f;
            }

            strength = Mathf.Clamp(strength, 0f, 1000f);
            if (strength == 0f)
            {
                return ms * 0.06f;
            }

            if (strength == 1000f)
            {
                attenuation = 0f; return 0f;
            }

            double exponent = ms * Math.Log(1d - strength * 0.001d);
            double dampening = Math.Exp(exponent);
            attenuation = (float)dampening;

            // exp(x)-1의 상쇄 오차를 피하고 작은 강도에서도 0 강도의 극한값으로 이어지게 합니다.
            double ratio =
                Math.Abs(exponent) < 0.0001d
                    ? 1d + exponent * (0.5d + exponent * (1d / 6d + exponent / 24d))
                    : (dampening - 1d) / exponent;

            return (float)(ms * ratio * 0.06d);
        }

        /// <summary>사전 취소 시 값을 쓰지 않습니다. 진행 중 취소는 취소로 종료하고 정상 완료 시 endValue를 설정합니다.</summary>
        public static async UniTask LerpAsync(Action<float> setter, Func<float> getter, float startValue, float endValue, float lerpDuration, bool reset, bool ignoreTimeScale, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            ValidateLerpDuration(lerpDuration);
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

            ValidateLerpDuration(duration);
            if (duration <= 0f)
            {
                setter(endValue);
                return;
            }

            float startTime = ignoreTimeScale ? Time.realtimeSinceStartup : Time.time;

            while (true)
            {
                await UniTask.Yield(cancellationToken: cancellationToken);

                float currentTime = ignoreTimeScale ? Time.realtimeSinceStartup : Time.time;
                float rate = Mathf.Clamp01((currentTime - startTime) / duration);

                setter(rate >= 1f ? endValue : Mathf.Lerp(startValue, endValue, rate));

                if (rate >= 1f)
                {
                    return;
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
            CancellationToken cancellationToken) =>
            await LerpAsync(null, setter, getter, startValue, endValue, lerpDuration, reset, ignoreTimeScale, cancellationToken);

        /// <summary>곡선은 중간 진행률에 적용하고 시간 경과 후 endValue를 설정합니다. 취소는 시작 시와 Yield에서 검사합니다.</summary>
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
            cancellationToken.ThrowIfCancellationRequested();

            ValidateLerpDuration(lerpDuration);
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

            ValidateLerpDuration(duration);
            if (duration <= 0f)
            {
                setter(endValue);
                return;
            }

            float startTime = ignoreTimeScale ? Time.realtimeSinceStartup : Time.time;

            while (true)
            {
                await UniTask.Yield(cancellationToken: cancellationToken);

                float currentTime = ignoreTimeScale ? Time.realtimeSinceStartup : Time.time;
                float rate = Mathf.Clamp01((currentTime - startTime) / duration);

                Vector3 value = rate >= 1f ? endValue : Vector3.Lerp(startValue, endValue, curve?.Evaluate(rate) ?? rate);
                setter(value);

                if (rate >= 1f)
                {
                    return;
                }
            }
        }

        private static void ValidateLerpDuration(float duration)
        {
            if (float.IsNaN(duration) || float.IsInfinity(duration))
            {
                throw new ArgumentOutOfRangeException(nameof(duration));
            }
        }

        /// <summary>활성 Box/Sphere/Capsule/convex MeshCollider의 내부와 경계를 0.00001 world 단위 오차로 검사합니다. 그 외 대상은 false입니다.</summary>
        /// <param name="useRigidbody">이전 호출과의 호환성을 위한 인수이며 포함 판정에는 영향을 주지 않습니다.</param>
        public static bool Contains(this Collider collider, Vector3 worldPoint, bool useRigidbody = true)
        {
            if (!collider || !collider.enabled || !collider.gameObject.activeInHierarchy)
            {
                return false;
            }

            if (!(collider is BoxCollider || collider is SphereCollider || collider is CapsuleCollider ||
                  collider is MeshCollider { convex: true, sharedMesh: not null }))
            {
                return false;
            }

            return (collider.ClosestPoint(worldPoint) - worldPoint).sqrMagnitude <= 0.0000000001f;
        }
    }
}
