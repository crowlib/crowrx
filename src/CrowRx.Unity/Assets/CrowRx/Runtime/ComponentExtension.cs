using System;
using UnityEngine;


namespace CrowRx
{
    public static class ComponentExtension
    {
        public static TComponent GetOrAddComponent<TComponent>(this Component self)
            where TComponent : Component
            => self.gameObject.GetOrAddComponent<TComponent>();

        public static Component GetOrAddComponent(this Component self, Type componentType) => self.gameObject.GetOrAddComponent(componentType);

        public static TComponent AddComponent<TComponent>(this Component self)
            where TComponent : Component
            => self.gameObject.AddComponent<TComponent>();

        public static Component AddComponent(this Component self, Type componentType) => self.gameObject.AddComponent(componentType);

        public static TComponent SetParent<TComponent>(this TComponent self, Transform transform)
            where TComponent : Component
        {
            if (self)
                self.gameObject.SetParent(transform);

            return self;
        }

        public static TComponent SetParent<TComponent>(this TComponent self, Component component)
            where TComponent : Component
            => component ? self.SetParent(component.transform) : null;

        public static TComponent SetParent<TComponent>(this TComponent self, GameObject gameObject)
            where TComponent : Component
            => gameObject ? self.SetParent(gameObject.transform) : null;

        public static TComponent SetActive<TComponent>(this TComponent self, bool isActive)
            where TComponent : Component
        {
            if (self && self.gameObject)
                self.gameObject.SetActive(isActive);

            return self;
        }

        public static TComponent SetAsFirstSibling<TComponent>(this TComponent self)
            where TComponent : Component
        {
            if (self && self.gameObject)
                self.gameObject.transform.SetAsFirstSibling();

            return self;
        }

        public static TComponent SetAsLastSibling<TComponent>(this TComponent self)
            where TComponent : Component
        {
            if (self && self.gameObject)
                self.gameObject.transform.SetAsLastSibling();

            return self;
        }

        public static TComponent SetLocalTransformDefault<TComponent>(this TComponent self)
            where TComponent : Component
        {
            self.transform.SetLocalTransformDefault();

            return self;
        }

        public static TComponent SetLayer<TComponent>(this TComponent self, string layerName)
            where TComponent : Component
        {
            self.gameObject.SetLayer(layerName);

            return self;
        }
    }
}