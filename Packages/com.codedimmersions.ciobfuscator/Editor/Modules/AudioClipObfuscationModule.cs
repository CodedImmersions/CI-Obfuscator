using AssetsTools.NET.Extra;
using UnityEditor;

namespace CodedImmersions.Obfuscator.Editor
{
    public sealed class AudioClipObfuscationModule : Module
    {
        protected override string name => "AudioClip Obfuscation Module";
        public override bool IsAssetModule => true;

        public AudioClipObfuscationModule() : base() { }

        internal override void Start(string buildPath, BuildTarget target)
        {
            if (!base.SetUp(buildPath, target)) return;
            base.ModifyAssets(AssetClassID.AudioClip, buildPath, target);
        }
    }
}