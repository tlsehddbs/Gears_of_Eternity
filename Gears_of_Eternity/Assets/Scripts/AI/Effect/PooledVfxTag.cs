using UnityEngine;

namespace Combat.Vfx
{
    [DisallowMultipleComponent]
    public sealed class PooledVfxTag : MonoBehaviour
    {
        public int SpawnId { get; private set; }

        public void MarkSpawn(int id)
        {
            SpawnId = id;
        }
    }
}