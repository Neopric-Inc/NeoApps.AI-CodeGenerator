using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace NoCodeAppGenerator
{
    public class DirectoryWork
    {
        public static void MakeDirs(string curDir,string projectName,string uid)
        {
            string[] dirss = System.IO.Directory.GetDirectories(Path.Combine(curDir, "../"), (uid + "_*"));
            string pt = "";
            if (dirss.Length == 0)
            {
                pt = Path.Combine(curDir, "../", (uid + "_" + DateTime.Now.Ticks));
                System.IO.Directory.CreateDirectory(pt);
                pt = pt + "/" + projectName;
            }
            else
                pt = (dirss[0] + "/" + projectName);
            bool ext = Directory.Exists(pt);
            if (ext)
            {
                Directory.Delete(pt, true);
            }
            Directory.CreateDirectory(pt);
            Directory.CreateDirectory(pt + "/zip");
            string[] drpath = Directory.GetDirectories(@"../", (uid + "_*"));
            string fname = drpath[0].Split("/").Last();
            string path1 = @"./DotNetMySQLTemplate/PostmanJson";
            string path2 = @$"../{fname}/{projectName}/DotNet_Output/PostmanJson";
            //CopyDirectory(source, des);
            CloneFolderStructure(path1, path2);
            path1 = @"./DotNetMySQLTemplate/solution";
            path2 = @$"../{fname}/{projectName}/DotNet_Output/solution";
            CloneFolderStructure(path1, path2);
        }
        static void CloneFolderStructure(string sourcePath, string destinationPath)
        {
            Directory.CreateDirectory(destinationPath);

            // Copy files
            foreach (string filePath in Directory.GetFiles(sourcePath))
            {
                string fileName = Path.GetFileName(filePath);
                string destinationFilePath = Path.Combine(destinationPath, fileName);
                File.Copy(filePath, destinationFilePath, true);
            }

            // Clone subdirectories
            foreach (string dirPath in Directory.GetDirectories(sourcePath))
            {
                string dirName = Path.GetFileName(dirPath);
                string destinationDirPath = Path.Combine(destinationPath, dirName);
                CloneFolderStructure(dirPath, destinationDirPath);
            }
        }
        public static void CopyDirectory(DirectoryInfo source, DirectoryInfo destination)
        {
            if (!destination.Exists)
            {
                destination.Create();
            }
            FileInfo[] files = source.GetFiles();
            foreach (FileInfo file in files)
            {
                file.CopyTo(Path.Combine(destination.FullName,
                    file.Name));
            }
            DirectoryInfo[] dirs = source.GetDirectories();
            foreach (DirectoryInfo dir in dirs)
            {
                string destinationDir = Path.Combine(destination.FullName, dir.Name);
                CopyDirectory(dir, new DirectoryInfo(destinationDir));
            }
        }
    }
}