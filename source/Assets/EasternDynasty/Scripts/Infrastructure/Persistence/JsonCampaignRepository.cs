using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace EasternDynasty
{
    /// <summary>Unity JSON adapter. Slot selection and validation happen before any state is returned or written.</summary>
    public sealed class JsonCampaignRepository : ICampaignRepository
    {
        public const string LegacyFileName = "campaign.json";
        public const string YouzhouFileName = "youzhou-40-v1.json";
        private readonly string directory;
        private readonly object sync = new object();

        public JsonCampaignRepository(string saveDirectory)
        {
            if (string.IsNullOrWhiteSpace(saveDirectory)) throw new ArgumentException("必须指定存档目录。", "saveDirectory");
            directory = Path.GetFullPath(saveDirectory);
        }

        public bool Exists(bool youzhou)
        {
            string path = Slot(youzhou);
            return File.Exists(path) || File.Exists(path + ".bak");
        }

        public World Load(bool youzhou, out bool restoredBackup)
        {
            lock (sync)
            {
                restoredBackup = false;
                string path = Slot(youzhou);
                var errors = new List<string>();
                foreach (string candidate in new[] { path, path + ".bak" })
                {
                    if (!File.Exists(candidate)) continue;
                    try
                    {
                        World loaded = ReadValidated(candidate, youzhou);
                        restoredBackup = candidate.EndsWith(".bak", StringComparison.Ordinal);
                        return loaded;
                    }
                    catch (Exception error) when (error is InvalidDataException || error is IOException || error is UnauthorizedAccessException)
                    { errors.Add(Path.GetFileName(candidate) + ": " + error.Message); }
                }
                if (errors.Count == 0) throw new FileNotFoundException("没有找到该征程的存档或备份。", path);
                throw new InvalidDataException("主存档和备份均无法读取。 " + string.Join(" | ", errors.ToArray()));
            }
        }

        public void Save(World world)
        {
            if (world == null) throw new ArgumentNullException("world");
            RequireValid(world, world.IsYouzhou);
            string json = JsonUtility.ToJson(world, true);
            lock (sync)
            {
                Directory.CreateDirectory(directory);
                string path = Slot(world.IsYouzhou);
                string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    byte[] bytes = new UTF8Encoding(false).GetBytes(json);
                    using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
                    if (File.Exists(path))
                    {
                        bool rotatePrimary = true;
                        try { ReadValidated(path, world.IsYouzhou); }
                        catch (InvalidDataException) { rotatePrimary = false; }
                        // A corrupt primary must never replace the valid backup used for recovery.
                        File.Replace(temporary, path, rotatePrimary ? path + ".bak" : null);
                    }
                    else File.Move(temporary, path);
                }
                finally
                {
                    try { if (File.Exists(temporary)) File.Delete(temporary); }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }
            }
        }

        private string Slot(bool youzhou)
        { return Path.Combine(directory, youzhou ? YouzhouFileName : LegacyFileName); }

        private static World ReadValidated(string path, bool youzhou)
        {
            string json = File.ReadAllText(path, Encoding.UTF8);
            World world;
            try { world = JsonUtility.FromJson<World>(json); }
            catch (Exception error) when (error is ArgumentException || error is FormatException)
            { throw new InvalidDataException("存档JSON格式损坏。", error); }
            RequireValid(world, youzhou);
            return world;
        }

        private static void RequireValid(World world, bool youzhou)
        {
            if (world == null) throw new InvalidDataException("存档为空。");
            string problem;
            if (!world.IsValid(out problem)) throw new InvalidDataException(problem);
            if (world.IsYouzhou != youzhou) throw new InvalidDataException("存档地图与目标槽位不一致。");
        }
    }
}
