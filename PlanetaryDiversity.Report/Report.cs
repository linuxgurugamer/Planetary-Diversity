using KSP.Localization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

#if false
namespace PlanetaryDiversity.Report
{

    /// <summary>
    /// Gives the user an opportunity to set the seed of their savegame
    /// </summary>
    [KSPAddon(KSPAddon.Startup.MainMenu, true)] 
    public class Report: MonoBehaviour
    {
        public static Report fetch;

        List<string> lines;
        string prefix = "";
        void Awake()
        {
            lines = new List<string>();
            fetch = this;
        }

        public void SetPrefix(string str)
        {
            prefix = str;
        }
        public void ClearPrefix()
        {
            prefix = "";
        }
        public void ReportLine(string str)
        {
            lines.Add(prefix + str);
        }
        public void ReportLines(string[] str)
        {
            foreach (var s in str)
                lines.Add(prefix + s);
        }
        public void ReportLines(List<string> str)
        {
            foreach (var s in str)
                lines.Add(s);
        }
        public void ReportSection()
        {
            lines.Add("");
            prefix = "";
        }

        public void WriteReportToFile(string fname)
        {
            File.WriteAllLines(fname, lines.ToArray());
        }
    }
}
#endif