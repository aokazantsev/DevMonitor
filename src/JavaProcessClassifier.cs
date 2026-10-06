using System;
using System.Collections.Generic;

namespace DevMonitor
{
    internal sealed class JavaProcessClassifier
    {
        private Dictionary<int, JavaProcessKind> knownKinds = new Dictionary<int, JavaProcessKind>();
        private Dictionary<int, JavaProcessKind> seenKinds = new Dictionary<int, JavaProcessKind>();

        public void BeginPass()
        {
            seenKinds.Clear();
        }

        public JavaProcessKind Classify(int processId)
        {
            JavaProcessKind kind;
            if (!knownKinds.TryGetValue(processId, out kind))
            {
                kind = KindOf(ProcessCommandLineReader.Read(processId));
            }
            seenKinds[processId] = kind;
            return kind;
        }

        public void EndPass()
        {
            Dictionary<int, JavaProcessKind> previous = knownKinds;
            knownKinds = seenKinds;
            seenKinds = previous;
        }

        private static JavaProcessKind KindOf(string commandLine)
        {
            if (commandLine == null) return JavaProcessKind.Worker;
            if (commandLine.IndexOf("GradleDaemon", StringComparison.Ordinal) >= 0) return JavaProcessKind.GradleDaemon;
            if (commandLine.IndexOf("KotlinCompileDaemon", StringComparison.Ordinal) >= 0) return JavaProcessKind.KotlinDaemon;
            return JavaProcessKind.Worker;
        }
    }
}
