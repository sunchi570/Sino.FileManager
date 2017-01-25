﻿using Sino.FileManager.Core;
using System;
using System.Threading.Tasks;
using System.IO;

namespace Sino.FileManager
{
    public class FileManager : IFileManager
    {
        public IFilenameParser FilenameParser { get; set; }

        public IFileStorage FileStorage { get; set; }

        public FileManager(IFileStorage filestorage, IFilenameParser filenameparser)
        {
            if (filestorage == null)
            {
                throw new ArgumentNullException("filestorage");
            }
            if (filenameparser == null)
            {
                throw new ArgumentNullException("filenameparser");
            }

            FilenameParser = filenameparser;
            FileStorage = filestorage;

            FileStorage.Init();
        }

        public async Task<IFileEntry> GetAsync(string arg)
        {
            return await GetAsync(arg, 0, -1);
        }

        public async Task<IFileEntry> GetAsync(string arg, long pos, long length)
        {
            if (FileStorage == null)
            {
                throw new NullReferenceException();
            }
            return await FileStorage.GetEntryAsync(arg, pos, length);
        }

        public async Task<string> SaveAsync(string path)
        {
            if(string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException("path");
            }

            if (File.Exists(path))
            {
                string filename = Path.GetFileName(path);
                using (var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    return await SaveAsync(fs, filename);
                }
            }
            else
            {
                throw new FileNotFoundException();
            }
        }

        public async Task<string> SaveAsync(Stream stream, string filename)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }
            if (string.IsNullOrEmpty(filename))
            {
                throw new ArgumentNullException("filename");
            }

            if (FileStorage == null)
            {
                throw new NullReferenceException();
            }
            if (FilenameParser == null)
            {
                throw new NullReferenceException();
            }

            stream.Position = 0;
            string savepath = FilenameParser.Parse(filename);
            return await FileStorage.SaveEntryAsync(stream, savepath);
        }
    }
}
