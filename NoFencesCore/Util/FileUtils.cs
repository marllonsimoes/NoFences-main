using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace NoFencesCore.Util
{

    public class ShortcutInfo
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Path { get; set; }
        public string Url { get; set; }
        public string WorkingDirectory { get; set; }
    }

    public class FileUtils
    {
        static Shell32.Shell shl = new Shell32.Shell();

        public static ShortcutInfo GetShortcutInfo(string full_name)
        {

            string extension = Path.GetExtension(full_name);

            if (extension.Equals(".lnk", StringComparison.OrdinalIgnoreCase)) {
                return getFromLink(full_name);
            }
            if (extension.Equals(".url", StringComparison.OrdinalIgnoreCase))
            {
                return getFromUrlFile(full_name);
            }
            return null;
        }

        private static ShortcutInfo getFromLink(string full_name)
        {
            try
            {
                full_name = Path.GetFullPath(full_name);
                var dir = shl.NameSpace(Path.GetDirectoryName(full_name));
                var itm = dir.Items().Item(Path.GetFileName(full_name));
                var lnk = (Shell32.ShellLinkObject)itm.GetLink;

                ShortcutInfo shortcutInfo = new ShortcutInfo();
                shortcutInfo.Name = itm.Name;
                shortcutInfo.Description = lnk.Description;
                shortcutInfo.Path = lnk.Path;
                shortcutInfo.WorkingDirectory = lnk.WorkingDirectory;
                //shortcutInfo.Args =  = lnk.Arguments;
                return shortcutInfo;
            }
            catch (Exception ex)
            {
                Console.Write(ex.ToString());
                return null;
            }
        }

        private static ShortcutInfo getFromUrlFile(string fileName)
        {
            IniFile ini = new IniFile(fileName);
            ini.KeyExists("Url", "InternetShortcut");

            ShortcutInfo shortcutInfo = new ShortcutInfo();
            shortcutInfo.Name = Path.GetFileNameWithoutExtension(fileName);
            shortcutInfo.Path = fileName;
            shortcutInfo.Url = ini.Read("Url", "InternetShortcut");
            return shortcutInfo;
        }
    }

    internal class IniFile
    {
        string Path;
        string EXE = Assembly.GetExecutingAssembly().GetName().Name;

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        static extern int GetPrivateProfileString(string Section, string Key, string Default, StringBuilder RetVal, int Size, string FilePath);

        public IniFile(string IniPath = null)
        {
            Path = new FileInfo(IniPath ?? EXE).FullName;
        }

        public string Read(string Key, string Section = null)
        {
            var RetVal = new StringBuilder(255);
            GetPrivateProfileString(Section ?? EXE, Key, "", RetVal, 255, Path);
            return RetVal.ToString();
        }

        public bool KeyExists(string Key, string Section = null)
        {
            return Read(Key, Section).Length > 0;
        }
    }

//    //Find the WEB URL in the arg
//                    foreach (Match item in Regex.Matches(args[index + 1], @"https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()@:%_\+.~#?&//=]*)"))
//                    {
//                        if (item.Success)
//                        {
//                            URL = item.Value;
//                            break;
//                        }
//                    }

//                    //Find the STEAM URL in the arg
//                    //foreach (Match item in Regex.Matches(arg, @"(steam|http|ftp|https):\/\/([\w\-_]+(?:(?:\.[\w\-_]+)+))([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])?"))
//                    foreach (Match item in Regex.Matches(args[index + 1], @"steam:\/\/rungameid/\d+"))
//{
//    if (item.Success)
//    {
//        URL = item.Value;
//        break;
//    }
//}

////uplay://launch/720/0
//foreach (Match item in Regex.Matches(args[index + 1], @"uplay:\/\/launch\/\d+\/\d+"))
//{
//    if (item.Success)
//    {
//        URL = item.Value;
//        break;
//    }
//}

////com.epicgames.launcher://apps/Curry?action=launch&silent=true
//foreach (Match item in Regex.Matches(args[index + 1], @"com.epicgames.launcher:\/\/apps\/.*"))
//{
//    if (item.Success)
//    {
//        URL = item.Value;
//        break;
//    }
//}

////twitch://fuel-launch/e94696a4-61ce-4930-80ba-138c0da0b433
//foreach (Match item in Regex.Matches(args[index + 1], @"twitch:\/\/fuel-launch\/.*"))
//{
//    if (item.Success)
//    {
//        URL = item.Value;
//        break;
//    }
//}
}
