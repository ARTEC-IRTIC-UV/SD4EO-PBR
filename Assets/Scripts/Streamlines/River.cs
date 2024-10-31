using DefaultNamespace.Regions;
using UnityEngine;

public class River: StreamlineRegion
{
    public StreamlineRegion riverRegion;
    public StreamlineRegion leftRegion;
    public StreamlineRegion rightRegion;
    
    public River(StreamlineRegion riverRegion, StreamlineRegion leftRegion, StreamlineRegion rightRegion)
    {
        this.riverRegion = riverRegion;
        this.leftRegion = leftRegion;
        this.rightRegion = rightRegion;
    }
    
    public River()
    {
        this.riverRegion = null;
        this.leftRegion = null;
        this.rightRegion = null;
    }
    
    public StreamlineRegion getLeftRegion()
    {
        return leftRegion;
    }
    
    public StreamlineRegion getRightRegion()
    {
        return rightRegion;
    }
    
    public StreamlineRegion getRiverRegion()
    {
        return riverRegion;
    }
    
    
}