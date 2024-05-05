using System;
using UnityEngine;
using UnityEngine.Serialization;

public class CircuitController : MonoBehaviour
{
    public float TotalLength { get; private set; }
    public Checkpoint[] Checkpoints { get; private set; }
    
    
    private LineRenderer _circuitPath;
    private Vector3[] _pathPos;
    private float[] _cumArcLength;
    private Transform[] _startPositions;
    

    private void Awake()
    {
        _circuitPath = GetComponent<LineRenderer>();
        _circuitPath.enabled = false;
        _startPositions = transform.Find("StartPos").GetComponentsInChildren<Transform>();

        var cParent = transform.Find("Checkpoints");
        Checkpoints = new Checkpoint[cParent.childCount];
        for (int i = 0; i < Checkpoints.Length; i++)
        {
            Checkpoints[i] = cParent.GetChild(i).GetComponent<Checkpoint>();
            Checkpoints[i].Index = i;
        }
        
        InitCircuitPath();
    }

    private void InitCircuitPath()
    {
        //obtiene en el array pathpos todos los puntos del linerenderer
        //y en el array cumArcLength todas las longitudes desde el punto 0 hasta el punto i
        //por tanto en la ultima posicion guarda la longitud total del linerenderer
        int numPoints = _circuitPath.positionCount;
        _pathPos = new Vector3[numPoints];
        _cumArcLength = new float[numPoints];
        _circuitPath.GetPositions(_pathPos);

        // Compute circuit arc-length
        _cumArcLength[0] = 0;

        for (int i = 1; i < _pathPos.Length; ++i)
        {
            float length = (_pathPos[i] - _pathPos[i - 1]).magnitude;
            _cumArcLength[i] = _cumArcLength[i - 1] + length;
        }

        TotalLength = _cumArcLength[_cumArcLength.Length - 1];
    }

    public Vector3 GetSegment(int idx)
    {
        if (idx < _pathPos.Length - 1) return _pathPos[idx + 1] - _pathPos[idx];
        else return _pathPos[0] - _pathPos[idx];
    }

    public Vector3 GetPoint(int idx) => _pathPos[idx];

    public Vector3 GetStartPos(int idx) => _startPositions[idx + 1].position;

    public float ComputeClosestPointArcLength(Vector3 posIn, out int segIdx, out Vector3 posProjOut, out float distOut)
    {
        int minSegIdx = 0;
        float minArcL = float.NegativeInfinity;
        float minDist = float.PositiveInfinity;
        Vector3 minProj = Vector3.zero;

        // Check segments for valid projections of the point
        for (int i = 0; i < _pathPos.Length - 1; ++i)
        {
            //vector que va de un punto i al siguiente
            var segment = GetSegment(i);
            Vector3 pathVec = segment.normalized;
            float segLength = segment.magnitude;

            //vector de un punto i al coche
            Vector3 carVec = (posIn - _pathPos[i]);
            float dotProd = Vector3.Dot(carVec, pathVec);

            if (dotProd < 0) //si es negativo el coche esta en el segmento previo al del punto i -> i+1
                continue;

            if (dotProd > segLength) //si es > a la longitud el coche esta en el segmento siguiente al del punto i -> i+1
                continue; // Passed

            Vector3 proj = _pathPos[i] + dotProd * pathVec; //posicion en el segmento
            float dist = (posIn - proj).magnitude;
            if (dist < minDist)
            {
                minDist = dist;
                minProj = proj;
                minSegIdx = i;
                minArcL = _cumArcLength[i] + dotProd;
            }
        }

        // If there was no valid projection check nodes
        if (float.IsPositiveInfinity(minDist)) //minDist == float.PositiveInfinity
        {
            for (int i = 0; i < _pathPos.Length - 1; ++i)
            {
                float dist = (posIn - _pathPos[i]).magnitude;
                if (dist < minDist)
                {
                    minDist = dist;
                    minSegIdx = i;
                    minProj = _pathPos[i];
                    minArcL = _cumArcLength[i];
                }
            }
        }

        segIdx = minSegIdx;
        posProjOut = minProj;
        distOut = minDist;

        return minArcL;
    }
}