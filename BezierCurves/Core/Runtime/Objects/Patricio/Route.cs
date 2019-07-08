/*PATRICIO HIDALGO SANCHEZ. 2019*/

using System.Collections.Generic;
using UnityEngine;

public class Route
{
    private NodeNetCreator nodeNetCreator;
    private List<Node> nodes;

    bool upToDate = false;

    bool isClosed = false;

    //Suma acumulada de las longitudes de los Stretch en el orden en que estan almacenados sus nodes
    float[] _accumulativeLengths;
    public float[] AccumulativeLengths
    {
        get
        {
            if (!upToDate)
            {
                int aclLength = isClosed? nodes.Count : (nodes.Count - 1);
                _accumulativeLengths = new float[aclLength];
                float totalDistance = 0;

                for (int i = 0; i < _accumulativeLengths.Length; i++)
                {
                    totalDistance += GetStretch(i, true).Length;
                    _accumulativeLengths[i] = totalDistance;
                }

                upToDate = true;
            }
            return _accumulativeLengths;
        }
    }

    //Longitud total de la ruta
    public float Length
    {
        get
        {
            return AccumulativeLengths[AccumulativeLengths.Length - 1];
        }
    }

    #region Constructors
    public Route(NodeNetCreator nodeNetCreator, bool isClosed = false)
    {
        this.nodeNetCreator = nodeNetCreator;
        nodes = new List<Node>();
        this.isClosed = isClosed;
    }

    public Route(NodeNetCreator nodeNetCreator, List<Node> nodes, bool isClosed = false)
    {
        this.nodeNetCreator = nodeNetCreator;
        this.nodes = nodes;
        this.isClosed = false;
    }
    #endregion

    public void AddNode(Node node)
    {
        nodes.Add(node);
        upToDate = false;
    }

    public Vector3 GetPointAtDistance(float dst)
    {
        Vector3 point = Vector3.zero;

        dst = Mathf.Repeat(dst, Length);

        for (int i = 0; i < AccumulativeLengths.Length; i++)
        {
            if(dst < AccumulativeLengths[i])
            {
                if(i > 0)
                    dst -= AccumulativeLengths[i - 1];
                //point = GetStretch(i).GetPointAtDistance(dst);
                break;
            }
        }
        return point;
    }

    public Vector3 GetDirectionAtDistance(float dst)
    {
        Vector3 rotation = Vector3.zero;

        dst = Mathf.Repeat(dst, Length);

        for (int i = 0; i < AccumulativeLengths.Length; i++)
        {
            if (dst < AccumulativeLengths[i])
            {
                if(i > 0)
                    dst -= AccumulativeLengths[i - 1];
               // rotation = GetStretch(i).GetRotationAtDistance(dst);
                break;
            }
        }
        return rotation;
    }

    public Vector3 GetClosestPointOnRoute(Vector3 pos)
    {
        Vector3 point = Vector3.zero;
        float minDistance = Mathf.Infinity;

        for (int i = 0; i < nodes.Count; i++)
        {
            Vector3 p = GetStretch(i).GetClosestPointOnStretch(pos);
            float d = Vector3.Distance(pos, p);
            if (d < minDistance)
            {
                minDistance = d;
                point = p;
            }
        }
        return point;
    }

    #region Internal

    //Devuelve el tramo que conecta un node de la lista con el siguiente. El anterior si !forward
    private Stretch GetStretch(int nodeIndex, bool forward = true)
    {
        int x = nodes[LoopNodeIndex(nodeIndex)].GetID();
        nodeIndex = forward ? (nodeIndex + 1) : (nodeIndex - 1);
        int y = nodes[LoopNodeIndex(nodeIndex)].GetID();

        Vector2Int key = nodeNetCreator.GenerateKey(x, y);

        Stretch st = null;

        try
        {
            st = nodeNetCreator.stretches[key];
        }
        catch
        {
            Debug.LogAssertion(key);
        }

        return st;
    }

    private int LoopNodeIndex(int index)
    {
        return index % nodes.Count;
    }

    #endregion
}
