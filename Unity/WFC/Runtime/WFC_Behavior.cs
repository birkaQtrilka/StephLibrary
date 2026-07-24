using UnityEngine;

namespace steph.Unity.WFC.Runtime
{
    public abstract class WFC_Behavior : ScriptableObject
    {
        public virtual void BeforePropagate(WorldGenConfig ctxt)
        {
            
        }
    }
}
