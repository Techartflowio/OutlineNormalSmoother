// Copyright (c) Jason Ma

using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Jobs;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace OutlineNormalSmoother
{
    public static class OutlineNormalBacker
    {
        public delegate void OutlineNormalBackerCustomSaveEvent(Mesh mesh, ref NativeArray<Vector3> smoothedNormalTangentSpace);

        public static OutlineNormalBackerCustomSaveEvent onSaveToMesh =
            (Mesh mesh, ref NativeArray<Vector3> smoothedNormalTangentSpace) =>
            {
                List<Vector3> smoothedNormals = new(smoothedNormalTangentSpace.Length);
                for (int i = 0; i < smoothedNormalTangentSpace.Length; i++)
                    smoothedNormals.Add(smoothedNormalTangentSpace[i]);

                mesh.SetUVs(7, smoothedNormals);
            };

        private const int BATCH_COUNT = 256;
        private const int MAX_COINCIDENT_VERTICES = 32;

        private struct CollectWeightedNormalJob : IJobParallelFor
        {
            [ReadOnly] internal NativeArray<int> indices;
            [ReadOnly] internal NativeArray<Vector3> vertices;
            [NativeDisableParallelForRestriction] internal NativeArray<UnsafeParallelHashMap<Vector3, float3>.ParallelWriter> outPositionNormalHashMapArray;

            public void Execute(int vertexIndexInSubMesh)
            {
                int vertexIndexInTriangle = vertexIndexInSubMesh % 3;
                var position = vertices[indices[vertexIndexInSubMesh]];
                float3 p1 = vertices[indices[vertexIndexInSubMesh - vertexIndexInTriangle]];
                float3 p2 = vertices[indices[vertexIndexInSubMesh - vertexIndexInTriangle + 1]];
                float3 p3 = vertices[indices[vertexIndexInSubMesh - vertexIndexInTriangle + 2]];

                CalculateWeightedAngle(p1, p2, p3, vertexIndexInTriangle, out var normal, out var angle);
                var angleWeightedNormal = normal * angle;

                for (int i = 0; i < outPositionNormalHashMapArray.Length + 1; i++)
                {
                    if (i == outPositionNormalHashMapArray.Length)
                    {
                        Debug.LogError($"[OutlineNormalSmoother] Coincident vertices exceed limit ({MAX_COINCIDENT_VERTICES})!");
                        break;
                    }

                    if (outPositionNormalHashMapArray[i].TryAdd(position, angleWeightedNormal))
                        break;
                }
            }
        }

        private struct BakeNormalJob : IJobParallelFor
        {
            [ReadOnly] internal NativeArray<int> indices;
            [ReadOnly] internal NativeArray<Vector3> vertices, normals;
            [ReadOnly] internal NativeArray<Vector4> tangents;
            [ReadOnly] internal NativeArray<UnsafeParallelHashMap<Vector3, float3>> positionNormalHashMapArray;

            [NativeDisableParallelForRestriction] internal NativeArray<Vector3> outSmoothedNormalTangentSpace;

            public void Execute(int vertexIndexInSubMesh)
            {
                var vertexIndex = indices[vertexIndexInSubMesh];
                var position = vertices[vertexIndex];

                float3 smoothedNormal = 0;
                for (int i = 0; i < positionNormalHashMapArray.Length; i++)
                {
                    if (positionNormalHashMapArray[i].TryGetValue(position, out var angleWeightedNormal))
                        smoothedNormal += angleWeightedNormal;
                    else break;
                }

                smoothedNormal = math.normalizesafe(smoothedNormal);

                float3 normal = math.normalizesafe(normals[vertexIndex]);
                float4 tangent = tangents[vertexIndex];
                tangent.xyz = math.normalizesafe(tangent.xyz);
                float3 binormal = math.normalizesafe(math.cross(normal, tangent.xyz) * tangent.w);

                float3x3 tangentToObject = new float3x3(tangent.xyz, binormal, normal);
                float3 smoothedNormalTS = math.mul(math.transpose(tangentToObject), smoothedNormal);
                outSmoothedNormalTangentSpace[vertexIndex] = smoothedNormalTS;
            }
        }

        internal static void BakeSmoothedNormalTangentSpaceToMesh(List<Mesh> meshes)
        {
            for (int i = 0; i < meshes.Count; i++)
            {
                var mesh = meshes[i];
                int vertexCount = mesh.vertexCount;
                var vertices = mesh.vertices;
                var normals = mesh.normals;
                var tangents = mesh.tangents;

                if (normals.Length == 0)
                {
                    mesh.RecalculateNormals();
                    normals = mesh.normals;
                    Debug.Log($"OutlineNormalSmoother: ({i + 1}/{meshes.Count}) Recalculate Normals: {mesh.name}");
                }

                if (tangents.Length == 0)
                {
                    mesh.RecalculateTangents();
                    tangents = mesh.tangents;
                    Debug.Log($"OutlineNormalSmoother: ({i + 1}/{meshes.Count}) Recalculate Tangents: {mesh.name}");
                }

                NativeArray<Vector3> nativeVertices = new(vertices, Allocator.TempJob);
                NativeArray<Vector3> nativeNormals = new(normals, Allocator.TempJob);
                NativeArray<Vector4> nativeTangents = new(tangents, Allocator.TempJob);
                NativeArray<Vector3> outSmoothedNormalTangentSpace = new(vertexCount, Allocator.TempJob);

                for (int j = 0; j < mesh.subMeshCount; j++)
                {
                    var indices = mesh.GetIndices(j);
                    int subMeshVertexCount = indices.Length;

                    NativeArray<int> nativeIndices = new(indices, Allocator.TempJob);
                    NativeArray<UnsafeParallelHashMap<Vector3, float3>> nativeHashMapArray = new(MAX_COINCIDENT_VERTICES, Allocator.TempJob);
                    NativeArray<UnsafeParallelHashMap<Vector3, float3>.ParallelWriter> nativeWriterArray = new(MAX_COINCIDENT_VERTICES, Allocator.TempJob);

                    for (int k = 0; k < MAX_COINCIDENT_VERTICES; k++)
                    {
                        UnsafeParallelHashMap<Vector3, float3> hashMap = new(subMeshVertexCount, Allocator.TempJob);
                        nativeHashMapArray[k] = hashMap;
                        nativeWriterArray[k] = hashMap.AsParallelWriter();
                    }

                    var collectJob = new CollectWeightedNormalJob
                    {
                        indices = nativeIndices,
                        vertices = nativeVertices,
                        outPositionNormalHashMapArray = nativeWriterArray
                    };

                    JobHandle collectHandle = collectJob.Schedule(subMeshVertexCount, BATCH_COUNT);

                    var bakeJob = new BakeNormalJob
                    {
                        indices = nativeIndices,
                        vertices = nativeVertices,
                        normals = nativeNormals,
                        tangents = nativeTangents,
                        positionNormalHashMapArray = nativeHashMapArray,
                        outSmoothedNormalTangentSpace = outSmoothedNormalTangentSpace
                    };

                    bakeJob.Schedule(subMeshVertexCount, BATCH_COUNT, collectHandle).Complete();

                    for (int k = 0; k < MAX_COINCIDENT_VERTICES; k++)
                        nativeHashMapArray[k].Dispose();

                    nativeIndices.Dispose();
                    nativeHashMapArray.Dispose();
                    nativeWriterArray.Dispose();
                }

                onSaveToMesh.Invoke(mesh, ref outSmoothedNormalTangentSpace);
                mesh.MarkModified();

                Debug.Log($"OutlineNormalSmoother: ({i + 1}/{meshes.Count}) Saved UV7 to mesh: {mesh.name}");

                nativeVertices.Dispose();
                nativeNormals.Dispose();
                nativeTangents.Dispose();
                outSmoothedNormalTangentSpace.Dispose();
            }
        }

        private static void CalculateWeightedAngle(float3 p1, float3 p2, float3 p3, int currentIndexInTriangle,
            out float3 outNormal, out float outAngle)
        {
            float3 d1 = 0, d2 = 0;

            switch (currentIndexInTriangle)
            {
                case 0: d1 = p1 - p3; d2 = p2 - p1; break;
                case 1: d1 = p2 - p1; d2 = p3 - p2; break;
                case 2: d1 = p3 - p2; d2 = p1 - p3; break;
            }

            d1 = math.normalizesafe(d1);
            d2 = math.normalizesafe(d2);

            outNormal = math.normalizesafe(math.cross(p1 - p3, p2 - p1));
            outAngle = math.acos(math.clamp(math.dot(d1, -d2), -1, 1));
        }
    }
}
