using UnityEngine;


namespace CrowRx
{
    public static class RectTransformExtension
    {
        public static RectTransform FitParents(this RectTransform self)
        {
            if (self)
            {
                // 기본 위치 세팅
                self.anchorMin = Vector2.zero;
                self.anchorMax = Vector2.one;
                self.offsetMin = Vector2.zero;
                self.offsetMax = Vector2.zero;

                self.transform.SetLocalTransformDefault();
            }

            return self;
        }
    }
}