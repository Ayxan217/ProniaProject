﻿using Microsoft.EntityFrameworkCore;
using ProniaProject.Utils.Enums;
using System.IO;

namespace ProniaProject.Utils
{
    public static class FileValidator
    {
        static string path = string.Empty;

        public static string BuildPath(string fileName, params string[] roots)
        {
            

            for (int i = 0; i < roots.Length; i++)
            {
                path = Path.Combine(path, roots[i]);
            }

            path = Path.Combine(path, fileName);
            return path;
        }


        public static bool ValidateType(this IFormFile file, string type)
        {
            if(file.ContentType.Contains(type))
            {
                return true;
            }
            return false;
        }

        public static bool ValidateSize(this IFormFile file, FileSize fileSize, int size )
        {
            switch (fileSize)
            {
                case FileSize.Kb:
                    return file.Length <= size * 1024;
                case FileSize.Mb:
                    return file.Length <= size * 1024 * 1024;
                case FileSize.Gb:
                    return file.Length <= size * Math.Pow(1024, 3);

            }
            return false;
        }

        public async static Task<string> CreateFileAsync(this IFormFile file, params string[] roots)
        {
            string originalFileName = file.FileName;
            int lastDotIndex = originalFileName.LastIndexOf('.');
            string fileExtension = originalFileName.Substring(lastDotIndex);

            string fileName = string.Concat(Guid.NewGuid().ToString(), fileExtension);
            

            path = BuildPath(fileName, roots);

            using (FileStream fileStream = new FileStream(path, FileMode.Create))
            {

                await file.CopyToAsync(fileStream);
            }

            return fileName;

            

        }

        public static void DeleteImage(this string fileName, params string[] roots)
        {
           

            path = BuildPath(fileName, roots);

            File.Delete(path);
        }

    }
}
