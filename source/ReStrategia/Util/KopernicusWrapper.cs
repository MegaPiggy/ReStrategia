namespace ReStrategia
{
    internal class KopernicusWrapper
    {
        public static bool APIReady { get; internal set; }

        internal static bool Init()
        {
            try
            {
                Kopernicus.Utility.Clamp(0, 0, 0);
                APIReady = true;
            }
            catch
            {
                APIReady = false;
                return false;
            }
            return true;
        }

        internal static bool IsInvisible(CelestialBody cb)
        {
            var sc = cb.GetComponent<Kopernicus.Components.StorageComponent>();
            if (sc != null)
            {
                return sc.Has("invisibleScaledSpace") && sc.Get<bool>("invisibleScaledSpace");
            }
            return false;
        }

        internal static bool IsRnDHidden(CelestialBody cb)
        {
            var sc = cb.GetComponent<Kopernicus.Components.StorageComponent>();
            if (sc != null)
            {
                return sc.Has("hiddenRnD") && sc.Get<Kopernicus.Configuration.PropertiesLoader.RnDVisibility>("hiddenRnD") == Kopernicus.Configuration.PropertiesLoader.RnDVisibility.Hidden;
            }
            return false;
        }

        internal static bool IsRnDSkip(CelestialBody cb)
        {
            var sc = cb.GetComponent<Kopernicus.Components.StorageComponent>();
            if (sc != null)
            {
                return sc.Has("hiddenRnD") && sc.Get<Kopernicus.Configuration.PropertiesLoader.RnDVisibility>("hiddenRnD") == Kopernicus.Configuration.PropertiesLoader.RnDVisibility.Skip;
            }
            return false;
        }

        internal static bool IsContractHidden(CelestialBody cb)
        {
            var sc = cb.GetComponent<Kopernicus.Components.StorageComponent>();
            if (sc != null)
            {
                return sc.Has("contractWeight") && sc.Get<int>("contractWeight") <= 0;
            }
            return false;
        }
    }
}
