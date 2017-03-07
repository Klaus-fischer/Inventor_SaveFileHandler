using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InvAddIn
{
    public class WorkingDir
    {
        public WorkingDir(string dir)
        {
            this.Dir = dir;
        }

        public string Dir { get; private set; }

        /// <summary>
        /// Gets path to 'CAD' folder. Will be created if not present.
        /// </summary>
        public string CAD
        {
            get
            {
                List<string> cadDirs = Directory.EnumerateDirectories(Dir, "*CAD*").ToList();

                if (cadDirs.Any())
                {
                    return cadDirs.First();
                }

                string result = Path.Combine(Dir, "CAD");
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
                List<string> cadDirs = Directory.EnumerateDirectories(Dir, "*Kaufteile*").ToList();

                if (cadDirs.Any())
                {
                    return cadDirs.First();
                }

                string result = Path.Combine(Dir, "Kaufteile");
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
                List<string> cadDirs = Directory.EnumerateDirectories(Dir, "*Kundenteile*").ToList();

                if (cadDirs.Any())
                {
                    return cadDirs.First();
                }

                string result = Path.Combine(Dir, "Kundenteile");
                Directory.CreateDirectory(result);

                return result;
            }
        }
    }
}
