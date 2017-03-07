// <copyright file="WorkingDir.cs" company="MTL - Montagetechnik Larem GmbH">
// Copyright (c) MTL - Montagetechnik Larem GmbH. All rights reserved.
// </copyright>

namespace InvAddIn
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Contains path to working directory and special sub directories.
    /// </summary>
    public class WorkingDir
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WorkingDir"/> class.
        /// </summary>
        /// <param name="dir">Path to working directory.</param>
        public WorkingDir(string dir)
        {
            this.Dir = dir;
        }

        /// <summary>
        /// Gets the path to working directory.
        /// </summary>
        public string Dir { get; private set; }

        /// <summary>
        /// Gets path to 'CAD' folder. Will be created if not present.
        /// </summary>
        public string CAD
        {
            get
            {
                List<string> cadDirs = Directory.EnumerateDirectories(this.Dir, "*CAD*").ToList();

                if (cadDirs.Any())
                {
                    return cadDirs.First();
                }

                string result = Path.Combine(this.Dir, "CAD");
                Directory.CreateDirectory(result);

                return result;
            }
        }

        /// <summary>
        /// Gets path to 'Kaufteile' folder. Will be created if not present.
        /// </summary>
        public string Kaufteile
        {
            get
            {
                List<string> cadDirs = Directory.EnumerateDirectories(this.Dir, "*Kaufteile*").ToList();

                if (cadDirs.Any())
                {
                    return cadDirs.First();
                }

                string result = Path.Combine(this.Dir, "Kaufteile");
                Directory.CreateDirectory(result);

                return result;
            }
        }

        /// <summary>
        /// Gets path to 'Kundenteile' folder. Will be created if not present.
        /// </summary>
        public string Kundenteile
        {
            get
            {
                List<string> cadDirs = Directory.EnumerateDirectories(this.Dir, "*Kundenteile*").ToList();

                if (cadDirs.Any())
                {
                    return cadDirs.First();
                }

                string result = Path.Combine(this.Dir, "Kundenteile");
                Directory.CreateDirectory(result);

                return result;
            }
        }
    }
}
