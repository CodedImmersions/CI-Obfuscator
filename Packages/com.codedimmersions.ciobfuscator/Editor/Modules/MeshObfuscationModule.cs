using AssetsTools.NET.Extra;
using UnityEditor;

namespace CodedImmersions.Obfuscator.Editor
{
    public sealed class MeshObfuscationModule : Module
    {
        protected override string name => "Mesh Obfuscation Module";
        public override bool IsAssetModule => true;

        public MeshObfuscationModule() : base() { }

        internal override void Start(string buildPath, BuildTarget target)
        {
            if (!base.SetUp(buildPath, target)) return;
            base.ModifyAssets(AssetClassID.Mesh, buildPath, target);
        }
    }
}
