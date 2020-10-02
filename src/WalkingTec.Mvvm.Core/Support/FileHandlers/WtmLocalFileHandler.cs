using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using WalkingTec.Mvvm.Core.Extensions;
using WalkingTec.Mvvm.Core.Models;

namespace WalkingTec.Mvvm.Core.Support.FileHandlers
{

    [Display(Name = "local")]
    public class WtmLocalFileHandler : WtmFileHandlerBase
    {
        private static string _modeName = "DataBase";

        public WtmLocalFileHandler(Configs config, string csName) : base(config, csName)
        {
        }

        public override IWtmFile GetFile(string id, bool withData)
        {
            IWtmFile rv;
            using (var dc = _config.CreateDC(_cs))
            {
                rv = dc.Set<FileAttachment>().CheckID(id).Where(x => x.SaveMode == _modeName).FirstOrDefault();
            }
            if(withData == true)
            {
                rv.DataStream = File.OpenRead(rv.Path);
            }
            return rv;

        }


        public override IWtmFile Upload(string fileName, long fileLength, Stream data, string group = null, string subdir = null, string extra = null)
        {
            FileAttachment file = new FileAttachment();
            file.FileName = fileName;
            file.Length = fileLength;
            file.UploadTime = DateTime.Now;
            file.SaveMode = _modeName;
            file.ExtraInfo = extra;
            file.IsTemprory = true;
            var ext = string.Empty;
            if (string.IsNullOrEmpty(fileName) == false)
            {
                var dotPos = fileName.LastIndexOf('.');
                ext = fileName.Substring(dotPos + 1);
            }
            file.FileExt = ext;

            var groupdir = _config.FileUploadOptions.Groups.First().Value; ;
            if (string.IsNullOrEmpty(group) == false && _config.FileUploadOptions?.Groups.ContainsKey(group) == true)
            {
                groupdir = _config.FileUploadOptions.Groups[group];
            }
            else
            {

            }

            string pathHeader = groupdir;
            if (pathHeader.StartsWith("."))
            {
                pathHeader = Path.Combine(_config.HostRoot, pathHeader); 
            }
            if (string.IsNullOrEmpty(subdir) == false)
            {
                pathHeader = Path.Combine(pathHeader, subdir);
            }
            else
            {
                var sub = WtmFileProvider._subDirFunc?.Invoke(this);
                if(string.IsNullOrEmpty(sub)== false)
                {
                    pathHeader = Path.Combine(pathHeader, sub);
                }
            }
            if (!Directory.Exists(pathHeader))
            {
                Directory.CreateDirectory(pathHeader);
            }
            var fullPath = Path.Combine(pathHeader, $"{Guid.NewGuid().ToNoSplitString()}.{file.FileExt}");
            file.Path = fullPath;
            using (var fileStream = File.Create(fullPath))
            {
                data.CopyTo(fileStream);
            }
            using (var dc = _config.CreateDC(_cs))
            {
                dc.AddEntity(file);
                dc.SaveChanges();
            }
            return file;
        }

        public override string DeleteFile(string id)
        {
            var path = base.DeleteFile(id);
            if (string.IsNullOrEmpty(path) == false)
            {
                File.Delete(path);
            }
            return path;
        }
    }

}