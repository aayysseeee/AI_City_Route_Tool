using UnityEngine;


public class Node : MonoBehaviour
{
  NodeNetCreator nnc;
  public NodeNetCreator NodeNetCreator
  {
    get
    {
      if (nnc == null)
      {
        nnc = transform.GetComponentInParent<NodeNetCreator>();
        if (nnc == null)
          Debug.LogError("Node " + name + " must be a child of NodeNetCreator object");
      }
      return nnc;
    }
  }

  [HideInInspector]
  public int nStretches = 0;

  [SerializeField]
  int id;

  [HideInInspector]
  private bool _mirror = false;
  public bool Mirror
  {
    get
    {
      return _mirror;
    }
    set
    {
      _mirror = (value && (nStretches == 2));
    }
  }

  public void InitializeNode(int id)
  {
    this.id = id;
  }

  public void NotifyDelete()
  {
    NodeNetCreator.NotifyNodeDeleted(id);
    Debug.Log(id);
  }

  public int GetID()
  {
    return id;
  }

  public void SetID(int id)
  {
    this.id = id;
  }
}
