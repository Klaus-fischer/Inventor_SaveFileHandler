// <copyright file="Routines.cs" company="MTL - Montagetechnik Larem GmbH">
// Copyright (c) MTL - Montagetechnik Larem GmbH. All rights reserved.
// </copyright>

namespace InvAddIn
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Windows;

    /// <summary>
    /// Contains several routines used twice or more in this project.
    /// </summary>
    public static class Routines
    {
        /// <summary>
        /// Calculates next part number based on prefix.
        /// </summary>
        /// <param name="prefix">PV001_T</param>
        /// <param name="suffix">Extension of requested type. Example 'ipt'</param>
        /// <param name="dir">PathToFiles</param>
        /// <returns>Next expected part number.</returns>
        public static string GetNextPartNumber(string prefix, string suffix, string dir)
        {
            // counter for highest index
            int i = 0;

            Regex regEx = new Regex($@"^{prefix}(\d+)", RegexOptions.IgnoreCase);

            foreach (string file in Directory.GetFiles(dir, $"{prefix}*.{suffix}", SearchOption.AllDirectories)
                .Select(o => Path.GetFileName(o)))
            {
                Match m = regEx.Match(file);

                if (m.Success)
                {
                    i = Math.Max(i, int.Parse(m.Groups[1].Value));
                }
            }

            return $"{prefix}{i + 1:000}";
        }

        /// <summary>
        /// To serialize an Exception to a file stream.
        /// File will be saved on desktop.
        /// </summary>
        /// <param name="ex">Exception to serialize.</param>
        internal static void SerializeException(Exception ex)
        {
            try
            {
                using (StreamWriter fs = File.AppendText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDesktopDirectory), @"ErrorLog.txt")))
                {
                    fs.WriteLine("\n========================================");
                    fs.WriteLine(DateTime.Now);
                    fs.WriteLine(ex.GetType().FullName);
                    fs.WriteLine(ex.Message);
                    fs.WriteLine(ex.StackTrace);
                    fs.Flush();
                }
            }
            catch (Exception ex2)
            {
                MessageBox.Show($"{ex2.GetType().Name}Caught:\n{ex2.Message}\n{ex2.StackTrace}");
                MessageBox.Show($"{ex.GetType().Name}Caught:\n{ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
