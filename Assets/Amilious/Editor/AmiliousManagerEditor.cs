using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Amilious.ProceduralTerrain.Map;
using Amilious.ProceduralTerrain.Saving;
using Amilious.Threading;
using Amilious.ValueAdds;
using ICSharpCode.NRefactory.Ast;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector.Editor.Examples;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using Object = UnityEngine.Object;
using System.IO;
using System.Text;

public class AmiliousManagerEditor : OdinMenuEditorWindow
{
    [OnValueChanged("StateChange")]
    [LabelText("Manager View")]
    [LabelWidth(100)]
    [EnumToggleButtons]
    [ShowInInspector]
    private ManagerState managerState;
    private int  enumIndex   = 0;
    private bool treeRebuild = false;


    private DrawMapManager drawMapManager = new DrawMapManager();
    private DrawPlayer     drawPlayer     = new DrawPlayer();
    
    [MenuItem("Window/Amilious/View Amilious Manager #a")]
    public static void OpenWindow()
    {
        GetWindow<AmiliousManagerEditor>().Show();
    }

    protected override void OnGUI()
    {
        SirenixEditorGUI.Title("Amilious Manager", "A simple manager that handles the game", TextAlignment.Center, true);
        EditorGUILayout.Space();
        
        switch (managerState)
        {
            case ManagerState.MapManager:
                break;
            case ManagerState.Player:
                break;
            default:
                break;
        }
        
        EditorGUILayout.Space();
        base.OnGUI();
    }

    
    void StateChange() => treeRebuild = true;

    protected override void Initialize()
    {
        drawMapManager.FindMyObject();
        drawPlayer.FindMyObject();
    }
    
    
    protected override void DrawEditors()
    {
        if(treeRebuild && Event.current.type == EventType.Layout)
        {
            ForceMenuTreeRebuild();
            treeRebuild = false;
        }
        switch (managerState)
        {
            case ManagerState.MapManager:
                DrawEditor(enumIndex);
                break;
            case ManagerState.Player:
                DrawEditor(enumIndex);
                break;
            default:
                break;
        }
        
        DrawEditor((int) managerState);
    }

    protected override IEnumerable<object> GetTargets()
    {
        List<object> targets = new List<object>();
        targets.Add(drawMapManager);
        targets.Add(drawPlayer);
        targets.Add(base.GetTarget());

        enumIndex = targets.Count - 1;
        return targets;
    }

    protected override void DrawMenu()
    {
        switch (managerState)
        {
            case ManagerState.MapManager:
                break;
            case ManagerState.Player:
                break;
            default:
                break;
        }
    }

    protected override OdinMenuTree BuildMenuTree()
    {
        OdinMenuTree tree = new OdinMenuTree();
        return tree;
    }
}

public enum ManagerState
{
    MapManager,
    Player,
}

public class DrawSceneObject<T> where T : MonoBehaviour
{
    [Title("Universe Creator")]
    [ShowIf("@myObject != null")]
    [InlineEditor(InlineEditorObjectFieldModes.CompletelyHidden)]
    public T myObject;

    public void FindMyObject()
    {
        if (myObject == null)
            myObject = Object.FindObjectOfType<T>();
    }

    
    [ShowIf("@myObject != null")]
    [GUIColor(0.7f,1f,0.7f)]
    [ButtonGroup("Top Button", -1000)]
    void SelectSceneObject()
    {
        if (myObject != null)
            Selection.activeObject = myObject.gameObject;
    }

    private string objectName = $"Create {typeof(T).Name} Object";
    
    [ShowIf("@myObject == null")]
    [Button("$objectName")]
    private void CreateObject()
    {
        GameObject newManager = new GameObject {name = "New " + typeof(T).Name};
        myObject = newManager.AddComponent<T>();
    }
    
    [ShowIf("@myObject != null")]
    [GUIColor(1f,1f,0.7f)]
    [Button]
    private void DeleteObject()
    {
        GameObject newManager = new GameObject {name = "New " + typeof(T).Name};
        myObject = newManager.AddComponent<T>();
    }


}

public class DrawMapManager : DrawSceneObject<MapManager>
{
}

public class DrawPlayer : DrawSceneObject<FlyCamera>
{
}

