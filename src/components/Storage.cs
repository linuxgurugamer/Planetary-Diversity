using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Object = System.Object;

namespace PlanetaryDiversity.Components
{
    /// <summary>
    /// Extension methods for interfacing with Kopernicus Storage Component
    /// </summary>
    public static class Storage
    {
        private static Type storageComponent =
            Templates.Types.FirstOrDefault(t => t.Name == "StorageComponent" && t.Namespace == "Kopernicus");

        /// <summary>
        /// Returns if the internal storage knows an id
        /// </summary>
        public static Boolean Has(CelestialBody body, String id)
        {
            if (storageComponent == null)
                return false;
            Component c = body?.gameObject.GetComponent(storageComponent);
            if (c == null)
                return false;
            MethodInfo info = storageComponent.GetMethod("Has");
            return (Boolean)info.Invoke(c, new Object[] { id });
        }
    }
}