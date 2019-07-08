/*PATRICIO HIDALGO SANCHEZ. 2019*/

using System.Linq;
using UnityEngine;
using UnityEditor;
using PathCreationEditor;
using PathCreation;


[CustomEditor(typeof(NodeNetCreator))]
public class NodeNetEditor : Editor
{
  NodeNetCreator creator;

  int nodeLayerIndex;

  Node selectedNode;

  float lastClickTime = 0;

  Vector3Int selectedControlPoint;

  [HideInInspector]
  public int prevSelectedToolbar = 0;
  [HideInInspector]
  public int selectedToolbar = 0;
  [HideInInspector]
  public string[] toolbarButtons = new string[]
  {
        "Node Edition",
        "Curve Edition"
  };

  enum ToolbarOptions
  {
    NodeEdition = 0,
    CurveEdition = 1
  }

  GUIStyle labelStyle = new GUIStyle();


  public override void OnInspectorGUI()
  {
    base.OnInspectorGUI();

    GUILayout.Space(20);

    prevSelectedToolbar = selectedToolbar;
    selectedToolbar = GUILayout.Toolbar(selectedToolbar, toolbarButtons);

    creator.curveEditing = selectedToolbar == 1;
    if (prevSelectedToolbar != selectedToolbar)
    {
      SceneView.RepaintAll();
      Repaint();
    }


    GUILayout.Space(40);

    if (selectedToolbar == 0)
    {
      if (GUILayout.Button("Stick Nodes to Ground"))
      {
        StickNodesToGround();
      }
      if (GUILayout.Button("Reassign IDs"))
      {
        for (int i = 0; i < creator.transform.childCount; i++)
        {
          int id = i + 1;
          Transform child = creator.transform.GetChild(i);
          child.GetComponent<Node>().SetID(id);
          child.name = "NODE_" + id;
        }

        SerializableStretchesDictionary stretches = new SerializableStretchesDictionary();

        for (int i = 0; i < creator.stretches.Count; i++)
        {
          Vector2Int key = creator.GenerateKey(creator.stretches.ElementAt(i).Value.anchorA.GetID(),
            creator.stretches.ElementAt(i).Value.anchorB.GetID());
           stretches.Add(key, creator.stretches.ElementAt(i).Value);
          Debug.Log(key);
        }

        creator.stretches = stretches;
      }
    }
  }

  public void OnSceneGUI()
  {
    Event e = Event.current;
    bool elementClicked = false;

    #region ProcessClickEvent
    DrawAnchorPoints(ref elementClicked);

    if (creator.curveEditing && !elementClicked)
      DrawControlPoints(ref elementClicked);

    if (e.type == EventType.MouseDown && e.button == 0 && !elementClicked)
    {
      selectedNode = null;
      if (e.shift)
      {
        Vector3 mouseClickPos = MouseUtility.MouseToFloorRaycast();
        GameObject o = new GameObject();
        Node node = o.AddComponent<Node>();
        o.transform.SetParent(creator.transform);
        int id = creator.transform.childCount;
        node.InitializeNode(id);
        node.transform.position = mouseClickPos;
        node.name = "NODE_" + id;
      }
    }

    else if (e.type == EventType.Layout)
    {
      HandleUtility.AddDefaultControl(0);
    }
    #endregion

    if (selectedToolbar == (int)ToolbarOptions.NodeEdition)
    {
      if (selectedNode != null)
      {
        Ray mouseRay = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        Vector3 rayEnd = mouseRay.GetPoint(Camera.current.nearClipPlane);
        Handles.DrawLine(selectedNode.transform.position, rayEnd);
        SceneView.RepaintAll();
      }
      DeleteStretchesButtons();
    }

    DrawBeziers();

    DrawStretches();

    if (creator.showLabels)
      DrawLabels();
  }

  private void DrawStretches()
  {
    foreach (Stretch s in creator.stretches.Values)
    {
      Handles.DrawLine(s.anchorA.transform.position, s.anchorB.transform.position);
    }
  }

  private void DrawAnchorPoints(ref bool elementClicked)
  {
    float buttonRadius = 1;

    Handles.color = Color.green;

    foreach (Transform t in creator.transform)
    {
      Node current = t.GetComponent<Node>();

      switch (selectedToolbar)
      {
        //Crear un stretch
        case (int)ToolbarOptions.NodeEdition:

          if (Handles.Button(t.position, Quaternion.identity, buttonRadius, buttonRadius, Handles.SphereHandleCap))
          {
            if (selectedNode == current)
            {
              float clickTime = Time.time;
              if ((clickTime - lastClickTime) < 0.2f)
              {
                Selection.activeGameObject = t.gameObject;
              }
            }
            if (selectedNode != null)
            {
              Undo.RecordObject(creator, "Stretch added");
              Stretch st = creator.TryCreateStretch(selectedNode, current);
            }
            selectedNode = current;
            elementClicked = true;

            lastClickTime = Time.time;
          }

          break;
        //Seleccionar un nodo para moverlo
        case (int)ToolbarOptions.CurveEdition:

          if (current != selectedNode)
          {
            if (Handles.Button(t.position, Quaternion.identity, buttonRadius, buttonRadius, Handles.SphereHandleCap))
            {
              selectedNode = current;
              Selection.activeGameObject = current.gameObject;
              elementClicked = true;
            }
          }

          break;

        default:

          Debug.LogError("NodeNetEditor: Invalid value of 'selectedToolbar': " + selectedToolbar);

          break;
      }
    }
  }

  private void DrawControlPoints(ref bool elementClicked)
  {
    float buttonRadius = 0.5f;

    Handles.color = Color.blue;

    //Por cada stretch
    foreach (Vector2Int k in creator.stretches.Keys)
    {
      Stretch current = creator.stretches[k];

      if (k == new Vector2Int(selectedControlPoint.x, selectedControlPoint.y))
      {
        //current.ControlA is the selected one
        if (selectedControlPoint.z == 0)
        {
          Vector3 cAPos = current.ControlA;
          Quaternion cARot = Quaternion.LookRotation(Camera.current.transform.position - cAPos);

          EditorGUI.BeginChangeCheck();
          Vector3 aPos = Handles.PositionHandle(cAPos, Quaternion.identity);
          if (EditorGUI.EndChangeCheck())
          {
            Undo.RecordObject(creator, "Move ControlA");
            EditorUtility.SetDirty(creator);

            current.ControlA = aPos;

            if (current.anchorA.Mirror)
            {
              foreach (Stretch st in creator.GetAllStretchesFromNode(current.anchorA))
              {
                if (st != current)
                {
                  if (st.anchorA == current.anchorA)
                  {
                    st.ControlA = 2 * current.anchorA.transform.position - current.ControlA;
                  }
                  else if (st.anchorB == current.anchorA)
                  {
                    st.ControlB = 2 * current.anchorA.transform.position - current.ControlA;
                  }
                }
              }
            }
          }

          Vector3 cBPos = current.ControlB;

          if (Handles.Button(cBPos, Quaternion.identity, buttonRadius, buttonRadius, Handles.SphereHandleCap))
          {
            selectedControlPoint = new Vector3Int(k.x, k.y, 1);
            elementClicked = true;
            SceneView.RepaintAll();
          }
        }
        //current.ControlB is the selected one
        else if (selectedControlPoint.z == 1)
        {
          Vector3 cAPos = current.ControlA;

          if (Handles.Button(cAPos, Quaternion.identity, buttonRadius, buttonRadius, Handles.SphereHandleCap))
          {
            selectedControlPoint = new Vector3Int(k.x, k.y, 0);
            elementClicked = true;
            SceneView.RepaintAll();
          }

          Vector3 cBPos = current.ControlB;

          EditorGUI.BeginChangeCheck();
          Vector3 bPos = Handles.PositionHandle(cBPos, Quaternion.identity);
          if (EditorGUI.EndChangeCheck())
          {
            Undo.RecordObject(creator, "Move ControlB");
            EditorUtility.SetDirty(creator);

            current.ControlB = bPos;

            if (current.anchorB.Mirror)
            {
              foreach (Stretch st in creator.GetAllStretchesFromNode(current.anchorB))
              {
                if (st != current)
                {
                  if (st.anchorA == current.anchorB)
                  {
                    st.ControlA = 2 * current.anchorB.transform.position - current.ControlB;
                  }
                  else if (st.anchorB == current.anchorB)
                  {
                    st.ControlB = 2 * current.anchorB.transform.position - current.ControlB;
                  }
                }
              }
            }
          }
        }
      }

      else
      {
        Vector3 cAPos = current.ControlA;

        if (Handles.Button(cAPos, Quaternion.identity, buttonRadius, buttonRadius, Handles.SphereHandleCap))
        {
          selectedControlPoint = new Vector3Int(k.x, k.y, 0);
          SceneView.RepaintAll();
        }

        Vector3 cBPos = current.ControlB;

        if (Handles.Button(cBPos, Quaternion.identity, buttonRadius, buttonRadius, Handles.SphereHandleCap))
        {
          selectedControlPoint = new Vector3Int(k.x, k.y, 1);
          SceneView.RepaintAll();
        }
      }

      Handles.DrawLine(current.ControlA, current.anchorA.transform.position);
      Handles.DrawLine(current.ControlB, current.anchorB.transform.position);
    }
  }

  private void DrawBeziers()
  {
    Handles.color = Color.white;
    foreach (Stretch st in creator.stretches.Values)
    {
      Vector3[] points = st.GetPoints();
      Handles.DrawBezier(points[0], points[1], points[2], points[3], Color.green, null, 1);
    }
  }

  private void DrawLabels()
  {
    foreach (Transform t in creator.transform)
    {
      Handles.Label(t.transform.position + Vector3.up * 2, t.name, labelStyle);
    }

    foreach (Stretch st in creator.stretches.Values)
    {
      Handles.Label(st.MidPoint, "Stretch_" + st.anchorA.GetID() + "_" + st.anchorB.GetID(), labelStyle);
    }
  }

  private void StickNodesToGround()
  {
    for (int i = 0; i < creator.transform.childCount; i++)
    {
      Transform node = creator.transform.GetChild(i);
      Vector3 rayStartOffset = new Vector3(0, 2, 0);
      Ray nodeRay = new Ray(node.position + rayStartOffset, Vector3.down);

      RaycastHit hit;

      if (Physics.Raycast(nodeRay, out hit, Mathf.Infinity, nodeLayerIndex))
      {
        node.position = hit.point;
      }
    }
  }

  private void DeleteStretchesButtons()
  {
    Handles.color = Color.red;
    for (int i = 0; i < creator.stretches.Count; i++)
    {
      Stretch st = creator.stretches.ElementAt(i).Value;
      Vector3 pos = st.MidPoint;
      Quaternion rot = Quaternion.LookRotation((Camera.current.transform.position - pos).normalized, Vector3.up);
      if (Handles.Button(pos, rot, 1, 1, Handles.SphereHandleCap))
      {
        Vector2Int key = creator.stretches.ElementAt(i).Key;
        creator.PrepareDelete(key);
        Undo.RegisterCompleteObjectUndo(creator, "Deleted Stretch");
        creator.stretches.Remove(key);

        GetNode(key.x).nStretches--;
        GetNode(key.y).nStretches--;
      }
    }
  }

  private Node GetNode(int id)
  {
    return creator.transform.GetChild(id - 1).GetComponent<Node>();
  }

  private void OnEnable()
  {
    creator = (NodeNetCreator)target;

    nodeLayerIndex = LayerMask.NameToLayer("Node");
    if (nodeLayerIndex == -1)
      Debug.LogError("NodeNetEditor necesita una capa llamada \"Node\". Crea una weputa");

    if (creator.stretches != null && creator.stretches.Count > 0)
    {
      foreach (Stretch st in creator.stretches.Values)
      {
        st.RecalculateVertices();
      }
    }

    labelStyle.alignment = TextAnchor.MiddleCenter;
    labelStyle.fontSize = 30;
    labelStyle.fontStyle = FontStyle.Bold;
  }
}
