using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/**
 * This script contains the functions used to render the maps generated with
 * different colors, materials, masks and resolutions
 */
public class RenderFunctions : MonoBehaviour
{
    // Main function for rendering, it calls the rest of rendering functions
    public static void RenderCaller()
    {
        RegionsController regionsController = RegionsController.GetInstance();
        
        int mainCameraResolution = regionsController.GetMainCameraRenderResolution();
        int mainCameraMetresPixelFactor = regionsController.GetMainCameraMetresPixelFactor();
        int zoomCamerasResolution = regionsController.GetZoomCamerasRenderResolution();
        int zoomCamerasMetresPixelFactor = regionsController.GetZoomCamerasMetresPixelFactor();
        
        string folderCropsName = Path.Combine(Application.persistentDataPath, "CropsFields");
        string folderCitiesName = Path.Combine(Application.persistentDataPath, "HumanSettlements");
        string folderSolarPanelsName = Path.Combine(Application.persistentDataPath, "SolarPanelsMask");
        string folderBuildingsName = Path.Combine(Application.persistentDataPath, "BuildingsMask");

        Material denoiseMaterial = regionsController.GetDenoiseMaterial();

        // Check if the crops folder already exists
        if (!Directory.Exists(folderCropsName))
            Directory.CreateDirectory(folderCropsName);
        
        // Check if the cities folder already exists
        if (!Directory.Exists(folderCitiesName))
            Directory.CreateDirectory(folderCitiesName);
        
        // Check if the solar panels folder already exists
        if (!Directory.Exists(folderSolarPanelsName))
            Directory.CreateDirectory(folderSolarPanelsName);
        
        // Check if the buildings folder already exists
        if (!Directory.Exists(folderBuildingsName))
            Directory.CreateDirectory(folderBuildingsName);

        // For crop fields, we render an image for each month
        if (regionsController.GetCropFields())
            RenderMultipleMonths(folderCropsName, mainCameraResolution);
        else    // For cities, we can render images of multiple types
        {
            string renderNumber = regionsController.GetRenderInitialNumber();
            Camera mainCamera = regionsController.GetRenderMainCamera();
            Camera[] zoomCameras = regionsController.GetRenderZoomCameras();
            regionsController.UpdateCropFieldTexture(CropFieldTexture.Color);
            OtherFunctions.ChangeCropFieldsMonth(Month.April, true);
            
            if (regionsController.GetRenderB2B3B4())
            {
                RenderCameraToImage(mainCamera, folderCitiesName, regionsController.GetRenderInitialNumber() + "_" + mainCameraMetresPixelFactor + "m_B2B3B4", denoiseMaterial, mainCameraResolution);

                for (int i = 0; i < zoomCameras.Length; i++)
                {
                    string name = renderNumber + "_" + (i + 1) + "_" + zoomCamerasMetresPixelFactor + "m_B2B3B4";
                    RenderCameraToImage(zoomCameras[i], folderCitiesName, name, denoiseMaterial, zoomCamerasResolution);
                }
            }

            if (regionsController.GetRenderB8())
            {
                ChangeShaderValue("ARTEC/StandardVertexColor", "_RenderB8", 1f);
                RenderCameraToImage(mainCamera, folderCitiesName, regionsController.GetRenderInitialNumber() + "_" + mainCameraMetresPixelFactor + "m_B8", denoiseMaterial, mainCameraResolution);
                ChangeShaderValue("ARTEC/StandardVertexColor", "_RenderB8", 0f);
            }

            if (regionsController.GetRenderLayers())
            {
                ChangeMaterials(false);
                
                string layersName = renderNumber + "_" + mainCameraMetresPixelFactor + "m_Layers";
                RenderCameraToImage(mainCamera, folderCitiesName, layersName, denoiseMaterial, mainCameraResolution);

                for (int i = 0; i < zoomCameras.Length; i++)
                {
                    string name = renderNumber + "_" + (i + 1) + "_" + zoomCamerasMetresPixelFactor + "m_Layers";
                    RenderCameraToImage(zoomCameras[i], folderCitiesName, name, denoiseMaterial, zoomCamerasResolution);
                }
                
                ChangeMaterials(true);
            }

            if (regionsController.GetRenderSolarPanelsMask())
            {
                ChangeSolarPanelsMaterials(false);
                
                string solarName = renderNumber + "_0_" + mainCameraMetresPixelFactor + "m_SolarPanels";
                RenderCameraToImage(mainCamera, folderSolarPanelsName, solarName, denoiseMaterial, mainCameraResolution);

                for (int i = 0; i < zoomCameras.Length; i++)
                {
                    string name = renderNumber + "_" + (i + 1) + "_" + zoomCamerasMetresPixelFactor + "m_SolarPanels";
                    RenderCameraToImage(zoomCameras[i], folderSolarPanelsName, name, denoiseMaterial, zoomCamerasResolution);
                }
                
                ChangeSolarPanelsMaterials(true);
            }
            
            if (regionsController.GetRenderBuildingsMask())
            {
                ChangeBuildingsMaterials(false);
                string buildingsName = renderNumber + "_" + mainCameraMetresPixelFactor + "m_Buildings";
                RenderCameraToImage(mainCamera, folderBuildingsName, buildingsName, denoiseMaterial, mainCameraResolution);

                for (int i = 0; i < zoomCameras.Length; i++)
                {
                    string name = renderNumber + "_" + (i + 1) + "_" + zoomCamerasMetresPixelFactor + "m_Buildings";
                    RenderCameraToImage(zoomCameras[i], folderBuildingsName, name, denoiseMaterial, zoomCamerasResolution);
                }
                
                ChangeBuildingsMaterials(true);
            }
        }

        if (regionsController.GetMakeGeojson())
        {
            string geojsonName = regionsController.GetRenderInitialNumber() + "_Geojson";
            if (regionsController.GetCropFields())
                GeoJSON.SaveToJson(regionsController.GetFinalBuildingsRegionsList(), folderCropsName, geojsonName);
            else
                GeoJSON.SaveToJson(regionsController.GetFinalBuildingsRegionsList(), folderCitiesName, geojsonName);
        }

        regionsController.NextInitialNumber();
        
        Debug.Log("Images & JSON exported");
    }

    // Function to render an image and export it to an external file
    private static void RenderCameraToImage(Camera cam, string folder, string name, Material denoiseMaterial, int resolution)
    {
        // Create a new render texture
        RenderTexture renderTexture = new RenderTexture((int)(resolution * cam.aspect), resolution, 24);

        // Set the camera to render to the render texture
        cam.targetTexture = renderTexture;

        // Render the camera
        cam.Render();

        // Create a new texture 2D
        Texture2D texture2D = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);

        // Read the pixels from the render texture
        RenderTexture.active = renderTexture;
        texture2D.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);

        // Reset the active render texture
        cam.targetTexture = null;
        RenderTexture.active = null;

        RenderTexture finalRenderTexture;
        if (denoiseMaterial != null)
        {
            finalRenderTexture = new RenderTexture(resolution, resolution, 24);
            Vector4 textureSizes = new Vector4(
                1.0f / renderTexture.width,
                1.0f / renderTexture.height,
                1.0f / finalRenderTexture.width,
                1.0f / finalRenderTexture.height);

            denoiseMaterial.SetVector("_TextureSizes", textureSizes);
            Graphics.Blit(renderTexture, finalRenderTexture, denoiseMaterial);
        }
        else
            finalRenderTexture = renderTexture;

        // Create a new texture 2D
        Texture2D texture2DDownsampling =
            new Texture2D(finalRenderTexture.width, finalRenderTexture.height, TextureFormat.RGB24, false);

        // Read the pixels from the render texture
        RenderTexture.active = finalRenderTexture;
        texture2DDownsampling.ReadPixels(new Rect(0, 0, finalRenderTexture.width, finalRenderTexture.height), 0, 0);
        
        // Save the texture to a file
        string path = folder + "/" + name + ".png";
        byte[] bytes = texture2DDownsampling.EncodeToPNG();
        File.WriteAllBytes(path, bytes);
    }
    
    // Function to render an image per month when we render a crop field
    private static void RenderMultipleMonths(string folderPath, int resolution)
    {
        RegionsController regionsController = RegionsController.GetInstance();

        // Check if the folder already exists
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);
        
        if (regionsController.GetRenderSar())
        {
            ChangeShaderValue("ARTEC/StandardVertexColor", "_RenderB8", 1f);
            RenderCropField(CropFieldTexture.SAR, "SAR", folderPath, resolution);
            ChangeShaderValue("ARTEC/StandardVertexColor", "_RenderB8", 0f);
        }
        
        if (regionsController.GetRenderB2B3B4())
        {
            ChangeShaderValue("ARTEC/StandardVertexColor", "_RenderB8", 0f);
            RenderCropField(CropFieldTexture.Color, "B2B3B4", folderPath, resolution);
            ChangeShaderValue("ARTEC/StandardVertexColor", "_RenderB8", 0f);
        }
        
        if (regionsController.GetRenderB8())
        {
            ChangeShaderValue("ARTEC/StandardVertexColor", "_RenderB8", 1f);
            RenderCropField(CropFieldTexture.B08, "B8", folderPath, resolution);
            ChangeShaderValue("ARTEC/StandardVertexColor", "_RenderB8", 0f);
        }
        
        if (regionsController.GetRenderB11())
        {
            ChangeShaderValue("ARTEC/StandardVertexColor", "_RenderB8", 1f);
            RenderCropField(CropFieldTexture.B11, "B11", folderPath, resolution);
            ChangeShaderValue("ARTEC/StandardVertexColor", "_RenderB8", 0f);
        }
    }

    // Function to render a crop field
    private static void RenderCropField(CropFieldTexture cft, string typeName, string folderName, int resolution)
    {
        RegionsController regionsController = RegionsController.GetInstance();
        regionsController.UpdateCropFieldTexture(cft);
        bool firstChange = true;
        
        foreach (Month mo in Enum.GetValues(typeof(Month)))
        {
            OtherFunctions.ChangeCropFieldsMonth(mo, firstChange);
            RenderCameraToImage(regionsController.GetRenderMainCamera(), folderName, regionsController.GetRenderInitialNumber() + "_" + regionsController.GetMainCameraMetresPixelFactor() + "m_" + typeName + "_" + StringFunctions.GetMonthNumberAsString(mo.ToString()) + "_" + mo, regionsController.GetDenoiseMaterial(), resolution);
            
            if (firstChange)
                firstChange = false;
        }
    }
    
    // Function to change the materials for rendering mask layers
    private static void ChangeMaterials(bool original)
    {
        if (original)
        {
            RestoreOriginalMaterials();
        }
        else
        {
            StoreOriginalMaterials();
            ReplaceMaterials();
        }
    }
    
    // Function to change the materials for rendering mask of solar panels
    private static void ChangeSolarPanelsMaterials(bool original)
    {
        if (original)
        {
            RestoreOriginalMaterials();
        }
        else
        {
            StoreOriginalMaterials();
            ReplaceSolarPanelsMaterials();
        }
    }
    
    // Function to change the materials for rendering mask of buildings
    private static void ChangeBuildingsMaterials(bool original)
    {
        if (original)
        {
            RestoreOriginalMaterials();
        }
        else
        {
            StoreOriginalMaterials();
            ReplaceBuildingsMaterials();
        }
    }
    
    // Function to delete the values of the original materials saved previously
    private static void RestoreOriginalMaterials()
    {
        RegionsController regionsController = RegionsController.GetInstance();
        Dictionary<Renderer, Material[]> originalMaterials = regionsController.GetOriginalMaterials();

        foreach (var entry in originalMaterials)
        {
            entry.Key.sharedMaterials = entry.Value;
        }

        originalMaterials.Clear();
    }

    // Function to reset the materials to the original ones
    private static void StoreOriginalMaterials()
    {
        RegionsController regionsController = RegionsController.GetInstance();
        
        foreach (var renderer in FindObjectsOfType<Renderer>())
        {
            regionsController.GetOriginalMaterials()[renderer] = renderer.sharedMaterials;
        }
    }

    // Function to replace the materials in order to render a mask of layers
    private static void ReplaceMaterials()
    {
        RegionsController regionsController = RegionsController.GetInstance();
        
        foreach (var renderer in FindObjectsOfType<Renderer>())
        {
            Material[] newMaterials = new Material[renderer.sharedMaterials.Length];
            for (int i = 0; i < renderer.sharedMaterials.Length; i++)
            {
                switch (renderer.gameObject.tag)
                {
                    case "Residential Building":
                        newMaterials[i] = regionsController.GetRenderResidentialBuildingMaterial();
                        break;
                    case "Non Residential Building":
                        newMaterials[i] = regionsController.GetRenderNonResidentialBuildingMaterial();
                        break;
                    case "Residential Zone":
                        newMaterials[i] = regionsController.GetRenderResidentialMaterial();
                        break;
                    case "Industrial Zone":
                        newMaterials[i] = regionsController.GetRenderIndustrialMaterial();
                        break;
                    case "Water":
                        newMaterials[i] = regionsController.GetRenderRiverMaterial();
                        break;
                    case "Vegetation":
                        newMaterials[i] = regionsController.GetRenderVegetationMaterial();
                        break;
                    case "Road":
                        newMaterials[i] = regionsController.GetRenderRoadMaterial();
                        break;
                    case "Highway":
                        newMaterials[i] = regionsController.GetRenderHighwayMaterial();
                        break;
                    case "Railway":
                        newMaterials[i] = regionsController.GetRenderRailwayMaterial();
                        break;
                    case "AlfalfaOrLucerne":
                        newMaterials[i] = regionsController.GetRenderAlfalfaOrLucerneMaterial();
                        break;
                    case "Barley":
                        newMaterials[i] = regionsController.GetRenderBarleyMaterial();
                        break;
                    case "FallowAndBareSoil":
                        newMaterials[i] = regionsController.GetRenderFallowAndBareSoilMaterial();
                        break;
                    case "Oats":
                        newMaterials[i] = regionsController.GetRenderOatsMaterial();
                        break;
                    case "OtherGrainLeguminous":
                        newMaterials[i] = regionsController.GetRenderOtherGrainLeguminousMaterial();
                        break;
                    case "Peas":
                        newMaterials[i] = regionsController.GetRenderPeasMaterial();
                        break;
                    case "Sunflower":
                        newMaterials[i] = regionsController.GetRenderSunflowerMaterial();
                        break;
                    case "Vetch":
                        newMaterials[i] = regionsController.GetRenderVetchMaterial();
                        break;
                    case "Wheat":
                        newMaterials[i] = regionsController.GetRenderWheatMaterial();
                        break;
                    default:
                        newMaterials[i] = renderer.sharedMaterials[i]; // Keep the original material if no tag matches
                        break;
                }
            }

            renderer.sharedMaterials = newMaterials;
        }
    }
    
    // Function to replace the materials in order to render a mask of solar panels
    private static void ReplaceSolarPanelsMaterials()
    {
        RegionsController regionsController = RegionsController.GetInstance();
        
        foreach (var renderer in FindObjectsOfType<Renderer>())
        {
            Material[] newMaterials = new Material[renderer.sharedMaterials.Length];
            for (int i = 0; i < renderer.sharedMaterials.Length; i++)
            {
                switch (renderer.gameObject.name)
                {
                    case "Solar Panels":
                        newMaterials[i] = regionsController.GetRenderWhiteMaterial();
                        break;
                    default:
                        newMaterials[i] = regionsController.GetRenderBlackMaterial();
                        break;
                }
            }

            renderer.sharedMaterials = newMaterials;
        }
    }
    
    // Function to replace the materials in order to render a mask of buildings
    private static void ReplaceBuildingsMaterials()
    {
        RegionsController regionsController = RegionsController.GetInstance();
        
        foreach (var renderer in FindObjectsOfType<Renderer>())
        {
            Material[] newMaterials = new Material[renderer.sharedMaterials.Length];
            for (int i = 0; i < renderer.sharedMaterials.Length; i++)
            {
                switch (renderer.gameObject.tag)
                {
                    case "Residential Building":
                        newMaterials[i] = regionsController.GetRenderRedMaterial();
                        break;
                    case "Non Residential Building":
                        newMaterials[i] = regionsController.GetRenderGreenMaterial();
                        break;
                    default:
                        newMaterials[i] = regionsController.GetRenderBlackMaterial();
                        break;
                }
            }

            renderer.sharedMaterials = newMaterials;
        }
    }

    // Function to change the value of the shader used to render infrared maps
    private static void ChangeShaderValue(string shaderName, string variableName, float value)
    {
        // Find all renderers in the scene
        Renderer[] renderers = FindObjectsOfType<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            // Check each material in the renderer
            foreach (Material mat in renderer.sharedMaterials)
            {
                if (mat != null && mat.shader != null && mat.shader.name == shaderName)
                {
                    mat.SetFloat(variableName, value);
                }
            }
        }
    }
}