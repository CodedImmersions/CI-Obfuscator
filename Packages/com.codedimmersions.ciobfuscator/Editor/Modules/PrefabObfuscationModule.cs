using AssetsTools.NET.Extra;
using UnityEditor;

namespace CodedImmersions.Obfuscator.Editor
{
    public sealed class PrefabObfuscationModule : Module
    {
        protected override string name => "Prefab Obfuscation Module";
        public override bool IsAssetModule => true;

        public PrefabObfuscationModule() : base() { }

        internal override void Start(string buildPath, BuildTarget target)
        {
            if (!base.SetUp(buildPath, target)) return;
            base.ModifyAssets(AssetClassID.Prefab, buildPath, target);
        }
    }
}
