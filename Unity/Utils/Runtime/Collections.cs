using System.Collections.Generic;
namespace steph.Unity.Utils.Runtime
{
    public static class Collections
    {
        public static T GetRandomItem<T>(this List<T> list, System.Random generator = null)
        {
            generator ??= new System.Random();
            return list[generator.Next(0, list.Count)];
        }

        public static T GetRandomItem<T>(this T[] arr, System.Random generator = null)
        {
            generator ??= new System.Random();
            return arr[generator.Next(0, arr.Length)];
        }
    }
}