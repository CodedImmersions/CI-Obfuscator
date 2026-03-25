using AssetsTools.NET.Extra;
using UnityEditor;

namespace CodedImmersions.Obfuscator.Editor
{
    public sealed class MaterialObfuscationModule : Module
    {
        protected override string name => "Material Obfuscation Module";
        public override bool IsAssetModule => true;

        public MaterialObfuscationModule() : base() { }

        internal override void Start(string buildPath, BuildTarget target)
        {
            if (!base.SetUp(buildPath, target)) return;
            base.ModifyAssets(AssetClassID.Material, buildPath, target);
        }
    }
}
