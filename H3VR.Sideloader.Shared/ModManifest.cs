﻿namespace H3VR.Sideloader.Shared
{
    internal enum AssetType
    {
        Texture,
        Prefab,
        Mesh,
        Material,
        AudioClip,
        FVRObject,
        ItemSpawnerID
    }

    internal class AssetMapping
    {
        public AssetType Type { get; set; }
        public string Target { get; set; }
        public string Path { get; set; }
    }

    internal class ModManifest
    {
        public const string MANIFEST_FILE_NAME = "manifest.json";
        public const string MANIFEST_REVISION = "1";
        public string ManifestRevision { get; set; }
        public string Guid { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public AssetMapping[] AssetMappings { get; set; }
    }
}