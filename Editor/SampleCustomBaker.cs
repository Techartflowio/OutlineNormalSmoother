// Copyright (c) Jason Ma
//
// Copy and modify this file to customize where outline normals are stored.

using Unity.Collections;
using UnityEditor;
using UnityEngine;

namespace OutlineNormalSmoother
{
    internal static class SampleCustomBaker
    {
        internal static void SaveOutlineNormalToMesh(Mesh mesh, ref NativeArray<Color> bakedColors, ref NativeArray<Vector3> smoothedNormalTangentSpace)
        {
            // Save to Vertex Color RGB (smoothedNormal * 0.5 + 0.5)
            // originalMesh.colors = bakedColors.ToArray();

            // Save to uv2
            var uvs = mesh.uv2;
            var smoothedNormalTS = smoothedNormalTangentSpace.ToArray();
            {
                for (int i = 0; i < mesh.vertexCount; i++)
                {
                    uvs[i] = new Vector2(smoothedNormalTS[i].x, smoothedNormalTS[i].y);
                }
            }
            mesh.uv2 = uvs;
        }

        [InitializeOnLoadMethod]
        internal static void RegisterEvent()
        {
            /* >>>>>>>>>>>>>>>>> Uncomment to register the event <<<<<<<<<<<<<<<<< */
            // OutlineNormalBacker.onSaveToOriginalMesh = SaveOutlineNormalToMesh;
        }
    }
}