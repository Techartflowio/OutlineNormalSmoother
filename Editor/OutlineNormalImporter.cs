// Copyright (c) Jason Ma

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace OutlineNormalSmoother
{
	public class OutlineNormalImporter : AssetPostprocessor
	{
		public delegate bool OutlineNormalImporterCustomRuleEvent(string assetPath, [MaybeNull] AssetPostprocessor assetPostprocessor);

		// 기본 규칙: _outline.이 포함된 메시만 처리
		public static OutlineNormalImporterCustomRuleEvent shouldBakeOutlineNormal =
			(assetPath, assetPostprocessor) => assetPath.ToLower().Contains("_outline");

		private void OnPostprocessModel(GameObject go)
		{
			if (shouldBakeOutlineNormal(assetPath, this))
			{
				var meshes = GetSharedMeshesFromGameObject(go);

				Debug.Log($"[OutlineNormalSmoother] Imported '{Path.GetFileName(assetPath)}': Found {meshes.Count} mesh(es)");

				OutlineNormalBacker.BakeSmoothedNormalTangentSpaceToMesh(meshes);
			}
		}

		private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
		{
			// movedAssets는 OnPostprocessModel을 트리거하지 않음
			foreach (var movedAsset in movedAssets)
			{
				if (!shouldBakeOutlineNormal(movedAsset, null))
					continue;

				var movedGO = AssetDatabase.LoadAssetAtPath<GameObject>(movedAsset);
				if (movedGO == null)
					continue;

				var meshes = GetSharedMeshesFromGameObject(movedGO);

				Debug.Log($"[OutlineNormalSmoother] Moved '{Path.GetFileName(movedAsset)}': Found {meshes.Count} mesh(es)");

				OutlineNormalBacker.BakeSmoothedNormalTangentSpaceToMesh(meshes);
			}
		}

		internal static List<Mesh> GetSharedMeshesFromGameObject(GameObject go)
		{
			HashSet<Mesh> uniqueMeshes = new();

			if (go == null)
				return new List<Mesh>();

			foreach (var meshFilter in go.GetComponentsInChildren<MeshFilter>())
			{
				if (meshFilter.sharedMesh != null)
					uniqueMeshes.Add(meshFilter.sharedMesh);
			}

			foreach (var skinnedMeshRenderer in go.GetComponentsInChildren<SkinnedMeshRenderer>())
			{
				if (skinnedMeshRenderer.sharedMesh != null)
					uniqueMeshes.Add(skinnedMeshRenderer.sharedMesh);
			}

			return uniqueMeshes.ToList();
		}

		public static string NormalizeDirectorySeparatorChar(string path)
		{
			return path.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		}
	}
}
