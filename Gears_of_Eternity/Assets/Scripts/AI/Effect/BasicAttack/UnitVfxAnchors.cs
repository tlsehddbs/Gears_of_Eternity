using UnityEngine;

[DisallowMultipleComponent]
public sealed class UnitVfxAnchors : MonoBehaviour
{
    // 원거리 유닛은 무기 끝(총구/노즐)에 빈 오브젝트를 만들어 여기에 할당
    public Transform muzzle;
}