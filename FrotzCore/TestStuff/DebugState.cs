using Frotz.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Frotz;

public static class DebugState
{
    internal static bool IsActive { get; private set; }
    public static void StartState(string stateFileToLoad)
    {
        if (stateFileToLoad != null)
        {
            using var good = new StreamReader(stateFileToLoad);
            string? line;
            while ((line = good.ReadLine()) != null)
            {
                if (!line.StartsWith('#'))
                {
                    StateLines.Add(line);
                }
            }
        }
        IsActive = true;
    }

    public static List<string> StateLines { get; } = new();
    public static List<string> OutputLines { get; } = new();

    private static int CurrentState = 0;

    internal static string LastCallMade = "";

    [Conditional("DEBUG")]
    public static void Output(ref DefaultInterpolatedStringHandler handler) => Output(true, ref handler);

    [Conditional("DEBUG")]
    public static void Output(bool log, ref DefaultInterpolatedStringHandler handler)
    {
        if (IsActive)
        {
            string current = string.Create(CultureInfo.InvariantCulture, ref handler);

            if (log && CurrentState < StateLines.Count && !current.StartsWith("#"))
            {
                string expected = StateLines[CurrentState++];

                if (string.Compare(expected, current, StringComparison.OrdinalIgnoreCase) != 0)
                {
                    Debug.WriteLine("mismatch! Expected:{0}: Current:{1}:{2}", expected, current, CurrentState);
                    StateLines.Clear();
                }

            }
            else
            {
                OutputLines.Add(current);
                Debug.WriteLine(current);
            }
        }
    }

    public static void SaveZMachine(string fileToSaveTo)
    {
        if (IsActive)
        {
            using var fs = new FileStream(fileToSaveTo, FileMode.Create);
            fs.Write(FastMem.ZMData);
        }
    }

    private static int Seed = 0;
    internal static int RandomSeed() => Seed++;
}
