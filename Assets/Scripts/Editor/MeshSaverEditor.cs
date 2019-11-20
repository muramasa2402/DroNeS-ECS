﻿using UnityEditor;
using UnityEngine;

namespace Editor
{
    public static class MeshSaverEditor {
        [MenuItem("CONTEXT/MeshFilter/Save Mesh...")]
        public static void SaveMeshInPlace (MenuCommand menuCommand) {
            MeshFilter mf = menuCommand.context as MeshFilter;
            Mesh m = mf.sharedMesh;
            SaveMesh(m, m.name, false, true);
        }

        [MenuItem("CONTEXT/MeshFilter/Save Mesh As New Instance...")]
        public static void SaveMeshNewInstanceItem (MenuCommand menuCommand) {
            MeshFilter mf = menuCommand.context as MeshFilter;
            Mesh m = mf.sharedMesh;
            SaveMesh(m, m.name, true, true);
        }

        private static void SaveMesh (Mesh mesh, string name, bool makeNewInstance, bool optimizeMesh) {
            var path = EditorUtility.SaveFilePanel("Save Separate Mesh Asset", "Assets/", name, "asset");
            if (string.IsNullOrEmpty(path)) return;
        
            path = FileUtil.GetProjectRelativePath(path);

            var meshToSave = (makeNewInstance) ? Object.Instantiate(mesh) as Mesh : mesh;
		
            if (optimizeMesh)
                MeshUtility.Optimize(meshToSave);
        
            AssetDatabase.CreateAsset(meshToSave, path);
            AssetDatabase.SaveAssets();
        }
    }
}