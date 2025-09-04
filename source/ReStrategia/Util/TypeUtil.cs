using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace ReStrategia
{
    public static class TypeUtil
    {
        private static List<Assembly> badAssemblies = new List<Assembly>();

        public static Type FindType(string typeName)
        {
            // Got through assemblies looking for a matching type
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = null;
                try
                {
                    type = assembly.GetTypes().Where(t => t.Name == typeName).FirstOrDefault();
                }
                catch (Exception e)
                {
                    // Only log once
                    if (!badAssemblies.Contains(assembly))
                    {
                        Debug.LogException(new Exception("Error loading types from assembly " + assembly.FullName, e));
                        badAssemblies.Add(assembly);
                    }
                    continue;
                }

                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }
    }
}
