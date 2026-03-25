using AssetsTools.NET.Extra;
using UnityEditor;

namespace CodedImmersions.Obfuscator.Editor
{
    public sealed class SpriteObfuscationModule : Module
    {
        protected override string name => "Sprite Obfuscation Module";
        public override bool IsAssetModule => true;

        public SpriteObfuscationModule() : base() { }

        internal override void Start(string buildPath, BuildTarget target)
        {
            if (!base.SetUp(buildPath, target)) return;
            base.ModifyAssets(AssetClassID.Sprite, buildPath, target);
        }
    }
}
