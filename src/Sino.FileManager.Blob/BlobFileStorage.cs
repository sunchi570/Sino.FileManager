﻿using Sino.FileManager.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.Azure;

namespace Sino.FileManager
{
    public class BlobFileStorage : IFileStorage
    {
        public const string DefaultConnectionString = "StorageConnectionString";
        public const string DefaultContainerString = "DefaultContainer";

        public CloudStorageAccount Account { get; }

        public string DefaultContainer { get; set; }

        protected CloudBlobContainer BlobContainer { get; set; }

        public BlobFileStorage()
            : this(DefaultConnectionString)
        { }

        public BlobFileStorage(string name)
           : this(CloudStorageAccount.Parse(CloudConfigurationManager.GetSetting(name)))
        { }

        public BlobFileStorage(CloudStorageAccount account)
        {
            Account = account;
            DefaultContainer = DefaultContainerString;
        }

        protected ICloudBlob GetBlockBlob(string filenames)
        {
            CloudBlobDirectory currentDir = null;
            string filename = Path.GetFileName(filenames);

            string[] dirs = Path.GetDirectoryName(filenames).Split('\\');
            if (dirs.Length > 0)
            {
                currentDir = BlobContainer.GetDirectoryReference(dirs[0]);
                for (int i = 1; i < dirs.Length; i++)
                {
                    currentDir = currentDir.GetDirectoryReference(dirs[i]);
                }
                return currentDir.GetBlockBlobReference(filename);
            }
            return BlobContainer.GetBlockBlobReference(filename);
        }

        #region IFileStorage Impl

        public void Init()
        {
            var blobClient = Account.CreateCloudBlobClient();
            BlobContainer = blobClient.GetContainerReference(DefaultContainer);
            BlobContainer.CreateIfNotExists();
        }

        public IEnumerable<IFileEntry> GetEntries(IEnumerable<IFileEntry> filenames)
        {
            var list = new List<IFileEntry>();
            if (filenames != null && filenames.Count() > 0)
            {
                foreach (var item in filenames)
                {
                    list.Add(GetEntry(item.FileName, item.StartPosition, item.Length));
                }
            }
            return list;
        }

        public IFileEntry GetEntry(string filename)
        {
            return GetEntry(filename, 0, -1);
        }

        public IFileEntry GetEntry(string filename, long pos, long length)
        {
            if(string.IsNullOrEmpty(filename))
            {
                throw new ArgumentNullException("filename");
            }
            if (pos < 0)
            {
                throw new ArgumentOutOfRangeException("pos");
            }
            if(length < -1)
            {
                throw new ArgumentOutOfRangeException("length");
            }

            DefaultFileEntry entry = new DefaultFileEntry
            {
                FileName = filename,
                StartPosition = pos,
                Length = length,
                Stream = new MemoryStream()
            };

            var blockblob = GetBlockBlob(filename);
            if (blockblob.Exists())
            {
                blockblob.DownloadRangeToStream(entry.Stream, pos, length == -1 ? (long?)null : length);
            }
            else
            {
                entry.Stream = null;
            }
            return entry;
        }

        public string SaveEntry(Stream stream, string filename)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }
            if (string.IsNullOrEmpty(filename))
            {
                throw new ArgumentNullException("filename");
            }
            var blockblob = GetBlockBlob(filename);
            blockblob.UploadFromStream(stream);
            return filename;
        }

        #endregion
    }
}
