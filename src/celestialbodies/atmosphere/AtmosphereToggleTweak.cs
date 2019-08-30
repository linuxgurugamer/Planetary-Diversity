using PlanetaryDiversity.API;
using System;
using System.Collections.Generic;
using PlanetaryDiversity.Components;
using UnityEngine;
using Gradient = PlanetaryDiversity.Components.Gradient;
//using Report = PlanetaryDiversity.Report;

namespace PlanetaryDiversity.CelestialBodies.Atmosphere
{
    /// <summary>
    /// A tweak that removes or adds atmospheres to bodies
    /// </summary>
    public class AtmosphereToggleTweak : CelestialBodyTweaker
    {
        /// <summary>
        /// Returns the name of the config node that stores the configuration
        /// </summary>
        public override String GetConfig() => "PD_CELESTIAL";

        /// <summary>
        /// Returns the name of the config option that can be used to disable the tweak
        /// </summary>
        public override String GetSetting() => "AtmosphereToggle";

        /// <summary>
        /// A list of bodies that should get their atmosphere restored
        /// </summary>
        private static readonly List<CelestialBody> toRestore = new List<CelestialBody>();

        /// <summary>
        /// A list of bodies that should get their new atmosphere removed
        /// </summary>
        private static readonly List<CelestialBody> toDelete = new List<CelestialBody>();

        /// <summary>
        /// Changes the parameters of the body
        /// </summary>
        public override Boolean Tweak(CelestialBody body)
        {
            // Is the body a GasPlanet or a Star?
            if (!body.hasSolidSurface || body.scaledBody.GetComponentsInChildren<SunShaderController>().Length != 0)
                return false;
            
            // Is the body our homeworld?
            if (body.isHomeWorld)
                return false;
            
            // Does the body have an ocean? This could end badly
            if (body.ocean)
                return false;
            
            // Process previous states
            if (toRestore.Contains(body))
            {
                // Enable the Atmosphere from Ground
                AtmosphereFromGround[] afgs = body.GetComponentsInChildren<AtmosphereFromGround>();
                foreach (AtmosphereFromGround afg in afgs)
                {
                    afg.gameObject.SetActive(true);
                }

                // Enable the Light controller
                MaterialSetDirection[] msds = body.GetComponentsInChildren<MaterialSetDirection>();
                foreach (MaterialSetDirection msd in msds)
                {
                    msd.gameObject.SetActive(true);
                }

                // Atmosphere \o/
                body.atmosphere = true;
            
                // Restore the old material
                GameObject backupGameObject = body.scaledBody.GetChild("Backup");
                backupGameObject.SetActive(false);
                body.scaledBody.GetComponent<Renderer>().sharedMaterial =
                    backupGameObject.GetComponent<MeshRenderer>().material;
                UnityEngine.Object.Destroy(backupGameObject);
                
                // Update state
                toRestore.Remove(body);
            }
            else if (toDelete.Contains(body))
            {
                // Remove the Atmosphere from Ground
                AtmosphereFromGround[] afgs = body.GetComponentsInChildren<AtmosphereFromGround>();
                foreach (AtmosphereFromGround afg in afgs)
                {
                    UnityEngine.Object.Destroy(afg.gameObject);
                }

                // Disable the Light controller
                MaterialSetDirection[] msds = body.GetComponentsInChildren<MaterialSetDirection>();
                foreach (MaterialSetDirection msd in msds)
                {
                    UnityEngine.Object.Destroy(msd.gameObject);
                }

                // No Atmosphere :(
                body.atmosphere = false;
            
                // Restore the old material
                GameObject backupGameObject = body.scaledBody.GetChild("Backup");
                backupGameObject.SetActive(false);
                body.scaledBody.GetComponent<Renderer>().sharedMaterial =
                    backupGameObject.GetComponent<MeshRenderer>().material;
                UnityEngine.Object.Destroy(backupGameObject);
                
                // Update state
                toDelete.Remove(body);
            }
            
            // For all other bodies, there is a small chance (5%) that their existing 
            // atmosphere is toggled, or a new one gets added
            if (GetRandom(HighLogic.CurrentGame.Seed, 0, 100) < 5)
            {
                //Report::Report.fetch.SetPrefix("PlanetaryBody: " + body.bodyDisplayName);
                //Report::Report.fetch.ReportLine("Atmosphere toggled");
               
                ToggleAtmosphere(body);

                //Report::Report.fetch.ReportSection();
                return true;
            }
            //Report::Report.fetch.ReportSection();
            // Did we tweak something?
            return false;
        }

        /// <summary>
        /// Disables an existing atmosphere, or adds a new one if there is none
        /// </summary>
        private void ToggleAtmosphere(CelestialBody body)
        {
            // Disable an existing atmosphere
            if (body.atmosphere)
            {
                // Disable the Atmosphere from Ground
                AtmosphereFromGround[] afgs = body.GetComponentsInChildren<AtmosphereFromGround>();
                foreach (AtmosphereFromGround afg in afgs)
                {
                    afg.gameObject.SetActive(false);
                }

                // Disable the Light controller
                MaterialSetDirection[] msds = body.GetComponentsInChildren<MaterialSetDirection>();
                foreach (MaterialSetDirection msd in msds)
                {
                    msd.gameObject.SetActive(false);
                }

                // No Atmosphere :(
                body.atmosphere = false;

                // Get the material
                Renderer renderer = body.scaledBody.GetComponent<Renderer>();
                Material material = renderer.sharedMaterial;
                Texture2D diffuseMap = (Texture2D) material.GetTexture("_MainTex");
                Texture2D bumpMap = (Texture2D) material.GetTexture("_BumpMap");
                
                // Create a new scaled material
                Material newMaterial = new Material(Shader.Find("Terrain/Scaled Planet (Simple)"));
                newMaterial.SetTexture("_MainTex", diffuseMap);
                newMaterial.SetTexture("_BumpMap", bumpMap);
                newMaterial.SetFloat("_Shininess", material.GetFloat("_Shininess")); // TODO: Investigate
                newMaterial.SetColor("_SpecColor", material.GetColor("_SpecColor")); // TODO: Investigate
                
                // Apply the material
                renderer.sharedMaterial = newMaterial;

                // Backup the old material
                GameObject backupGameObject = new GameObject("Backup");
                backupGameObject.SetActive(false);
                backupGameObject.AddComponent<MeshRenderer>().material = material;
                backupGameObject.transform.parent = body.scaledBody.transform;

                // Update state
                toRestore.Add(body);
            }
            else
            {

                // Add a new atmosphere, this could get funny
                // We will just copy Laythe for the most parts
                body.atmosphere = true;
                body.atmosphereContainsOxygen = GetRandom(HighLogic.CurrentGame.Seed, 0, 99) < 10; // Oxygen is rare
                body.atmosphereDepth = (body.pqsController.radiusMax / 10) * GetRandomDouble(HighLogic.CurrentGame.Seed, 0.8, 1.2);
                body.atmosphereAdiabaticIndex =
                    1.39999997615814 * GetRandomDouble(HighLogic.CurrentGame.Seed, 0.8, 1.2);
                body.atmosphereGasMassLapseRate =
                    4.84741125702493 * GetRandomDouble(HighLogic.CurrentGame.Seed, 0.8, 1.2);
                body.atmosphereMolarMass = 0.0289644002914429 * GetRandomDouble(HighLogic.CurrentGame.Seed, 0.8, 1.2);
                Double multiplier = GetRandomDouble(HighLogic.CurrentGame.Seed, 0, 1);
                body.atmospherePressureSeaLevel = (595 * multiplier) + 5;
                body.atmosphereTemperatureSeaLevel = (270 * multiplier) + 240;
                body.atmDensityASL = (6.9 * multiplier) + 0.1;
                body.atmosphereTemperatureLapseRate = GetRandomDouble(HighLogic.CurrentGame.Seed, 0.004, 0.005);
                body.atmospherePressureCurveIsNormalized = true;
                body.atmosphereTemperatureCurveIsNormalized = true;
                body.atmosphereUsePressureCurve = true;
                body.atmosphereUseTemperatureCurve = true;

                // Select a curve template
                KeyValuePair<FloatCurve, FloatCurve> template =
                    GetRandomElement(HighLogic.CurrentGame.Seed, CurveTemplates.Atmospheres);
                body.atmospherePressureCurve = template.Key;
                body.atmosphereTemperatureCurve = template.Value;

                // Now add the visuals
                GameObject scaledVersion = body.scaledBody;

                // Add the material light direction behavior
                MaterialSetDirection materialLightDirection = scaledVersion.AddComponent<MaterialSetDirection>();
                materialLightDirection.valueName = "_localLightDirection";

                // Create the atmosphere shell game object
                GameObject scaledAtmosphere = new GameObject("Atmosphere");
                scaledAtmosphere.transform.parent = scaledVersion.transform;
                scaledAtmosphere.transform.position = scaledVersion.transform.position;
                scaledAtmosphere.transform.localPosition = Vector3.zero;
                scaledAtmosphere.layer = 9;
                MeshRenderer mrenderer = scaledAtmosphere.AddComponent<MeshRenderer>();
                mrenderer.sharedMaterial = new Material(Shader.Find("AtmosphereFromGround"));
                MeshFilter meshFilter = scaledAtmosphere.AddComponent<MeshFilter>();
                meshFilter.sharedMesh = Templates.ReferenceGeosphere;
                AtmosphereFromGround atmosphereFromGround = scaledAtmosphere.AddComponent<AtmosphereFromGround>();

                // Get the average color of the current texture
                Renderer renderer = body.scaledBody.GetComponent<Renderer>();
                Material material = renderer.sharedMaterial;
                Texture2D diffuseMap = Utility.CreateReadable((Texture2D) material.GetTexture("_MainTex"));
                Texture2D bumpMap = (Texture2D) material.GetTexture("_BumpMap");
                Color average = Utility.GetAverageColor(diffuseMap);
                Color altered = AlterColor(average);
                Color darkAltered = Utility.Dark(altered);

                body.afg = atmosphereFromGround;
                atmosphereFromGround.planet = body;
                atmosphereFromGround.sunLight = Planetarium.fetch.Sun.gameObject;
                atmosphereFromGround.mainCamera = PlanetariumCamera.fetch.transform;
                atmosphereFromGround.waveLength = new Color(1 - darkAltered.r, 1 - darkAltered.g, 1 - darkAltered.b, 0.5f);

                // Ambient Light
                body.atmosphericAmbientColor = altered;

                // Scaled Material
                Material newMaterial = new Material(Shader.Find("Terrain/Scaled Planet (RimAerial)"));
                newMaterial.SetTexture("_MainTex", diffuseMap);
                newMaterial.SetTexture("_BumpMap", bumpMap);
                newMaterial.SetFloat("_Shininess", material.GetFloat("_Shininess")); // TODO: Investigate
                newMaterial.SetColor("_SpecColor", material.GetColor("_SpecColor")); // TODO: Investigate
                newMaterial.SetFloat("_rimPower", (Single)GetRandomDouble(HighLogic.CurrentGame.Seed, 3.8, 5));
                newMaterial.SetFloat("_rimBlend", 1f);

                // Generate the atmosphere rim texture
                Gradient gradient = new Gradient();
                gradient.Add(0f, altered);
                gradient.Add(0.2f, new Color(0.0549f, 0.0784f, 0.141f, 1f));
                gradient.Add(1f, new Color(0.0196f, 0.0196f, 0.0196f, 1f));

                // Generate the ramp from a gradient 
                Texture2D ramp = new Texture2D(512, 1);
                Color[] colors = ramp.GetPixels(0);
                for (Int32 i = 0; i < colors.Length; i++)
                {
                    // Compute the position in the gradient 
                    Single k = (Single) i / colors.Length;
                    colors[i] = gradient.ColorAt(k);
                }
                ramp.SetPixels(colors, 0);
                ramp.Apply(true, false);
                ramp.wrapMode = TextureWrapMode.Clamp;
                ramp.mipMapBias = 0.0f;

                // Set the color ramp 
                newMaterial.SetTexture("_rimColorRamp", ramp);

                // Apply the material
                renderer.sharedMaterial = newMaterial;

                // Backup the old material
                GameObject backupGameObject = new GameObject("Backup");
                backupGameObject.SetActive(false);
                backupGameObject.AddComponent<MeshRenderer>().material = material;
                backupGameObject.transform.parent = scaledVersion.transform;

                // Update state
                toDelete.Add(body);
            }
        }

        /// <summary> 
        /// Makes a color a bit different 
        /// </summary> 
        private Color AlterColor(Color c)
        {
            return new Color((Single) Math.Min(1, c.r * GetRandomDouble(HighLogic.CurrentGame.Seed, 0.92, 1.05)),
                (Single) Math.Min(1, c.g * GetRandomDouble(HighLogic.CurrentGame.Seed, 0.92, 1.05)),
                (Single) Math.Min(1, c.b * GetRandomDouble(HighLogic.CurrentGame.Seed, 0.92, 1.05)), c.a);
        }
    }
}