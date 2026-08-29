using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VRC.Core;

namespace S0iRu.PipelineDetacher.Editor
{
    internal static class PipelineDetacher
    {
        const string GameObjectMenuPath = "GameObject/S0iRu/Detach Pipeline Manager";
        const string ToolsMenuPath = "Tools/S0iRu/Detach Pipeline Manager";

        // GameObject/ menu fires once per selected object; guard against that.
        static bool _called;

        [MenuItem(GameObjectMenuPath, false, 0)]
        static void ExecuteFromGameObjectMenu()
        {
            if (_called) return;
            _called = true;
            EditorApplication.delayCall += () => _called = false;
            Execute();
        }

        [MenuItem(GameObjectMenuPath, true)]
        static bool ValidateGameObjectMenu() => HasDetachablePipelineManager();

        [MenuItem(ToolsMenuPath, false)]
        static void ExecuteFromToolsMenu() => Execute();

        [MenuItem(ToolsMenuPath, true)]
        static bool ValidateToolsMenu() => HasDetachablePipelineManager();

        static bool HasDetachablePipelineManager()
        {
            return !EditorApplication.isPlaying && GetTargets().Any();
        }

        // Selected scene objects only (children are not included).
        // Already-empty blueprint IDs are skipped (same as Detach being a no-op).
        static PipelineManager[] GetTargets()
        {
            return Selection.gameObjects
                .Where(go => !EditorUtility.IsPersistent(go))
                .Select(go => go.GetComponent<PipelineManager>())
                .Where(pm => pm != null && !string.IsNullOrEmpty(pm.blueprintId))
                .ToArray();
        }

        static void Execute()
        {
            var targets = GetTargets();
            if (targets.Length == 0) return;

            Undo.RecordObjects(targets.Cast<Object>().ToArray(), "Detach Pipeline Manager");

            foreach (var pm in targets)
            {
                // Inspector "Detach (Optional)" / SDK Content Manager delete both only clear blueprintId.
                // completedSDKPipeline is obsolete and computed by the SDK — do not write it.
                pm.blueprintId = "";

                EditorUtility.SetDirty(pm);
                PrefabUtility.RecordPrefabInstancePropertyModifications(pm);
                EditorSceneManager.MarkSceneDirty(pm.gameObject.scene);
            }

            Debug.Log($"[S0iRu] Detached Blueprint ID from {targets.Length} PipelineManager(s): " +
                      string.Join(", ", targets.Select(pm => pm.gameObject.name)));
        }
    }
}
