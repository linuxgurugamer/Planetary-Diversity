using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;


namespace PlanetaryDiversity
{
    // http://forum.kerbalspaceprogram.com/index.php?/topic/147576-modders-notes-for-ksp-12/#comment-2754813
    // search for "Mod integration into Stock Settings

    public class PlanetaryDiversity : GameParameters.CustomParameterNode
    {
        public override string Title { get { return "Planetary Diversity"; } }
        public override GameParameters.GameMode GameMode { get { return GameParameters.GameMode.ANY; } }
        public override string Section { get { return "Planetary Diversity"; } }
        public override string DisplaySection { get { return "Planetary Diversity"; } }
        public override int SectionOrder { get { return 3; } }
        public override bool HasPresets { get { return false; } }

        [GameParameters.CustomParameterUI("Active")]
        public bool active = false;

        [GameParameters.CustomParameterUI("Orbit")]
        public bool Orbit = false;

        [GameParameters.CustomParameterUI("GasPlanetColor")]
        public bool GasPlanetColor = false;

        [GameParameters.CustomParameterUI("AtmospherePressure")]
        public bool AtmospherePressure = false;

        [GameParameters.CustomParameterUI("AtmosphereToggle")]
        public bool AtmosphereToggle = false;

        [GameParameters.CustomParameterUI("Name")]
        public bool Name = false;


        public override void SetDifficultyPreset(GameParameters.Preset preset)
        {
        }

        public override bool Enabled(MemberInfo member, GameParameters parameters)
        {

            return true;
        }


        public override bool Interactible(MemberInfo member, GameParameters parameters)
        {

            return false;
        }

        public override IList ValidValues(MemberInfo member)
        {
            return null;
        }
    }
}
