using AssetsTools.NET.Extra;
using UnityEditor;

namespace CodedImmersions.Obfuscator.Editor
{
    public sealed class Texture3DObfuscationModule : Module
    {
        protected override string name => "Texture3D Obfuscation Module";
        public override bool IsAssetModule => true;

        public Texture3DObfuscationModule() : base() { }

        internal override void Start(string buildPath, BuildTarget target)
        {
            if (!base.SetUp(buildPath, target)) return;
            base.ModifyAssets(AssetClassID.Texture3D, buildPath, target);
        }
    }
}
