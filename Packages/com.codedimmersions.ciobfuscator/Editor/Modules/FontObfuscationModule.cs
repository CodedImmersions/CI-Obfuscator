using AssetsTools.NET.Extra;
using UnityEditor;

namespace CodedImmersions.Obfuscator.Editor
{
    public sealed class FontObfuscationModule : Module
    {
        protected override string name => "Font Obfuscation Module";
        public override bool IsAssetModule => true;

        public FontObfuscationModule() : base() { }

        internal override void Start(string buildPath, BuildTarget target)
        {
            if (!base.SetUp(buildPath, target)) return;
            base.ModifyAssets(AssetClassID.Font, buildPath, target);
        }
    }
}
