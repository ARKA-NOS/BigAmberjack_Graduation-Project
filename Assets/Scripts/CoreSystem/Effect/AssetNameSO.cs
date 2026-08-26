using UnityEngine;

namespace CoreSystem.Effect
{
    [CreateAssetMenu(fileName = "AssetName", menuName = "AssetName Data", order = 0)]
    public class AssetNameSo : ScriptableObject
    {
        [field: SerializeField] public string AssetName { get; private set; }
        [field:SerializeField] public int AssetHash { get; private set; }

        private void OnValidate()
        {
            if (!string.IsNullOrEmpty(AssetName))
            {
                AssetHash = Animator.StringToHash(AssetName);
            }
        }
    }
}