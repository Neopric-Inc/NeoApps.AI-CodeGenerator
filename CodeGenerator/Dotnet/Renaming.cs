using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace NoCodeAppGenerator
{
    public class Renaming
    {
        public static string projectName;
        public Renaming()
        {
        }
        public Renaming(string projectName)
        {
            Renaming.projectName = projectName;
        }
        public static void RenameDir(string source, string des)
        {
            Directory.Move(source, des);
        }
        public static void join(string[] list, string last, ref string des)
        {
            foreach (string i in list)
            {
                if (i == last)
                    break;
                des += i;
                des += '/';
            }
        }
        public static void join_again(string last, ref string des)
        {
            int cnt = 0;
            foreach (char i in last)
            {
                if (i == '.')
                {
                    cnt++;
                }
                if (cnt >= 2)
                    des += i;
            }
        }
        public static void ProcessDir(string rel, string projectName)
        {
            var directories = Directory.GetDirectories(@rel);
            foreach (string path in directories)
            {
                string des = "";
                var list = path.Split('/');
                string last = list[^1];
                join(list, last, ref des);
                des += projectName;
                join_again(last, ref des);
                if (!Directory.Exists(des))
                    RenameDir(path, des);
            }
        }
        static void RenameFolder(string sourcePath)
        {
            // Rename files
            foreach (string filePath in Directory.GetFiles(sourcePath))
            {
                string fileName = Path.GetFileName(filePath);
                if (fileName.Contains("nkv.MicroService"))
                {
                    string newFileName = fileName.Replace("nkv.MicroService", projectName);
                    string destinationFilePath = Path.Combine(Path.GetDirectoryName(filePath), newFileName);
                    File.Move(filePath, destinationFilePath);
                }
            }

            // Rename subdirectories
            foreach (string dirPath in Directory.GetDirectories(sourcePath))
            {
                string dirName = Path.GetFileName(dirPath);
                if (dirName.Contains("nkv.MicroService"))
                {
                    string newDirName = dirName.Replace("nkv.MicroService",projectName);
                    string destinationDirPath = Path.Combine(Path.GetDirectoryName(dirPath), newDirName);
                    Directory.Move(dirPath, destinationDirPath);
                    RenameFolder(destinationDirPath);
                }
                
            }
        }
        public void RenameDirs(string fname,string projectName)
        {
            string rel = $"../{fname}/{projectName}/DotNet_Output/solution/";
            RenameFolder(rel);
        }
    }
}