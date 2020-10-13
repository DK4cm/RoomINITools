using System;
using System.Text;
using System.Runtime.InteropServices;

namespace RoomINITools
{
    public class IniTools : IDisposable
    {
        public string path;
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool WritePrivateProfileString(string section, string key, string val, string filePath);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, uint size, string filePath);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetLongPathName(string path, StringBuilder longPath, int longPathLength);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetShortPathName(string path, StringBuilder shortPath, int shortPathLength);


        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="INIPath">INI File Path</param>
        public IniTools(string INIPath)
        {
            path = ToShortPathName(INIPath);
        }

        /// <summary>
        /// Write Data to the INI File
        /// </summary>
        /// <param name="Section">Section Name</param>
        /// <param name="Key">Key Name</param>
        /// <param name="Value">Key Value</param>
        public void WriteValue(string Section, string Key, string Value)
        {
            WritePrivateProfileString(Section, Key, Value, path);
        }

        /// <summary>
        /// Read Data Value From the Ini File
        /// </summary>
        /// <param name="Section">Section Name</param>
        /// <param name="Key">Key Name</param>
        /// <returns>Key Value</returns>
        public string ReadValue(string Section, string Key)
        {
            StringBuilder temp = new StringBuilder(1000);
            GetPrivateProfileString(Section, Key, "", temp, (uint)temp.Capacity, path);
            return temp.ToString();
        }

        /// <summary>
        /// Get The Short Name of The Path(8.3 mode)
        /// </summary>
        /// <param name="longName"></param>
        /// <returns></returns>
        public static string ToShortPathName(string longName)
        {
            StringBuilder shortNameBuffer = new StringBuilder(256);
            int bufferSize = shortNameBuffer.Capacity;
            int result = GetShortPathName(longName, shortNameBuffer, bufferSize);
            return shortNameBuffer.ToString();
        }

        /// <summary>
        /// Get The Long Name of The Path
        /// </summary>
        /// <param name="shortName"></param>
        /// <returns></returns>
        public static string ToLongPathName(string shortName)
        {
            StringBuilder longNameBuffer = new StringBuilder(256);
            int bufferSize = longNameBuffer.Capacity;
            int result = GetShortPathName(shortName, longNameBuffer, bufferSize);
            return longNameBuffer.ToString();
        }

        public void Dispose()
        {
            //nothing to do with
        }
    }
}
