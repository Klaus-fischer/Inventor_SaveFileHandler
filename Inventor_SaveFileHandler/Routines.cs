using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace InvAddIn
{
    public static class Routines
    {
        /// <summary>
        /// Calculates next part number based on prefix.
        /// </summary>
        /// <param name="prefix">PV001_T</param>
        /// <param name="suffix">ipt</param>
        /// <param name="dir">PathToFiles</param>
        /// <returns></returns>
        public static string GetNextPartNumber(string prefix, string suffix, string dir)
        {
            // zähler für hochsten index
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
