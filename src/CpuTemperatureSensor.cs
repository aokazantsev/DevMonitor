using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Security;

namespace DevMonitor
{
    internal sealed class CpuTemperatureSensor : IDisposable
    {
        public const string GuardName = "cpu";

        private const string LibraryFolder = "lib";
        private const string LibraryFile = "LibreHardwareMonitorLib.dll";
        private const string Namespace = "LibreHardwareMonitor.Hardware.";

        private static readonly string[] PreferredSensorNames = { "CPU Package", "Core Max", "Core Average" };
        private static bool resolverRegistered;

        private object computer;
        private PropertyInfo hardwareListProperty;
        private MethodInfo updateMethod;
        private PropertyInfo sensorsProperty;
        private PropertyInfo sensorTypeProperty;
        private PropertyInfo sensorNameProperty;
        private PropertyInfo sensorValueProperty;
        private MethodInfo closeMethod;
        private bool firstReadPending;

        public string Problem { get; private set; }

        [HandleProcessCorruptedStateExceptions]
        [SecurityCritical]
        public void Open()
        {
            string blockedStep = SensorGuard.BlockedStep(GuardName);
            if (blockedStep != null)
            {
                Problem = "отключена: прошлый запуск упал на шаге «" + blockedStep + "»";
                AppLog.Append("cpu temperature: " + Problem);
                return;
            }
            string libraryPath = Path.Combine(LibraryDirectory(), LibraryFile);
            if (!File.Exists(libraryPath))
            {
                Problem = "нет библиотеки " + libraryPath;
                AppLog.Append("cpu temperature: " + Problem);
                return;
            }
            SensorGuard.Enter(GuardName, "запуск LibreHardwareMonitor и драйвера");
            try
            {
                if (!resolverRegistered)
                {
                    AppDomain.CurrentDomain.AssemblyResolve += ResolveFromLibraryFolder;
                    resolverRegistered = true;
                }
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
                Problem = null;
                firstReadPending = true;
                AppLog.Append("cpu temperature: LibreHardwareMonitor opened");
            }
            catch (Exception error)
            {
                computer = null;
                Problem = "недоступна: " + Describe(error);
                AppLog.Append("cpu temperature: " + Problem);
                SensorGuard.Leave(GuardName);
            }
        }

        [HandleProcessCorruptedStateExceptions]
        [SecurityCritical]
        public float? Read()
        {
            if (computer == null) return null;
            if (firstReadPending) SensorGuard.Enter(GuardName, "первое чтение температуры CPU");
            try
            {
                var found = new float?[PreferredSensorNames.Length];
                int hardwareCount = 0;
                foreach (object hardware in (IEnumerable)hardwareListProperty.GetValue(computer, null))
                {
                    hardwareCount++;
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
                float? result = null;
                string source = null;
                for (int i = 0; i < found.Length && result == null; i++)
                {
                    if (found[i].HasValue && found[i].Value > 0)
                    {
                        result = found[i];
                        source = PreferredSensorNames[i];
                    }
                }
                if (firstReadPending)
                {
                    firstReadPending = false;
                    SensorGuard.Leave(GuardName);
                    AppLog.Append("cpu temperature: first read, hardware " + hardwareCount + ", "
                        + (source == null ? "no temperature sensor (driver PawnIO missing or app not elevated?)" : source + " = " + result));
                }
                return result;
            }
            catch (Exception error)
            {
                computer = null;
                Problem = "отключена после ошибки: " + Describe(error);
                AppLog.Append("cpu temperature: " + Problem);
                if (firstReadPending)
                {
                    firstReadPending = false;
                    SensorGuard.Leave(GuardName);
                }
                return null;
            }
        }

        private static string Describe(Exception error)
        {
            Exception inner = error is TargetInvocationException && error.InnerException != null ? error.InnerException : error;
            return inner.GetType().Name + ": " + inner.Message;
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

        [HandleProcessCorruptedStateExceptions]
        [SecurityCritical]
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
            computer = null;
        }
    }
}
