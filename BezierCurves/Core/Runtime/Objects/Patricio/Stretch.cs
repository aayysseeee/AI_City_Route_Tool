/*PATRICIO HIDALGO SANCHEZ. 2019*/

using UnityEngine;
using PathCreation.Utility;

[System.Serializable]
public class Stretch
{
  private Vector3 md;
  public Vector3 MidPoint
  {
    get
    {
      return anchorA.transform.position + (anchorB.transform.position - anchorA.transform.position) * 0.5f;
    }
  }

  public Node anchorA;
  public Node anchorB;

  //ControlPoints are defined as offsets from anchors´ positions

  [SerializeField]
  private Vector3 cA;
  public Vector3 ControlA
  {
    get
    {
      return anchorA.transform.position + cA;
    }
    set
    {
      cA = value - anchorA.transform.position;
      pathModified = true;
    }
  }

  [SerializeField]
  private Vector3 cB;
  public Vector3 ControlB
  {
    get
    {
      return anchorB.transform.position + cB;
    }
    set
    {
      cB = value - anchorB.transform.position;
      pathModified = true;
    }
  }

  bool pathModified = true;

  [HideInInspector]
  int pathResolution = 100;

  private Vector3[] _vertexPath;
  public Vector3[] VertexPath
  {
    get
    {
      if (pathModified || _vertexPath == null || _vertexPath.Length == 0)
      {
        RecalculateVertices();
      }
      return _vertexPath;
    }
  }


  private float[] _accumulativeLength;
  public float[] AccumulativeLength
  {
    get
    {
      if (_accumulativeLength == null)
        RecalculateLength();

      return _accumulativeLength;
    }
  }

  public float Length
  {
    get
    {
      return AccumulativeLength[AccumulativeLength.Length - 1];
    }
  }

  //Rehacer esta vaina. En vez de tener un numero fijo de vertices, hacerlo en funcion de la longitud
  public void RecalculateVertices()
  {
    pathResolution = Mathf.Clamp(pathResolution, 1, int.MaxValue);
    _vertexPath = new Vector3[1000];

    float step = 0.001f;
    float t = 0;

    for (int i = 0; i < 1000; i++)
    {
      _vertexPath[i] = GetPoint(t);
      t += step;
    }

    pathModified = false;
  }

  public Vector3 GetClosestPointOnStretch(Vector3 worldPos)
  {
    Vector3 point;
    int closestVertex = 0;
    int secondClosestVertex = 0;

    float minDistance = float.MaxValue;
    float minSecondDistance = float.MaxValue;

    for (int i = 0; i < VertexPath.Length; i++)
    {
      float d = (VertexPath[i] - worldPos).sqrMagnitude;
      if (d < minDistance)
      {
        secondClosestVertex = closestVertex;
        closestVertex = i;

        minSecondDistance = minDistance;
        minDistance = d;
      }
    }

    point = MathUtility.ClosestPointOnLineSegment(worldPos, VertexPath[closestVertex], VertexPath[secondClosestVertex]);

    return point;
  }

  public Stretch(Node a, Node b)
  {
    anchorA = a;
    anchorB = b;

    cA = (anchorB.transform.position - anchorA.transform.position) * 0.333f;
    cB = (anchorA.transform.position - anchorB.transform.position) * 0.333f;

    pathResolution = 100;

    a.nStretches++;
    b.nStretches++;
  }

  public Node GetOpossiteNode(Node n)
  {
    if (n == anchorA)
      return anchorB;
    if (n == anchorB)
      return anchorA;

    return null;
  }

  public Vector3[] GetPoints()
  {
    Vector3[] points = new Vector3[4];

    points[0] = anchorA.transform.position;
    points[1] = anchorB.transform.position;
    points[2] = ControlA;
    points[3] = ControlB;

    return points;
  }

  private void RecalculateLength()
  {
    _accumulativeLength = new float[VertexPath.Length - 1];
    float totalDistance = 0;

    for (int i = 0; i < VertexPath.Length - 1; i++)
    {
      totalDistance += (VertexPath[i + 1] - VertexPath[i]).magnitude;
      AccumulativeLength[i] = totalDistance;
    }
  }

  private Vector3 QuadraticCurve(Vector3 a, Vector3 b, Vector3 c, float t)
  {
    Vector3 p0 = Vector3.Lerp(a, b, t);
    Vector3 p1 = Vector3.Lerp(b, c, t);
    return Vector3.Lerp(p0, p1, t);
  }

  public Vector3 GetPoint(float t)
  {
    Vector3 a = QuadraticCurve(anchorA.transform.position, ControlA, ControlB, t);
    Vector3 b = QuadraticCurve(ControlA, ControlB, anchorB.transform.position, t);
    return Vector3.Lerp(a, b, t);
  }

  //Distance should be in between 0 and this stretchs length
  public Vector3 GetPointAtDistance(float d, Node start)
  {
    float t = 0;

    if (start != anchorA && start != anchorB)
      Debug.LogError("Node start no es parte de este stretch");

    if(start == anchorA)
    {
      t = d / Length;
    }
    else if(start == anchorB)
    {
      t = 1 - (d / Length);
    }

    return GetPoint(t);
  }

  public Vector3 GetPointAtDistanceWithOvershoot(float d, Node start, Stretch nextStretch, ref bool overShoot)
  {
    float t = 0;

    if (start == anchorA)
    {
      t = d / Length;

      if(t > 1)
      {
        overShoot = true;
        return nextStretch.GetPointAtDistance(t - 1, this.GetOpossiteNode(start));
      }
    }
    else if (start == anchorB)
    {
      t = 1 - (d / Length);

      if(t < 0)
      {
        overShoot = true;
        return nextStretch.GetPointAtDistance(-t, GetOpossiteNode(start));
      }
    }
    overShoot = false;
    return GetPoint(t);
  }

  public Vector3 GetRotationAtDistance(float d, Node start)
  {
    float t = 0;

    if (start == anchorA)
    {
      t = d / Length;
    }
    else if (start == anchorB)
    {
      t = 1 - (d / Length);
    }

    Vector3 a = GetPoint(t);
    Vector3 b = GetPoint(t + 0.1f);

    return (b - a).normalized * 10;
  }
}
