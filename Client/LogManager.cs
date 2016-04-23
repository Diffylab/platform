﻿using System;
using System.IO;
using Rage;

namespace GTANetwork
{
    public class LogManager
    {
        public static string LogDirectory = Main.GTANInstallDir + "\\logs";

        public static void CreateLogDirectory()
        {
            if (!Directory.Exists(Main.GTANInstallDir + "\\logs"))
                Directory.CreateDirectory(Main.GTANInstallDir + "\\logs");
        }

        public static void SimpleLog(string filename, string text)
        {
            CreateLogDirectory();
            File.AppendAllText(LogDirectory + "\\" + filename + ".log", text + "\r\n");
        }

        public static void DebugLog(string text)
        {
            CreateLogDirectory();
            if (!Main.WriteDebugLog) return;
            //File.AppendAllText(LogDirectory + "\\debug.log", text + "\r\n");
            Game.LogTrivial(text);
        }

        public static void LogException(Exception ex, string source)
        {
            CreateLogDirectory();
            File.AppendAllText(LogDirectory + "\\error.log", ">> EXCEPTION OCCURED AT " + DateTime.Now + " FROM " +source + "\r\n" +  ex.ToString() + "\r\n\r\n");
        }
    }
}