using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

public class AmiliousManagerEditor : OdinMenuEditorWindow
{
    
    public static void OpenWindow()
    {
        GetWindow<AmiliousManagerEditor>().Show();
    }
    protected override OdinMenuTree BuildMenuTree()
    {
        OdinMenuTree tree = new OdinMenuTree();
        return tree;
    }
}
