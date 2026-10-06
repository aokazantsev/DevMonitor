using System;
using System.Collections;
using System.IO;
using System.Reflection;

namespace DevMonitor
{
    internal sealed class CpuTemperatureSensor : IDisposable
    {
        private const string LibraryFolder = "lib";
        private const string LibraryFile = "LibreHardwareMonitorLib.dll";
        private const string Namespace = "LibreHardwareMonitor.Hardware.";

        private static readonly string[] PreferredSensorNames = { "CPU Package", "Core Max", "Core Average" };

        private readonly object computer;
        private readonly PropertyInfo hardwareListProperty;
        private readonly MethodInfo updateMethod;
        private readonly PropertyInfo sensorsProperty;
        private readonly PropertyInfo sensorTypeProperty;
        private readonly PropertyInfo sensorNameProperty;
        private readonly PropertyInfo sensorValueProperty;
        private readonly MethodInfo closeMethod;

        public CpuTemperatureSensor()
        {
            string libraryPath = Path.Combine(LibraryDirectory(), LibraryFile);
            if (!File.Exists(libraryPath)) return;
            try
            {
                AppDomain.CurrentDomain.AssemblyResolve += ResolveFromLibraryFolder;
                Assembly assembly = Assembly.LoadFrom(libraryPath);
                Type computerType = assembly.GetType(Namespace + "Computer", true);
                Type hardwareType = assembly.GetType(Namespace + "IHardware", true);
                Type sensorType = assembly.GetType(Namespace + "ISensor", true);

                object instance = Activator.CreateInstance(computerType);
                computerType.GetProperty("IsCpuEnabled").SetValue(instance, true, null);
                computerType.GetMethod("Open", Type.EmptyTypes).Invoke(instance, null);

                hardwareListProperty = computerType.GetProperty("Hardware");
                closeMethod = computerType.GetMethod("Close", Type.EmptyTypes);
                updateMethod = hardwareType.GetMethod("Update");
                sensorsProperty = hardwareType.GetProperty("Sensors");
                sensorTypeProperty = sensorType.GetProperty("SensorType");
                sensorNameProperty = sensorType.GetProperty("Name");
                sensorValueProperty = sensorType.GetProperty("Value");
                computer = instance;
            }
            catch (Exception)
            {
                computer = null;
            }
        }

        public float? Read()
        {
            if (computer == null) return null;
            try
            {
                var found = new float?[PreferredSensorNames.Length];
                foreach (object hardware in (IEnumerable)hardwareListProperty.GetValue(computer, null))
                {
                    updateMethod.Invoke(hardware, null);
                    foreach (object sensor in (IEnumerable)sensorsProperty.GetValue(hardware, null))
                    {
                        if (sensorTypeProperty.GetValue(sensor, null).ToString() != "Temperature") continue;
                        object value = sensorValueProperty.GetValue(sensor, null);
                        if (value == null) continue;
                        int index = Array.IndexOf(PreferredSensorNames, (string)sensorNameProperty.GetValue(sensor, null));
                        if (index >= 0) found[index] = Convert.ToSingle(value);
                    }
                }
                foreach (float? temperature in found)
                {
                    if (temperature.HasValue && temperature.Value > 0) return temperature;
                }
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string LibraryDirectory()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LibraryFolder);
        }

        private static Assembly ResolveFromLibraryFolder(object sender, ResolveEventArgs args)
        {
            string candidate = Path.Combine(LibraryDirectory(), new AssemblyName(args.Name).Name + ".dll");
            return File.Exists(candidate) ? Assembly.LoadFrom(candidate) : null;
        }

        public void Dispose()
        {
            if (computer == null || closeMethod == null) return;
            try
            {
                closeMethod.Invoke(computer, null);
            }
            catch (Exception)
            {
            }
        }
    }
}
