using AssetsTools.NET.Extra;
using UnityEditor;

namespace CodedImmersions.Obfuscator.Editor
{
    public sealed class Texture2DObfuscationModule : Module
    {
        protected override string name => "Texture2D Obfuscation Module";
        public override bool IsAssetModule => true;

        public Texture2DObfuscationModule() : base() { }

        internal override void Start(string buildPath, BuildTarget target)
        {
            if (!base.SetUp(buildPath, target)) return;
            base.ModifyAssets(AssetClassID.Texture2D, buildPath, target);
        }
    }
}
