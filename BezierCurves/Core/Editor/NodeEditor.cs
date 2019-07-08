using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Node))]
public class NodeEditor : Editor
{
    Node node;
    NodeNetCreator creator;

    public override void OnInspectorGUI()
    {
        GUILayout.Space(20);

        node.Mirror = GUILayout.Toggle(node.Mirror, "mirror");

        GUILayout.Space(20);

        if(GUILayout.Button("Delete"))
        {
            GameObject[] children = new GameObject[creator.transform.childCount];
            for(int i = 0; i < children.Length; i++)
            {
                children[i] = creator.transform.GetChild(i).gameObject;
            }

            Undo.RegisterCompleteObjectUndo(creator, "Delete Stretches");
            Undo.RecordObjects(children, "names");
            node.NotifyDelete();
            Undo.DestroyObjectImmediate(node.gameObject);
        }
        GUILayout.Space(20);
    }


    private void OnSceneGUI()
    {
        if(Event.current.keyCode == KeyCode.Alpha1)
        {
            Selection.activeGameObject = node.transform.parent.gameObject;
        }
        DrawBezierCurves();
    }

    private void DrawBezierCurves()
    {
        foreach (Stretch st in creator.stretches.Values)
        {
            Vector3[] points = st.GetPoints();
            Handles.DrawBezier(points[0], points[1], points[2], points[3], Color.green, null, 1);
        }
    }


    private void OnEnable()
    {
        node = (Node)target;
        creator = node.transform.GetComponentInParent<NodeNetCreator>();
    }
}
