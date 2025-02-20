// Copyright (c) Jason Ma
//
// Copy and modify this file to customize which models should bake outline normals.

using System.Diagnostics.CodeAnalysis;
using System.IO;
using UnityEditor;

namespace OutlineNormalSmoother
{
    internal static class SampleCustomImportRule
    {
        internal static bool ShouldBakeOutlineNormal(string assetPath, [MaybeNull] AssetPostprocessor assetPostprocessor)
        {
	        bool shouldBakeOutlineNormal = false;
            
	        // assetPostprocessor will be null when moving assets.
	        var modelImporter = assetPostprocessor == null 
		        ? AssetImporter.GetAtPath(assetPath) as ModelImporter
		        : assetPostprocessor.assetImporter as ModelImporter; 

	        // **_outline.*
	        shouldBakeOutlineNormal |= Path.GetFileNameWithoutExtension(assetPath).ToLower().EndsWith("_outline");

	        // Assets\Test\**
	        shouldBakeOutlineNormal |= OutlineNormalImporter.NormalizeDirectorySeparatorChar(assetPath).StartsWith(
		        OutlineNormalImporter.NormalizeDirectorySeparatorChar(@"Assets\Test\"));
            
	        if (modelImporter != null)
	        {
		        // importTangents == Import
		        shouldBakeOutlineNormal |= modelImporter.importTangents == ModelImporterTangents.Import;
	        }

	        return shouldBakeOutlineNormal;
        }

        [InitializeOnLoadMethod]
        internal static void RegisterEvent()
        {
            /* >>>>>>>>>>>>>>>>> Uncomment to register the event <<<<<<<<<<<<<<<<< */
            // OutlineNormalImporter.shouldBakeOutlineNormal = ShouldBakeOutlineNormal;
        }
    }
}