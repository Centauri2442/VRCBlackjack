using System;
using System.Collections;
/*
Copyright 2022 CentauriCore

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.
 */

using System.Collections.Generic;
using UdonSharpEditor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.SceneManagement;

namespace CentauriCore.Blackjack
{
    [CustomEditor(typeof(BlackjackWorldController))]
    
    public class BlackjackWorldControllerEditor : Editor
    {
        private BlackjackWorldController script;
        private Transform TableInstantiateParent;
        private List<BlackjackTableInstance> tables = new List<BlackjackTableInstance>();
        private List<GameObject> InstantiatedTables = new List<GameObject>();
        private List<Material> CameraMaterials = new List<Material>();
        private List<Texture> CameraRenderTextures = new List<Texture>();

        private Texture2D Logo;

        private int destroyIndex = -1;


        private void OnEnable()
        {
            script = target as BlackjackWorldController;
            if (script == null || AssetDatabase.Contains(script)) return;

            if (PrefabUtility.IsPartOfAnyPrefab(script.gameObject))
            {
                PrefabUtility.UnpackPrefabInstance(script.gameObject, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
            }
            
            TableInstantiateParent = script.transform.GetChild(0);
            
            Undo.RecordObject(script, "Pushed Values to World Controller");
            script.ChipController = script.transform.GetChild(1).GetComponent<ChipController>();
            Logo = Resources.Load<Texture2D>("BlackjackLogo");

            Undo.undoRedoPerformed += PullFromScript;
            PullFromScript();
        }
        
        private void OnDisable()
        {
            Undo.undoRedoPerformed -= PullFromScript;
        }
        
        public override void OnInspectorGUI()
        {
            if (AssetDatabase.Contains(script)) return;
            
            HeaderGUI();
            
            GUILayout.Space(5f);
            UdonSharpGUI.DrawUILine();
            GUILayout.Space(5f);

            ListGUI();
        }

        private void HeaderGUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                GUILayout.Box(Logo, GUIStyle.none);
                GUILayout.FlexibleSpace();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(new GUIContent("Use Chip System", "Toggle whether to globally use the chip system or not! This overrides all individual table chip settings!"));
                Undo.RecordObject(script, "Pushed Values to World Controller");
                Undo.RecordObject(script.ChipController, "Pushed Values to World Controller");
                script.ChipController.UseChipSystem = EditorGUILayout.Toggle(script.ChipController.UseChipSystem, GUILayout.Width(30));
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                Undo.RecordObject(script, "Pushed Values to World Controller");
                script.TablePrefab = (GameObject)EditorGUILayout.ObjectField(new GUIContent("Blackjack Table Prefab", "DO NOT TOUCH UNLESS YOU KNOW WHAT YOU'RE DOING"), script.TablePrefab, typeof(GameObject), false);
            }
        }

        private void ListGUI()
        {
            EditorGUI.BeginChangeCheck();

            for (int i = 0; i < tables.Count; i++)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(new GUIContent($"{tables[i].TableController.transform.parent.name}"), EditorStyles.boldLabel);
                    
                    GUILayout.FlexibleSpace();
                    
                    if (GUILayout.Button("Select", GUILayout.Width(60)))
                    {
                        Selection.activeGameObject = InstantiatedTables[i];
                        break;
                    }
                    
                    if (GUILayout.Button("✖", GUILayout.Width(20)))
                    {
                        RemoveTable(i);
                        break;
                    }
                }
                
                UdonSharpGUI.DrawUILine();
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (script.ChipController.UseChipSystem)
                    {
                        GUILayout.Label(new GUIContent("Use Chips", "Set whether or not to use the chip system!"));
                        tables[i].UseChips = EditorGUILayout.Toggle(tables[i].UseChips, GUILayout.Width(30));
                    }
                    else
                    {
                        GUIStyle style = new GUIStyle (); 
                        style.richText = true; 
                        GUILayout.Label(new GUIContent("<color=red>Use Chips</color>", "THIS WILL BE OVERRIDDEN BY GLOBAL SETTING"), style);
                        tables[i].UseChips = EditorGUILayout.Toggle(tables[i].UseChips, GUILayout.Width(30));
                    }
                    
                    GUILayout.Label(new GUIContent("Use Helpers", "Set use of helpers, such as number values shown above cards!"));
                    tables[i].UseHelpers = EditorGUILayout.Toggle(tables[i].UseHelpers, GUILayout.Width(30));
                }
                
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label(new GUIContent("Deck Count", "Set Deck Count!"));
                    tables[i].DeckCount = EditorGUILayout.IntField(tables[i].DeckCount, GUILayout.Width(30));

                    if (tables[i].DeckCount < 2)
                    {
                        tables[i].DeckCount = 2;
                    }
                    
                    GUILayout.Label(new GUIContent("Allow Change", "With this enabled, people can change these settings during runtime!"));
                    tables[i].CanChangeSettingsDuringRuntime = EditorGUILayout.Toggle(tables[i].CanChangeSettingsDuringRuntime, GUILayout.Width(30));
                }
                
                EditorGUILayout.EndVertical();
            }

            if (script.TablePrefab != null && PrefabUtility.GetPrefabType(target) == PrefabType.None)
            {
                if (GUILayout.Button("Add"))
                {
                    AddTable();
                }
            }

            if (EditorGUI.EndChangeCheck())
            {
                PushToScript();
            }
        }

        private void AddTable()
        {
            GameObject newTableObject = (GameObject)PrefabUtility.InstantiatePrefab(script.TablePrefab);
            newTableObject.transform.parent = TableInstantiateParent;
            
            InstantiatedTables.Add(newTableObject);

            Controller controller = newTableObject.transform.GetChild(0).GetComponent<Controller>();
            controller.ChipController = script.ChipController;
            
            BlackjackTableInstance table = new BlackjackTableInstance
            {
                TableController = controller,
                UseChips = controller.UsingChips,
                UseHelpers = controller.HelpersEnabled,
                DeckCount = controller.Deck.NumberOfDecks,
                CanChangeSettingsDuringRuntime = controller.CanChangeSettings
                
            };
            
            tables.Add(table);
            
            newTableObject.name = $"{script.TablePrefab.name} {tables.Count}";
        }

        private void RemoveTable(int index)
        {
            var tableToDelete = InstantiatedTables[index];

            destroyIndex = index;
            
            DestroyCameraAssets(index);
            
            tables.RemoveAt(index);
            InstantiatedTables.RemoveAt(index);
            DestroyImmediate(tableToDelete);

            FixTableNames();
        }
        
        private void CreateCameraAssets(int tableIndex)
        {
            if (destroyIndex > -1) return;

            if (script.BlackjackTables[tableIndex].RendererMaterial != null || script.BlackjackTables[tableIndex].RendererMaterial != null) return;
            
            var cameraMaterial = new Material(Shader.Find("Unlit/Texture"));
            var renderTexture = new RenderTexture(1024, 256, 24, GraphicsFormat.R8G8B8A8_UNorm);

            //string path = SceneManager.GetActiveScene().path;
            string path = "Assets";

            if (!AssetDatabase.IsValidFolder(path + "/BlackjackGeneratedAssets"))
            { 
                AssetDatabase.CreateFolder(path, "BlackjackGeneratedAssets");
            }
            
            AssetDatabase.CreateAsset(cameraMaterial, path + $"/BlackjackGeneratedAssets/{DateTime.Now.Millisecond+DateTime.Now.Ticks}.mat");
            AssetDatabase.CreateAsset(renderTexture, path + $"/BlackjackGeneratedAssets/{DateTime.Now.Millisecond+DateTime.Now.Ticks}.renderTexture");
            
            /*
             * GUILayout.BeginHorizontal();
        path = EditorGUILayout.TextField(path);
        if (GUILayout.Button("..."))
        {
            string newPath = path;

            newPath = EditorUtility.OpenFolderPanel("Select Output Directory", newPath, "");

            if (!newPath.StartsWith(Application.dataPath))
            {
                EditorUtility.DisplayDialog("Error", "Files from outside the project are not yet supported!", "OK");
                return;
            }

            path = "Assets/" + newPath.Substring(Application.dataPath.Length);
        }
             */

            cameraMaterial.mainTexture = renderTexture;
            
            CameraMaterials.Add(cameraMaterial);
            CameraRenderTextures.Add(renderTexture);
            
            Undo.RecordObject(script.BlackjackTables[tableIndex], "Pushed Camera Values to Controller");
            Undo.RecordObject(script.BlackjackTables[tableIndex].Camera, "Pushed Camera Values to Controller");
            Undo.RecordObject(script.BlackjackTables[tableIndex].CameraRenderer, "Pushed Camera Values to Controller");

            script.BlackjackTables[tableIndex].RenderTexture = renderTexture;
            script.BlackjackTables[tableIndex].Camera.targetTexture = renderTexture;
            script.BlackjackTables[tableIndex].RendererMaterial = cameraMaterial;
            script.BlackjackTables[tableIndex].CameraRenderer.material = cameraMaterial;
        }

        private void DestroyCameraAssets(int tableIndex)
        {
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(tables[tableIndex].TableController.RenderTexture));
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(tables[tableIndex].TableController.RendererMaterial));
        }

        private void FixTableNames()
        {
            for (int i = 0; i < InstantiatedTables.Count; i++)
            {
                InstantiatedTables[i].name = $"Blackjack {i}";
            }
        }

        private void PullFromScript()
        {
            int tableCount = script.BlackjackTables.Length;

            tables = new List<BlackjackTableInstance>();
            InstantiatedTables = new List<GameObject>();
            CameraMaterials = new List<Material>();
            CameraRenderTextures = new List<Texture>();

            for (int i = 0; i < tableCount; i++)
            {
                if (script.BlackjackTables[i] != null)
                {
                    BlackjackTableInstance table = new BlackjackTableInstance
                    {
                        TableController = script.BlackjackTables[i],
                        UseChips = script.BlackjackTables[i].UsingChips,
                        UseHelpers = script.BlackjackTables[i].HelpersEnabled,
                        DeckCount = script.BlackjackTables[i].Deck.NumberOfDecks,
                        CanChangeSettingsDuringRuntime = script.BlackjackTables[i].CanChangeSettings
                    };
                
                    tables.Add(table);
                    InstantiatedTables.Add(script.TableGameObjects[i]);
                    CameraMaterials.Add(script.TableCameraMaterials[i]);
                    CameraRenderTextures.Add(script.TableRenderTextures[i]);
                }
            }

            /*
            for (int i = 0; i < script.TableCameraMaterials.Length; i++)
            {
                if (!CameraMaterials.Contains(script.TableCameraMaterials[i]))
                {
                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(script.TableCameraMaterials[i]));
                    AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(script.TableRenderTextures[i]));
                }
            }*/
            
            FixTableNames();
        }

        private void PushToScript()
        {
            List<Controller> tempControllers = new List<Controller>();
            List<bool> tempUseChips = new List<bool>();
            List<bool> tempUseHelpers = new List<bool>();
            List<int> tempDeckCount = new List<int>();
            List<bool> tempChangeSettings = new List<bool>();
            List<GameObject> tempTables = new List<GameObject>();

            foreach (BlackjackTableInstance table in tables)
            {
                tempControllers.Add(table.TableController);
                tempUseChips.Add(table.UseChips);
                tempUseHelpers.Add(table.UseHelpers);
                tempDeckCount.Add(table.DeckCount);
                tempChangeSettings.Add(table.CanChangeSettingsDuringRuntime);
            }
            
            Undo.RecordObject(script, "Pushed Values to World Controller");

            foreach (GameObject table in InstantiatedTables)
            {
                tempTables.Add(table);
            }

            script.BlackjackTables = tempControllers.ToArray();
            script.TableGameObjects = tempTables.ToArray();
            
            Undo.RecordObject(script.ChipController, "Pushed Values to Tables");
            script.ChipController.BlackjackTables = script.BlackjackTables;

            for (int i = 0; i < tables.Count; i++)
            {
                Undo.RecordObject(script.BlackjackTables[i], "Pushed Values to Tables");
                Undo.RecordObject(script.BlackjackTables[i].Deck, "Pushed Values to Tables");
                script.BlackjackTables[i].UsingChips = tempUseChips[i];
                script.BlackjackTables[i].HelpersEnabled = tempUseHelpers[i];
                script.BlackjackTables[i].Deck.NumberOfDecks = tempDeckCount[i];
                script.BlackjackTables[i].CanChangeSettings = tempChangeSettings[i];
            }
            
            CreateCameraAssets(tables.Count-1);

            if (destroyIndex > -1)
            {
                destroyIndex = -1;
            }
            
            script.TableCameraMaterials = CameraMaterials.ToArray();
            script.TableRenderTextures = CameraRenderTextures.ToArray();

            FixTableNames();
            
            ApplyPrefabModifications();
        }

        private void ApplyPrefabModifications()
        {
            foreach (var table in script.BlackjackTables)
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(table);
                PrefabUtility.RecordPrefabInstancePropertyModifications(table.Deck);
            }
        }
    }

    [Serializable]
    public class BlackjackTableInstance
    {
        public Controller TableController;
        public bool UseChips;
        public bool UseHelpers;
        public int DeckCount = 2;
        public bool CanChangeSettingsDuringRuntime;
    }
}
