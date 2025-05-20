using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace RegistrySearch
{
    [SupportedOSPlatform("windows")]

    class Program
    {
        static readonly (string Name, RegistryKey Root)[] RootKeys = new[]
        {
            ("HKCR", Registry.ClassesRoot),
            ("HKCU", Registry.CurrentUser),
            ("HKLM", Registry.LocalMachine),
            ("HKCC", Registry.CurrentConfig)

        };


        static void Main(string[] args)
        {
            List<string> SearchTerms = new();
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string OutputFile = Path.Combine(desktopPath, "registry-vysledky.txt");
            string OutputErrorFile = Path.Combine(desktopPath, "registry-errors.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(OutputFile)!);
            Directory.CreateDirectory(Path.GetDirectoryName(OutputErrorFile)!);
            using var writer = new StreamWriter(OutputFile, false);
            using var errorwriter = new StreamWriter(OutputErrorFile, false);



            // Jednoduché zpracování parametru -p nebo --pattern
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-p" || args[i] == "--pattern")
                {
                    // všechny další argumenty do dalšího parametru, nebo do konce
                    i++;
                    while (i < args.Length && !args[i].StartsWith("-"))
                    {
                        SearchTerms.Add(args[i]);
                        i++;
                    }
                    i--; // protože for ještě přičte
                }
                // můžeš přidat další parametry, např. -o výstupní soubor atd.
            }

            if (SearchTerms.Count == 0)
            {
                Console.WriteLine("Použití: RegistrySearch.exe -p výraz1 výraz2 ...");
                return;
            }
            writer.WriteLine($"hledané výrazy: {string.Join(", ", SearchTerms)}");

            foreach (var (name, rootKey) in RootKeys)
            {
                Console.WriteLine($"Prohledávám {name}...");
                SearchRegistryKey(rootKey, name + "\\", writer,errorwriter,SearchTerms);
            }

            Console.WriteLine($"Hotovo! Výsledek v {OutputFile}");




            static void SearchRegistryKey(RegistryKey key, string path, StreamWriter writer, StreamWriter errorwriter, List<string> SearchTerms)
            {
                try
                {
                    // Hledání v názvu klíče
                    foreach (var term in SearchTerms)
                        if (path.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0)
                            writer.WriteLine($"{path}");

                    // Hledání v hodnotách
                    foreach (var valueName in key.GetValueNames())
                    {
                        object? value = null;
                        try { value = key.GetValue(valueName); } catch { }
                        foreach (var term in SearchTerms)
                        {
                            if ((valueName != null && valueName.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0) ||
                                (value != null && value.ToString().IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0))
                            {
                                writer.WriteLine($"{path} → {valueName} = {value} ");
                            }
                        }
                    }

                    // REKURZE DO PODKLÍČŮ
                    foreach (var subkeyName in key.GetSubKeyNames())
                    {
                        try
                        {
                            using RegistryKey? subkey = key.OpenSubKey(subkeyName);
                            if (subkey != null)
                            {
                                Console.WriteLine("\rdělám: " + path + "\\" + subkeyName + "   ");
                                SearchRegistryKey(subkey, path + "\\" + subkeyName, writer,errorwriter,SearchTerms);
                            }
                        }
                        catch (Exception ex)
                        {
                            errorwriter.WriteLine($"Chyba: Přeskočen podklíč: {path}\\{subkeyName} ({ex.Message})");
                            //Console.Write($"\rPřeskočen podklíč: {path}\\{subkeyName} ({ex.Message})        ");
                        }
                    }
                }
                catch (Exception ex)
                {
                    errorwriter.WriteLine($"Chyba:Chyba při procházení klíče: {path} ({ex.Message}) ");
                    //Console.Write($"\rChyba při procházení klíče: {path} ({ex.Message})        ");
                }
            }
        }
    }
}