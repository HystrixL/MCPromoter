using System;
using System.Collections.Generic;
using System.IO;

namespace MCPromoter
{
    public static class QuickBackupPath
    {
        public static string RootPath = @"plugins\MCPromoter";
        public static string ConfigPath = $@"{RootPath}\config.yml";
        public static string QbRootPath = $@"{RootPath}\QuickBackup";
        public static string QbHelperPath = $@"{QbRootPath}\QuickBackup.exe";
        public static string QbLogPath = $@"{QbRootPath}\qbLog.txt";
        public static string QbInfoPath = $@"{QbRootPath}\qbInfo.ini";
    }
}