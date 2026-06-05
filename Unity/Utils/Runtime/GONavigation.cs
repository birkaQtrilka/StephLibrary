using System.Collections.Generic;
using UnityEngine;
namespace steph.Unity.Utils.Runtime
{
    public static class GONavigation
    {
        public static void DestroyAllChildren(this Transform transform, bool immediate = false)
        {
            if (immediate)
            {
                for (int i = transform.childCount - 1; i >= 0; --i)
                    GameObject.DestroyImmediate(transform.GetChild(i).gameObject);
            }
            else
            {
                for (int i = transform.childCount - 1; i >= 0; --i)
                    GameObject.Destroy(transform.GetChild(i).gameObject);
            }
        }

        public static List<GameObject> FindGameObjectInChildWithTag(this GameObject parent, string tag)
        {
            Transform t = parent.transform;
            List<GameObject> objs = new();
            for (int i = 0; i < t.childCount; i++)
            {
                if (t.GetChild(i).gameObject.CompareTag(tag))
                {
                    objs.Add(t.GetChild(i).gameObject);
                }

            }

            return objs;
        }

        public static void FindGameObjectInChildWithTag(this GameObject parent, string tag, List<GameObject> results)
        {
            Transform t = parent.transform;
            for (int i = 0; i < t.childCount; i++)
            {
                if (t.GetChild(i).gameObject.CompareTag(tag))
                {
                    results.Add(t.GetChild(i).gameObject);
                }

            }

        }

    }
}
