using UnityEngine;

namespace Tidebound
{
    internal static class TideVisuals
    {
        public static GameObject Part(Transform parent, PrimitiveType shape, string name,
            Vector3 position, Vector3 scale, Material material)
        {
            var part = GameObject.CreatePrimitive(shape);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            var collider = part.GetComponent<Collider>();
            collider.enabled = false;
            Object.Destroy(collider);
            part.GetComponent<Renderer>().sharedMaterial = material;
            return part;
        }

        public static Material Material(Color color, bool glow = false)
        {
            // A Resources asset keeps this shader in desktop builds even though geometry is created at runtime.
            var shader = Resources.Load<Shader>("TidePalette");
            var material = new Material(shader);
            material.SetColor("_BaseColor", color);
            if (glow)
            {
                material.SetColor("_EmissionColor", color * 2);
            }
            return material;
        }

        public static Transform Avatar(Transform parent, Material navy, Material white, Material cyan)
        {
            var rig = new GameObject("CharacterVisual - replace with your licensed model").transform;
            rig.SetParent(parent, false);
            Part(rig, PrimitiveType.Capsule, "Navy coat", new Vector3(0, 1.05f, 0), new Vector3(.6f, .42f, .42f), navy);
            Part(rig, PrimitiveType.Cylinder, "Coat hem", new Vector3(0, .62f, 0), new Vector3(.75f, .2f, .65f), navy);
            Part(rig, PrimitiveType.Sphere, "Head", new Vector3(0, 1.65f, 0), Vector3.one * .4f, white);
            Part(rig, PrimitiveType.Cylinder, "Hat brim", new Vector3(0, 1.91f, 0), new Vector3(.65f, .035f, .55f), navy);
            Part(rig, PrimitiveType.Cylinder, "Top hat", new Vector3(0, 2.08f, 0), new Vector3(.4f, .14f, .4f), navy);
            Part(rig, PrimitiveType.Sphere, "Water brooch", new Vector3(0, 1.25f, .23f), Vector3.one * .13f, cyan);
            foreach (var side in new[] { -1f, 1f })
            {
                Part(rig, PrimitiveType.Capsule, "Boot", new Vector3(side * .17f, .28f, 0), new Vector3(.2f, .26f, .24f), navy);
                Part(rig, PrimitiveType.Capsule, "Sleeve", new Vector3(side * .39f, 1.13f, 0), new Vector3(.17f, .3f, .17f), white);
                Part(rig, PrimitiveType.Capsule, "Hair ribbon", new Vector3(side * .18f, 1.3f, -.2f), new Vector3(.15f, .35f, .15f), white);
            }
            return rig;
        }

        public static void Wave(Vector3 center, float radius, Material material)
        {
            var go = new GameObject("Curtain of Tides");
            go.transform.position = center + Vector3.up * .15f;
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = false;
            line.loop = true;
            line.widthMultiplier = .16f;
            line.positionCount = 64;
            for (int i = 0; i < 64; i++)
            {
                float angle = i * Mathf.PI * 2 / 64;
                line.SetPosition(i, new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)));
            }
            var effect = go.AddComponent<TideRing>();
            effect.Radius = radius;
        }
    }

    internal sealed class TideRing : MonoBehaviour
    {
        public float Radius;
        float age;
        void Update()
        {
            age += Time.deltaTime;
            transform.localScale = Vector3.one * Mathf.Lerp(.2f, Radius, age / .5f);
            if (age > .6f) Destroy(gameObject);
        }
    }
}
