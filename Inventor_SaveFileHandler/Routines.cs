// <copyright file="Routines.cs" company="MTL - Montagetechnik Larem GmbH">
// Copyright (c) MTL - Montagetechnik Larem GmbH. All rights reserved.
// </copyright>

namespace InvAddIn
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Windows;
    using Iv = Inventor;

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

        /// <summary>
        /// Calculates the outer dimensions of a part.
        /// </summary>
        /// <param name="partDocument">Reference to part object.</param>
        /// <param name="isRound">Flag that indicates a rotating part.</param>
        /// <returns>Outer dimensions as string.</returns>
        internal static string DetermineOuterDimensions(Iv.PartDocument partDocument, bool isRound)
        {
            // partDocument.UnitsOfMeasure.LengthUnits are not supported by now.
            double factor = 10;

            List<double> dim = new List<double>();

            // get max ranges in each dimensions.
            Iv.PartComponentDefinition cd = partDocument.ComponentDefinition;
            double length = (cd.RangeBox.MaxPoint.X - cd.RangeBox.MinPoint.X) * factor;
            double width = (cd.RangeBox.MaxPoint.Y - cd.RangeBox.MinPoint.Y) * factor;
            double height = (cd.RangeBox.MaxPoint.Z - cd.RangeBox.MinPoint.Z) * factor;
            dim = new List<double>() { Math.Floor(length), Math.Floor(width), Math.Floor(height) };
            dim.Sort();

            // for rotating parts
            if (isRound)
            {
                // if length / width is equal or height / width
                if (Math.Abs(dim[0] - dim[1]) < Math.Abs(dim[2] - dim[1]))
                {
                    return $"Ø{dim[1]}x{dim[2]}";
                }
                else
                {
                    return $"Ø{dim[1]}x{dim[0]}";
                }
            }
            else if (partDocument.ComponentDefinition is Iv.SheetMetalComponentDefinition)
            {
                Iv.SheetMetalComponentDefinition smcd = partDocument.ComponentDefinition as Iv.SheetMetalComponentDefinition;

                if (!smcd.HasFlatPattern)
                {
                    smcd.Unfold();
                    StandardAddInServer.This.InventorApplication.CommandManager.ControlDefinitions["PartSwitchRepresentationCmd"].Execute();
                }

                Iv.FlatPattern fp = smcd.FlatPattern;
                if (fp == null)
                {
                    return string.Empty;
                }

                length = (fp.RangeBox.MaxPoint.X - fp.RangeBox.MinPoint.X) * factor;
                width = (fp.RangeBox.MaxPoint.Y - fp.RangeBox.MinPoint.Y) * factor;
                height = (fp.RangeBox.MaxPoint.Z - fp.RangeBox.MinPoint.Z) * factor;

                dim = new List<double>() { Math.Floor(length), Math.Floor(width), Math.Floor(height) };

                dim.Sort();

                dim.RemoveAt(0);
                dim.Insert(0, (double)smcd.Thickness.Value * factor);
            }

            return $"{dim[2]}x{dim[1]}x{dim[0]}";
        }
    }
}
