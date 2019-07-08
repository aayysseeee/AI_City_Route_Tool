/*PATRICIO HIDALGO SANCHEZ. 2019*/

using System.Collections.Generic;
using UnityEngine;
using PathCreation;
using PathCreation.Utility;

[System.Serializable]
public class NodeNetCreator : MonoBehaviour
{
  [HideInInspector]
  public bool curveEditing;

  [SerializeField]
  public bool showLabels = false;

  private int childCount;

  [HideInInspector]
  public SerializableStretchesDictionary stretches;

  public Stretch TryCreateStretch(Node a, Node b)
  {
    if (a == null || b == null)
    {
      Debug.LogError("Trying to create a Stretch with one or two null gameobjects.");
      return null;
    }

    if (GetStretch(a, b) == null)
    {
      Vector2Int key = GenerateKey(a.name, b.name);
      stretches.Add(key, new Stretch(a, b));
      return stretches[key];
    }

    return null;
  }


  public void NotifyNodeDeleted(int id)
  {
    //Renaming nother below
    for (int i = id; i < transform.childCount; i++)
    {
      transform.GetChild(i).name = "NODE_" + i;
    }

    //Delete stretches using that node
    List<Vector2Int> keysToRemove = new List<Vector2Int>();

    foreach (Vector2Int key in stretches.Keys)
    {
      if (key.x == id || key.y == id)
      {
        keysToRemove.Add(key);
      }
    }

    for (int i = 0; i < keysToRemove.Count; i++)
    {
      stretches.Remove(keysToRemove[i]);
    }
  }

  public Vector2Int[] GetAllKeysFromNode(Node node)
  {
    List<Vector2Int> foundStretches = new List<Vector2Int>();
    int searchingIndex = TrimNodeIndex(node.name);

    foreach (Vector2Int key in stretches.Keys)
    {
      if (key.x == searchingIndex || key.y == searchingIndex)
      {
        foundStretches.Add(key);
      }
    }

    return foundStretches.ToArray();
  }

  public Stretch[] GetAllStretchesFromNode(Node node)
  {
    List<Stretch> foundStretches = new List<Stretch>();
    int searchingIndex = TrimNodeIndex(node.name);

    foreach (Vector2Int key in stretches.Keys)
    {
      if (key.x == searchingIndex || key.y == searchingIndex)
      {
        foundStretches.Add(stretches[key]);
      }
    }

    return foundStretches.ToArray();
  }

  //Metodo temporal para debug
  private Stretch GetFirstOrSecondStretch(Node node, Stretch excludedStretch = null, bool first = true)
  {
    int id = node.GetID();

    if (excludedStretch == null)
      foreach (Vector2Int key in stretches.Keys)
      {
        if (key.x == id || key.y == id)
        {
          Stretch st = stretches[key];
          if (node == st.anchorA)
            return stretches[key];
        }
      }

    else
      foreach (Vector2Int key in stretches.Keys)
      {
        Stretch st = stretches[key];

        if ((key.x == id || key.y == id) && st != excludedStretch)
        {
          return stretches[key];
        }
      }

    return null;
  }

  private Stretch lastDeletedStretch;
  public void PrepareDelete(Vector2Int key)
  {
    lastDeletedStretch = stretches[key];
  }

  public Stretch GetClosestStretch(Vector3 worldPos)
  {
    float minDistance = float.MaxValue;
    Vector2Int key = new Vector2Int(-1, -1);

    float dst;

    foreach (Vector2Int k in stretches.Keys)
    {
      dst = (worldPos - MathUtility.ClosestPointOnLineSegment(worldPos, stretches[k].anchorA.transform.position, stretches[k].ControlA)).sqrMagnitude;
      if (dst < minDistance)
      {
        minDistance = dst;
        key = k;
      }

      dst = (worldPos - MathUtility.ClosestPointOnLineSegment(worldPos, stretches[k].ControlA, stretches[k].ControlB)).sqrMagnitude;
      if (dst < minDistance)
      {
        minDistance = dst;
        key = k;
      }

      dst = (worldPos - MathUtility.ClosestPointOnLineSegment(worldPos, stretches[k].ControlB, stretches[k].anchorB.transform.position)).sqrMagnitude;
      if (dst < minDistance)
      {
        minDistance = dst;
        key = k;
      }
    }

    return stretches[key];
  }

  public Node GetClosestNode(Vector3 worldPos)
  {
    float minDistance = float.MaxValue;
    Node closestNode = null;

    foreach (Transform t in transform)
    {
      float d = (t.position - worldPos).sqrMagnitude;
      if (d < minDistance)
      {
        closestNode = t.GetComponent<Node>();
        minDistance = d;
      }
    }
    return closestNode;
  }

  private int TrimNodeIndex(string nodeName)
  {
    int index;
    string sub = nodeName.Substring(5);

    if (int.TryParse(sub, out index))
      return index;
    else
    {
      Debug.LogError("Not suitable node name: " + nodeName);
    }
    return -1;
  }

  ///Searches for a stretch connecting two nodes.
  public Stretch GetStretch(Node a, Node b)
  {
    Vector2Int key = GenerateKey(a.name, b.name);
    if (stretches.ContainsKey(key))
      return stretches[key];
    else
      return null;
  }

  public Vector2Int GenerateKey(string a, string b)
  {
    int aN = TrimNodeIndex(a);
    int bN = TrimNodeIndex(b);

    Vector2Int key = new Vector2Int(Mathf.Min(aN, bN), Mathf.Max(aN, bN));

    return key;
  }

  public Vector2Int GenerateKey(int a, int b)
  {
    Vector2Int key = new Vector2Int(Mathf.Min(a, b), Mathf.Max(a, b));

    return key;
  }

  private Node AreStretchesConnected(Stretch a, Stretch b)
  {
    if (a.anchorA == b.anchorA || a.anchorA == b.anchorB)
      return a.anchorA;
    if (a.anchorB == b.anchorA || a.anchorB == b.anchorB)
      return a.anchorB;

    return null;
  }

  public Node GetRandomNeighbour(Node targetNode, Node previous)
  {
    Vector2Int[] keys = GetAllKeysFromNode(targetNode);

    for (int i = 0; i < 800; i++)
    {
      int index = (int)Random.Range(0, keys.Length);
      Node n = GetOppositeNode(keys[index], targetNode);

      if (n != previous)
      {
        if (keys.Length > 1)
        {
          if (GetStretch(n, previous) == null)
            return n;
        }
        else
          return n;
      }

    }
    Debug.Log("FailRandom");
    for (int i = 0; i < keys.Length; i++)
    {
      Node n = GetOppositeNode(keys[i], targetNode);
      if (n != previous)
      {
        if (keys.Length > 1)
        {
          if (GetStretch(n, previous) == null)
            return n;
        }
        else
          return n;
      }
    }
    Debug.Log("fail todo");
    return targetNode;
  }

  private Node GetOppositeNode(Vector2Int key, Node node)
  {
    if (node.GetID() == key.x)
      return GetNode(key.y);
    else if(node.GetID() == key.y)
      return GetNode(key.x);

    Debug.LogWarning("OppositeNode not part of this stretch");
    return null;
  }

  private Node GetNode(int id)
  {
    Node node = transform.GetChild(id - 1).GetComponent<Node>();

    if (node.GetID() == id)
      return node;

    else
    {
      foreach (Transform child in transform)
      {
        Node n = child.GetComponent<Node>();

        if (n && n.GetID() == id)
          return n;
      }
    }

    return null;
  }
  //////////////POINTS MANAGEMENT////////////////////

  [SerializeField, HideInInspector]
  private List<Vector3> positions = new List<Vector3>();

  [SerializeField, HideInInspector]
  private List<int> positionsNRefs = new List<int>();

  public event System.Action<int> OnPointDeleted;

  public int NumPoints
  {
    get
    {
      return positions.Count;
    }
  }

  //Crear un nuevo punto y añadirlo a un nuevo indice en una coleccion
  public int CreatePoint(Vector3 pos)
  {
    positions.Add(pos);
    positionsNRefs.Add(1);
    return (positions.Count - 1);
  }

  public void NotifyRemoved(int pointRef)
  {
    DecrementNRefs(pointRef);
  }

  public int InsertNewPointRef(int index, Vector3 position)
  {
    positions.Add(position);
    positionsNRefs.Add(1);
    return (positions.Count - 1);
  }

  public void MovePointAt(int pointRef, Vector3 position)
  {
    positions[pointRef] = position;
  }

  public int CheckForClosePoint(Vector3 pos, float threeshold)
  {
    float minDistance = threeshold;
    int closestPointIndex = -1;

    for (int i = 0; i < positions.Count; i++)
    {
      float distance = Vector3.Distance(pos, positions[i]);
      if (distance < threeshold)
      {
        minDistance = distance;
        closestPointIndex = i;
      }
    }
    return closestPointIndex;
  }

  private void DecrementNRefs(int pointRef)
  {
    --positionsNRefs[pointRef];
    if (positionsNRefs[pointRef] <= 0)
    {
      positions.RemoveAt(pointRef);
      positionsNRefs.RemoveAt(pointRef);
      OnPointDeleted(pointRef);
    }
  }

  private void IncrementNRefs(int index)
  {
    ++positionsNRefs[index];
  }

  public Vector3 GetPointAt(int pointRef)
  {
    return positions[pointRef];
  }
}
