using UnityEngine;

namespace CodedImmersions.Obfuscator
{
    [AddComponentMenu("Coded Immersions/CI Obfuscator/CI Obfuscator Ignore")]
    public class CIObfuscatorIgnore : MonoBehaviour
    {
        [Tooltip("Enable if CI Obfuscator should skip child objects.")] public bool applyToChildren;
    }
}
