using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using KSP;
using Strategies;

namespace Strategia
{
    /// <summary>
    /// Special MonoBehaviour to fix up the departments.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class DepartmentFixer : MonoBehaviour
    {
        public void Awake()
        {
        }

        public void Update()
        {
            if (FixDepartments())
            {
                Destroy(this);
            }
        }

        public bool FixDepartments()
        {
            // Wait for the strategy system to get loaded
            if (StrategySystem.Instance == null)
            {
                return false;
            }

            // Find the departments
            DepartmentConfig gene = null;
            DepartmentConfig wernher = null;
            foreach (DepartmentConfig department in StrategySystem.Instance.SystemConfig.Departments)
            {
                // Save Gene and Wernher so we can do a reorg
                if (department.AvatarPrefab != null)
                {
                    if (department.AvatarPrefab.name == "Instructor_Gene")
                    {
                        gene = department;
                    }
                    else if (department.AvatarPrefab.name == "Instructor_Wernher")
                    {
                        wernher = department;
                    }
                }
            }

            // Re-order stuff
            if (wernher != null)
            {
                StrategySystem.Instance.SystemConfig.Departments.Remove(wernher);
                StrategySystem.Instance.SystemConfig.Departments.Insert(0, wernher);
            }
            if (gene != null)
            {
                StrategySystem.Instance.SystemConfig.Departments.Remove(gene);
                StrategySystem.Instance.SystemConfig.Departments.Insert(0, gene);
            }

            return true;
        }
    }
}
