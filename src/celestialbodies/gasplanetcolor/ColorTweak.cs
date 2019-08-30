using PlanetaryDiversity.API;
using System;
using System.IO;
using System.Reflection;
using PlanetaryDiversity.Components;
using UnityEngine;
using Gradient = PlanetaryDiversity.Components.Gradient;
//using Report = PlanetaryDiversity.Report;

namespace PlanetaryDiversity.CelestialBodies.GasPlanetColor
{
    /// <summary>
    /// Changes the colors of Gas Planets
    /// </summary>
    public class ColorTweak : CelestialBodyTweaker
    {
        /// <summary>
        /// Returns the name of the config node that stores the configuration
        /// </summary>
        public override String GetConfig() => "PD_CELESTIAL";

        /// <summary>
        /// Returns the name of the config option that can be used to disable the tweak
        /// </summary>
        public override String GetSetting() => "GasPlanetColor";

        /// <summary>
        /// Changes the parameters of the body
        /// </summary>
        public override Boolean Tweak(CelestialBody body)
        {
            // Is the body a GasPlanet?
            if (body.hasSolidSurface || body.scaledBody.GetComponentsInChildren<SunShaderController>().Length != 0)
                return false;
            
            // Is the body a barycenter?
            if (Storage.Has(body, "barycenter"))
                return false;
            
            // Get the material of the body
            Renderer renderer = body.scaledBody.GetComponent<Renderer>();
            Material material = renderer.sharedMaterial;

            // Get the average color of the current texture
            Texture2D diffuseMap = Utility.CreateReadable((Texture2D) material.GetTexture("_MainTex"));
            Color average = Utility.GetAverageColor(diffuseMap);

            // Select a new color and apply it
            Color newColor = Utility.Dark(GetRandomElement(HighLogic.CurrentGame.Seed, Utility.colors));
            material.color = Utility.ReColor(newColor, average);

            // Does this planet have an atmosphere?
            if (!body.atmosphere)
                return true;

            // Recolor the Atmosphere from Ground
            Color afgColor = new Color(1 - newColor.r, 1 - newColor.g, 1 - newColor.b, 1 - newColor.a);
            if (body.afg != null)
            {
                body.afg.waveLength = afgColor;
                EventData<AtmosphereFromGround> afgEvent =
                    GameEvents.FindEvent<EventData<AtmosphereFromGround>>("Kopernicus.RuntimeUtility.PatchAFG");
                afgEvent?.Add((afg) => afg.waveLength = afgColor);
            }
            body.atmosphericAmbientColor = newColor;
            
            // Generate the atmosphere rim texture
            Gradient gradient = new Gradient();
            gradient.Add(0f, newColor);
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
            material.SetTexture("_rimColorRamp", ramp);

            // Apply the material
            renderer.sharedMaterial = material;

            //Report::Report.fetch.SetPrefix("PlanetaryBody: " + body.bodyDisplayName);
            //Report::Report.fetch.ReportLine("Color altered");
            //Report::Report.fetch.ReportSection();

            return true;
        }
    }
}
