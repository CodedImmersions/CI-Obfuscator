using AssetsTools.NET.Extra;
using UnityEditor;

namespace CodedImmersions.Obfuscator.Editor
{
    public sealed class RenderTextureObfuscationModule : Module
    {
        protected override string name => "Render Texture Obfuscation Module";
        public override bool IsAssetModule => true;

        public RenderTextureObfuscationModule() : base() { }

        internal override void Start(string buildPath, BuildTarget target)
        {
            if (!base.SetUp(buildPath, target)) return;
            base.ModifyAssets(AssetClassID.RenderTexture, buildPath, target);
        }
    }
}
