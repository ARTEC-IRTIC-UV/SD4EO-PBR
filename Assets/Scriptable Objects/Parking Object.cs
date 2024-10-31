using System.Collections.Generic;
using UnityEngine;

/*
 * Scriptable object to save information about the characteristics of the objects that
 * are going to represent the parkings
 */
[CreateAssetMenu(fileName = "Parking", menuName = "ScriptableObjects/Parking Object")]
public class ParkingObject : GeometryObject
{
    // ATTRIBUTES
    [SerializeField] private int numberOfCarsPerUnit = 3;
    [SerializeField] private bool flipNoneStreets;
    [SerializeField] private float vehicleRotation;
    [SerializeField] private Vector2 vehicleProbabilityMinMax = new Vector2(0.2f, 0.8f);
    [SerializeField] private List<Color> vehicleColors;
    [SerializeField] private Material vehicleMaterial;
    
    // GETTERS
    public int GetNumberOfCarsPerUnit()
    {
        return numberOfCarsPerUnit;
    }
    
    public bool GetFlipNoneStreets()
    {
        return flipNoneStreets;
    }
    
    public float GetVehicleRotation()
    {
        return vehicleRotation;
    }

    public Vector2 GetVehicleProbabilityMinMax()
    {
        return vehicleProbabilityMinMax;
    }

    public List<Color> GetVehicleColors()
    {
        return vehicleColors;
    }

    public Material GetVehicleMaterial()
    {
        return vehicleMaterial;
    }
}