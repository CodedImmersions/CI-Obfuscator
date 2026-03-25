using AssetsTools.NET.Extra;
using UnityEditor;

namespace CodedImmersions.Obfuscator.Editor
{
    public sealed class VideoClipObfuscationModule : Module
    {
        protected override string name => "VideoClip Obfuscation Module";
        public override bool IsAssetModule => true;

        public VideoClipObfuscationModule() : base() { }

        internal override void Start(string buildPath, BuildTarget target)
        {
            if (!base.SetUp(buildPath, target)) return;
            base.ModifyAssets(AssetClassID.VideoClip, buildPath, target);
        }
    }
}
