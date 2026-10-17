using System.Collections.Generic;

namespace DevMonitor
{
    internal static class UninstallProfile
    {
        public const string ConfirmDetails =
            "Удалится ярлык на рабочем столе. Драйвер PawnIO останется — это отдельная программа, удаляется в «Параметры → Приложения».";

        public static void Remove(List<string> problems)
        {
            string problem = DesktopShortcut.Delete();
            if (problem != null) problems.Add("ярлык на рабочем столе не удалён: " + problem);
        }
    }
}
