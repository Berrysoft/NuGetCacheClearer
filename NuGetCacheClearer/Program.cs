using System;
using System.Collections.Generic;
using System.IO;
using CommandLine;
using NuGet.Configuration;
using NuGet.Versioning;

namespace NuGetCacheClearer
{
    public class Options
    {
        [Option('v', "verbose", Required = false, HelpText = "Set output to verbose messages.")]
        public bool Verbose { get; set; }

        [Option("dry-run", Required = false, HelpText = "Show the directories to be cleared.")]
        public bool DryRun { get; set; }
    }

    static class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed(Work);
        }

        static void Work(Options o)
        {
            var settings = Settings.LoadDefaultSettings(Environment.GetLogicalDrives()[0]);
            var cache = Directory.CreateDirectory(SettingsUtility.GetGlobalPackagesFolder(settings));
            foreach (var dir in cache.EnumerateDirectories())
            {
#if NETCOREAPP
                if (!dir.Name.StartsWith('.'))
#else
                if (!dir.Name.StartsWith("."))
#endif
                {
                    var verdirs = dir.GetDirectories();
                    var newestdir = verdirs.MaxSource(d => NuGetVersion.Parse(d.Name), VersionComparer.VersionRelease);
                    foreach (var d in verdirs)
                    {
                        if (d != newestdir)
                        {
                            if (o.Verbose || o.DryRun)
                            {
                                Console.WriteLine(d);
                            }
                            if (!o.DryRun)
                            {
                                d.Delete(true);
                            }
                        }
                    }
                }
            }
        }

        static TSource MaxSource<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IComparer<TKey> comparer)
        {
            using var e = source.GetEnumerator();
            if (e.MoveNext())
            {
                var result = e.Current;
                var key = keySelector(result);
                while (e.MoveNext())
                {
                    var tkey = keySelector(e.Current);
                    if (comparer.Compare(tkey, key) > 0)
                    {
                        result = e.Current;
                        key = tkey;
                    }
                }
                return result;
            }
            else
            {
                return default;
            }
        }
    }
}
