using System;
using System.Collections.Generic;
using System.Linq;
using PlanetaryDiversity.API;
using UnityEngine;

namespace PlanetaryDiversity.CelestialBodies.Name
{
    /// <summary>
    /// Changes the names of the planets
    /// </summary>
    public class NameTweak : CelestialBodyTweaker
    {
        /// <summary>
        /// Returns the name of the config node that stores the configuration
        /// </summary>
        public override String GetConfig() => "PD_CELESTIAL";

        /// <summary>
        /// Returns the name of the config option that can be used to disable the tweak
        /// </summary>
        public override String GetSetting() => "Name";

        /// <summary>
        /// Whether a star uses systematic names
        /// </summary>
        private static Dictionary<String, Boolean> usesSystematicName;

        /// <summary>
        /// Changes the parameters of the body
        /// </summary>
        public override Boolean Tweak(CelestialBody body)
        {
            // Create the dictionary
            if (usesSystematicName == null)
            {
                usesSystematicName = new Dictionary<String, Boolean>();
                foreach (CelestialBody b in PSystemManager.Instance.localBodies.Where(b =>
                    b.scaledBody.GetComponentsInChildren<SunShaderController>().Length != 0))
                {
                    usesSystematicName.Add(b.transform.name, false);
                }
            }

            // Are we a star?
            if (body.scaledBody.GetComponentsInChildren<SunShaderController>().Length != 0)
            {
                Boolean useSystematicNames = GetRandom(HighLogic.CurrentGame.Seed, 0, 100) < 50;
                if (useSystematicNames)
                {
                    body.bodyDisplayName = SystematicStarName();
                    usesSystematicName[body.transform.name] = true;
                }
                else
                {
                    body.bodyDisplayName = GenerateStarName();
                    usesSystematicName[body.transform.name] = false;
                }
            }
            else
            {
                CelestialBody star = GetNextStar(body);
                if (usesSystematicName[star.transform.name])
                {
                    CelestialBody referenceBody = body.orbit.referenceBody;
                    do
                    {
                        body.bodyDisplayName = GetRandom(HighLogic.CurrentGame.Seed, 0, 100) < 25 ? GenerateProceduralName() :
                            GenerateSystematicName(referenceBody.bodyDisplayName,
                                referenceBody.orbitingBodies.IndexOf(body), star == referenceBody);
                    } while (body.bodyDisplayName == "");
                }
                else
                {
                    do
                    {
                        body.bodyDisplayName = GetRandom(HighLogic.CurrentGame.Seed, 0, 100) < 75 ? GenerateProceduralName() : GenerateName();
                    } while (body.bodyDisplayName == "");
                }
            }

            return true;
        }

        /// <summary>
        /// Returns the first star the body is orbiting
        /// </summary>
        private CelestialBody GetNextStar(CelestialBody body)
        {
            if (body.orbitDriver == null || body.scaledBody.GetComponentsInChildren<SunShaderController>().Length != 0)
                return body;
            return GetNextStar(body.orbit.referenceBody);
        }

        /// <summary>
        /// Returns a random name for the star
        /// </summary>
        private String GenerateStarName()
        {
            // Load Greek Letters
            String[] commonLetters = Starnames.commonLetters;
            String[] rareLetters = Starnames.rareLetters;

            // Load Greek Characters
            String[] commonChars = Starnames.commonChars;
            String[] rareChars = Starnames.rareChars;

            // Load constellation names
            String[] prefix = Starnames.starprefix;
            String[] middle = Starnames.starmiddle;
            String[] suffix = Starnames.starsuffix;

            // Load multiple systems nomenclature
            String[] multiple = Starnames.starmultiple;
            Boolean useChars = false;

            // Generate Constellation Name First

            String name = GetRandomElement(HighLogic.CurrentGame.Seed, prefix);
            name += GetRandomElement(HighLogic.CurrentGame.Seed, middle);

            // Avoid being anal
            if (name == "An")
            {
                name += "n";
            }
            name += GetRandomElement(HighLogic.CurrentGame.Seed, suffix);

            // Add Letters or Characters
            if (GetRandom(HighLogic.CurrentGame.Seed, 0, 100) < 25)
            {
                useChars = true;
            }

            // Choose which letter to use
            Int32 letter = GetRandom(HighLogic.CurrentGame.Seed, 0, 501);
            if (letter < 350)
            {
                if (useChars)
                {
                    name = GetRandomElement(HighLogic.CurrentGame.Seed, commonChars) + " " + name;
                }
                else
                {
                    name = GetRandomElement(HighLogic.CurrentGame.Seed, commonLetters) + " " + name;
                }
            }
            else if (letter < 500)
            {
                if (useChars)
                {
                    name = GetRandomElement(HighLogic.CurrentGame.Seed, rareChars) + " " + name;
                }
                else
                {
                    name = GetRandomElement(HighLogic.CurrentGame.Seed, rareLetters) + " " + name;
                }
            }
            else
            {
                if (useChars)
                {
                    name = "κ " + name;
                }
                else
                {
                    name = "Kappa " + name;
                }
            }

            // Add Majoris or Minoris
            if (GetRandom(HighLogic.CurrentGame.Seed, 0, 100) < 10)
            {
                if (GetRandom(HighLogic.CurrentGame.Seed, 0, 100) < 50)
                {
                    name += " Minoris";
                }
                else
                {
                    name += " Majoris";
                }
            }

            // Add multiple systems nomenclature
            if (GetRandom(HighLogic.CurrentGame.Seed, 0, 100) < 5)
            {
                name += " " + GetRandomElement(HighLogic.CurrentGame.Seed, multiple);
            }

            return name;
        }


        /// <summary>
        /// Returns a systematic name for the star
        /// </summary>
        private String SystematicStarName()
        {
            // Load Acronyms
            String[] acronym = Starnames.acronyms;

            String name = GetRandomElement(HighLogic.CurrentGame.Seed, acronym);
            name += new String('0', GetRandom(HighLogic.CurrentGame.Seed, 0, 5));
            name += GetRandom(HighLogic.CurrentGame.Seed, 100, 9999);
            return name;
        }


        /// <summary>
        /// Returns a random name for the body
        /// </summary>
        private String GenerateName()
        {
            String[] prefix = Planetnames.prefix;
            String[] middle = Planetnames.middle;
            String[] suffix = Planetnames.suffix;
            Boolean hasMiddle = false;

            String name = GetRandomElement(HighLogic.CurrentGame.Seed, prefix);
            if (GetRandom(HighLogic.CurrentGame.Seed, 0, 100) < 50)
            {
                name += GetRandomElement(HighLogic.CurrentGame.Seed, middle);
                hasMiddle = true;
            }
            if (GetRandom(HighLogic.CurrentGame.Seed, 0, 100) < 50 || !hasMiddle)
            {
                name += GetRandomElement(HighLogic.CurrentGame.Seed, suffix);
            }
            if (name == "Kerbin" || name == "Kerbol")
            {
                name = GenerateName();
            }
            return name;
        }

        ProceduralNameGenerator names = null;
        private string GenerateProceduralName()
        {
            if (names == null)
                names = new ProceduralNameGenerator("GameData/PlanetaryDiversity/PluginData/names.txt");
            if (!names.initialized)
                return GenerateName();
            return names.GenerateRandomWord(GetRandom(HighLogic.CurrentGame.Seed, 7, 12));
        }

        /// <summary>
        /// Returns a systematic name for the body
        /// </summary>
        private String GenerateSystematicName(String parentname, Int32 position, Boolean isOrbitingStar)
        {
            const String moons = "abcdefghijklmnopqrstuvwxyz";

            if (isOrbitingStar)
                return parentname + " " + ToRomanNumeral(position + 1);
            else
                return parentname + moons[position];
        }

        /// <summary>
        /// Converts an integer into a roman numeral
        /// </summary>
		public static String ToRomanNumeral(Int32 number)
        {
            String romanNumeral = String.Empty;
            while (number > 0)
            {
                // find biggest numeral that is less than equal to number
                Int32 index = Planetnames.numerals.ToList().FindIndex(x => x <= number);

                // subtract it's value from your number
                number -= Planetnames.numerals[index];

                // tack it onto the end of your roman numeral
                romanNumeral += Planetnames.romanNumerals[index];
            }
            return romanNumeral;
        }
    }
}