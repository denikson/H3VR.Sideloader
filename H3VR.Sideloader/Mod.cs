﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using MicroJson;
using UnityEngine;

namespace H3VR.Sideloader
{
    internal class Mod
    {
        private const string MANIFEST_FILE = "manifest.json";
        public ModManifest Manifest { get; private set; }
        public string ModPath { get; private set; }
        private ZipFile Archive { get; set; }
        public string Name => $"{Manifest.Name} {Manifest.Version}";

        private Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();

        public static Mod LoadFromDir(string path)
        {
            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException("The path is not a valid mod directory!");

            var manifestPath = Path.Combine(path, MANIFEST_FILE);
            if (!File.Exists(manifestPath))
                throw new FileNotFoundException("Failed to find manifest.json, the directory is not valid!");

            var manifest = new JsonSerializer().Deserialize<ModManifest>(File.ReadAllText(manifestPath));
            if (!manifest.Verify(out var errors))
                throw new ModLoadException(
                    $"The manifest file is invalid. The following problems were found: {errors.Aggregate(new StringBuilder(), (sb, s) => sb.AppendLine($"* {s}"))}");

            return new Mod
            {
                Manifest = manifest,
                ModPath = path
            };
        }

        public static Mod LoadFromZip(string path)
        {
            var file = new ZipFile(path);

            var manifestEntry = file.GetEntry(MANIFEST_FILE);

            if (manifestEntry == null)
                throw new ModLoadException("The archive is not a valid zipmod.");

            using var manifestStream = file.GetInputStream(manifestEntry);
            using var s = new StreamReader(manifestStream);

            var manifest = new JsonSerializer().Deserialize<ModManifest>(s.ReadToEnd());
            if (!manifest.Verify(out var errors))
                throw new ModLoadException(
                    $"The manifest file is invalid. The following problems were found: {errors.Aggregate(new StringBuilder(), (sb, s) => sb.AppendLine($"* {s}"))}");

            return new Mod
            {
                Manifest = manifest,
                ModPath = path,
                Archive = file
            };
        }

        public void RegisterTreeAssets(AssetTree tree, AssetType type)
        {
            foreach (var manifestAssetMapping in Manifest.AssetMappings.Where(m => m.Type == type))
            {
                if (!FileExists(manifestAssetMapping.Path))
                    Sideloader.Logger.LogWarning(
                        $"[{Name}] Asset `{manifestAssetMapping.Path}` of type `{type}` does not exist in the mod, skipping...");
                tree.AddMod(manifestAssetMapping.Target, manifestAssetMapping.Path, this);
                textures[manifestAssetMapping.Path] = null;
            }
        }

        public Texture2D LoadTexture(string path)
        {
            if (!textures.TryGetValue(path, out var tex))
                throw new FileNotFoundException($"Tried to load non-existent texture `{path}` from mod {Name}");

            if (tex != null) return tex;

            tex = textures[path] = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            tex.LoadImage(LoadBytes(path));
            return tex;
        }

        private byte[] LoadBytes(string path)
        {
            if (!FileExists(path))
                throw new FileNotFoundException($"`{path}` does not exist in {Name}");
            var entry = Archive.GetEntry(path);
            using var stream = Archive != null
                ? Archive.GetInputStream(entry)
                : File.OpenRead(Path.Combine(ModPath, path));
            var result = new byte[entry?.Size ?? stream.Length];
            stream.Read(result, 0, result.Length);
            return result;
        }

        private bool FileExists(string path)
        {
            if (Archive != null)
                return Archive.GetEntry(path) != null;
            return File.Exists(Path.Combine(ModPath, path));
        }
    }

    internal class ModLoadException : Exception
    {
        public ModLoadException(string msg) : base(msg)
        {
        }
    }
}