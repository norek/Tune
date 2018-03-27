using System;
using System.Collections.Generic;
using System.IO;

namespace Tune.Core
{
    public static class ScriptManipulationExtensions
    {
        public static IEnumerable<string> GetLines(this string str, bool removeEmptyLines = false)
        {
            using (var sr = new StringReader(str))
            {
                string line;

                while ((line = sr.ReadLine()) != null)
                {
                    if (removeEmptyLines && String.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    yield return line;
                }
            }
        }

        public static IEnumerable<string> GetScriptReferences(this IEnumerable<string> lines)
        {
            foreach (var line in lines)
            {
                if (line.Contains("#r"))
                {
                    //remove sign //#r
                    yield return line.Remove(0, 4);
                }
                else
                {
                    break;
                }
            }
        }
    }
}
