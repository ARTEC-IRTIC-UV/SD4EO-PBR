using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public class Streamline
{
    private List<Vector3> streamlinePoints;
    private int initialID;
    private int finalID;
    private float score;
    [SerializeField] private float streamlineWidth;
    [SerializeField] private StreamlineType type;


    public enum  StreamlineType
    {
        Street,
        Train,
        River
    }

    public enum StreamlineSide
    {
        Right,
        Left
    }

    public Streamline(List<Vector3> streamlinePoints, int initialID, int finalID, float score, StreamlineType type, float streamlineWidth)
    {
        this.streamlinePoints = streamlinePoints;
        this.initialID = initialID;
        this.finalID = finalID;
        this.score = score;
        this.type = type;
        this.streamlineWidth = streamlineWidth;
    }
    
    public List<Vector3> GetStreamlinePoints()
    {
        return streamlinePoints;
    }
    
    public void SetStreamlinePoints(List<Vector3> streamlinePoints)
    {
        this.streamlinePoints = streamlinePoints;
    }

    public int GetInitialID()
    {
        return initialID;
    }

    public void SetInitialID(int initialID)
    {
        this.initialID = initialID;
    }
    
    public int GetFinalID()
    {
        return finalID;
    }

    public void SetFinalID(int finalID)
    {
        this.finalID = finalID;
    }
    
    public float GetScore()
    {
        return score;
    }
    
    public void SetScore(float score)
    {
        this.score = score;
    }
    
    public StreamlineType GetStreamlineType()
    {
        return type;
    }
    
    public void SetStreamlineType(StreamlineType s)
    {
        this.type = s;
    }
    
    public float GetStreamlineWidth()
    {
        return streamlineWidth;
    }
    
    public void SetStreamlineWidth(float streamlineWidth)
    {
        this.streamlineWidth = streamlineWidth;
    }
}