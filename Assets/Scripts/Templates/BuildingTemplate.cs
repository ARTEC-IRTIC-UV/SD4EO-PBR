using System;
using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace
{

    [Serializable]
    public class BuildingTemplate
    {
        [SerializeField] private List<int> indices;
        [SerializeField] private int initialIndex;
        [SerializeField] private BuildingType buildingType;

        private Mesh debugMesh;

        public Mesh DebugMesh
        {
            get => debugMesh;
            set
            {
                debugMesh = value;
            }
        }
        
        public Color GetBuildingColor()
        {
            Color c;
            
            switch (buildingType)
            {
                case BuildingType.Parking:
                    c = Color.gray;
                    break;
                case BuildingType.Vegetation:
                    c = Color.green;
                    break;
                case BuildingType.BuildingCornice:
                    c = Color.blue;
                    break;
                case BuildingType.BuildingRoof:
                    c = Color.red;
                    break;
                case BuildingType.CropField:
                    c = Color.yellow;
                    break;
                case BuildingType.Water:
                    c = Color.cyan;
                    break;
                case BuildingType.Street:
                    c = Color.black;
                    break;
                case BuildingType.Train:
                    c = new Color(151, 131, 90, 1);
                    break;
                case BuildingType.DefaultFloor:
                    c = Color.clear;
                    break;
                default:
                    c = Color.white;
                    break;
            }
            
            return c;
        }

        public BuildingTemplate(List<int> indices, int initialIndex, Mesh mesh = null)
        {
            this.indices = indices;
            this.initialIndex = initialIndex;
            this.debugMesh = mesh;
        }
        public List<int> GetIndicesList()
        {
            return indices;
        }
        
        public int GetInitialIndex()
        {
            return initialIndex;
        }
        
        public BuildingType GetBuildingType()
        {
            return buildingType;
        }
    }
}

