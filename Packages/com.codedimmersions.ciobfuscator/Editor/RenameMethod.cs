using UnityEngine;

namespace CodedImmersions.Obfuscator.Editor
{
    public enum RenameMethod
    {
        /// <summary>
        /// Randomized pre-set ASCII characters (instead of randomized unicode characters) that will confuse bad actors.
        /// </summary>
        [InspectorName("Randomized (ASCII)")] Randomized = 0,

        /// <summary>
        /// Randomized UTF-16 characters. (non-surrogate Unicode characters from <see cref="char.MinValue"/> to <see cref="char.MaxValue"/>)
        /// </summary>
        [InspectorName("Randomized (Unicode)")] RandomizedUnicode = 1,

        /// <summary>
        /// Replaces all names with the same repeated name.
        /// </summary>
        SameName = 2,

        /// <summary>
        /// Completely nullifies the name. Scene modules will show blank GameObject names, and Asset modules will be different depending on the tool (Ex. Asset Ripper will show [AssetType]_[Number++], and UABEA will show "Unnamed asset").
        /// </summary>
        NoName = 3
    }
}
