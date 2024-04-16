using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using BepInEx;

namespace TheOtherRoles.Modules;

public class DependentDownload : ManagerBase<DependentDownload>
{
    public static readonly string DllPath = Path.Combine(Paths.GameRootPath, "Dependent");
    public static readonly DirectoryInfo DllDir = new(DllPath);
    private readonly HttpClient _client = new();
    private readonly Dictionary<string, List<string>> Map = new();
    internal readonly List<string> HasFileNames = [];


    public DependentDownload()
    {
        if (!Directory.Exists(DllPath))
            Directory.CreateDirectory(DllPath);
    }

    public async void CheckDependent(string fileName, string url, bool formDownload = true)
    {
        if (AppDomain.CurrentDomain.GetAssemblies().Any(n => n.GetName().Name == fileName.Replace(Path.GetExtension(fileName), string.Empty)))
            return;
        
        var filePath = Path.Combine(DllPath, fileName);
        if (HasFileNames.Contains(fileName))
            goto load;
        
        var stream = formDownload ? await DownloadDependent(url) : await ReadDependent(fileName);
        var file = File.Open(filePath, FileMode.OpenOrCreate);
        await stream.CopyToAsync(file);
        stream.Close();
        file.Close();
        HasFileNames.Add(fileName);
        
        load:
        Assembly.LoadFile(filePath);
        Info($"Loaded File:{fileName} Url:{url}");
    }
    
    public void DownLoadDependentMap(string mapUrl, string option)
    {
        var mapStream = _client.GetStreamAsync(mapUrl.GithubUrl()).Result;
        mapStream.StartRead(Read, out _);
        var Url = Map["Url"][0];
        var DLLs = Map[option];
        foreach (var dll in DLLs)
        {
            CheckDependent(dll, Path.Combine(Url, dll));
        }
    }

    public void Read(string s, int i)
    {
        if (s.IsNullOrWhiteSpace())
            return;

        var map = Map;
        var data =  s.Split(":");
        var option = data[0];
        if (option == "Url")
        {
            map[option] = [s.Replace("Url:", string.Empty)];
            return;
        }
        
        var list = data[1].Contains(',') ? data[1].Split(",").ToList() : [data[1]];

        map[option] = list;
    }

    public async Task<Stream> DownloadDependent(string url)
    {
        Info($"Download Url{url}");
        var stream = await _client.GetStreamAsync(url.GithubUrl());
        return await Task.FromResult(stream);
    }

    public async Task<Stream> ReadDependent(string fileName)
    {
        var path = "TheOtherRoles.Resources.Dependent." + fileName;
        var stream = typeof(DependentDownload).Assembly.GetManifestResourceStream(path);
        return await Task.FromResult(stream);
    }
}