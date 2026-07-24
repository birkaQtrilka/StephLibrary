using System;
using UnityEngine;

namespace steph.Unity.WFC.Runtime
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class PolymorphicAttribute : PropertyAttribute { }
}