using NetCord;
using OriginsBot.JsonModels;
using System.IO.Compression;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace OriginsBot.Services
{
    public static class OriginDataService
    {
        public const string DataDirectoryName = "data";
        public static readonly string DataDirectory = Path.Combine(Directory.GetCurrentDirectory(), DataDirectoryName);

        public const string CreatorsDirectoryName = "creators";
        public static readonly string CreatorsDirectory = Path.Combine(DataDirectory, CreatorsDirectoryName);

        public const string PacksDirectoryName = "packs";
        public static readonly string PacksDirectory = Path.Combine(DataDirectory, PacksDirectoryName);

        public const string OrderedFileName = "ordered.txt";
        public static readonly string OrderedCreatorsFile = Path.Combine(CreatorsDirectory, OrderedFileName);
        public static readonly string OrderedPacksFile = Path.Combine(PacksDirectory, OrderedFileName);


        public const string BackupDirectoryName = "backup";
        public static readonly string BackupDirectory = Path.Combine(Directory.GetCurrentDirectory(), BackupDirectoryName);
        public static readonly string BackupCreatorsDirectory = Path.Combine(BackupDirectory, CreatorsDirectoryName);
        public static readonly string BackupPacksDirectory = Path.Combine(BackupDirectory, PacksDirectoryName);
        

        public const string CreatorJsonFileName = "creator.json";

        public const string PackJsonFileName = "pack.json";
        public const string PackLangFileName = "en_US.lang";
        public const string PackIconsDirectoryName = "icons";


        public static readonly IReadOnlyList<string> Impacts = [
            "<:impact_none:1064378719147413644> <:impact_none:1064378719147413644> <:impact_none:1064378719147413644>",
            "<:impact_low:1064378716949590047> <:impact_none:1064378719147413644> <:impact_none:1064378719147413644>",
            "<:impact_medium:1064378715552882779> <:impact_medium:1064378715552882779> <:impact_none:1064378719147413644>",
            "<:impact_high:1064378713669636127> <:impact_high:1064378713669636127> <:impact_high:1064378713669636127>",
            "<:impact_major:1064378712088395786> <:impact_major:1064378712088395786> <:impact_major:1064378712088395786>"
        ];


        public static readonly JsonSerializerOptions JsonOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            AllowTrailingCommas = true,
            MaxDepth = 8,
            ReadCommentHandling = JsonCommentHandling.Skip,
            WriteIndented = true,
            NewLine = "\n",
            AllowDuplicateProperties = false,
        };


        /**
         * <summary>This technically <i>could</i> be null, but it never <i>should</i> be, so long as the first <see cref="TryReload(out Exception?)"/> call in <see cref="Program"/> is successful.</summary>
         * <remarks>If this <i>is</i> null past that point, then something's went <b>seriously</b> wrong with <see cref="OriginDataService"/>.</remarks>
         */
        public static Data Data => _data!;
        private static Data? _data;


        public static bool TryReload(out Exception? exception)
        {
            try
            {
                Console.WriteLine("Reloading Origin data...");

                Dictionary<ulong, Exception> invalidCreatorIds = [];
                HashSet<ulong> exclusiveCreatorIds = [];
                List<ulong> orderedCreatorIds = [];
                string[] orderedCreatorIdStrings = File.ReadAllLines(OrderedCreatorsFile);

                foreach (string creatorIdString in orderedCreatorIdStrings)
                    if (ulong.TryParse(creatorIdString, out ulong creatorId) && exclusiveCreatorIds.Add(creatorId))
                        orderedCreatorIds.Add(creatorId);
                foreach (string creatorDirectory in Directory.EnumerateDirectories(CreatorsDirectory))
                    if (ulong.TryParse(Path.GetFileName(creatorDirectory), out ulong creatorId) && exclusiveCreatorIds.Add(creatorId))
                        orderedCreatorIds.Add(creatorId);


                Dictionary<PackId, Exception> invalidPackIds = [];
                HashSet<PackId> exclusivePackIds = [];
                List<PackId> orderedPackIds = [];
                string[] orderedPackIdStrings = File.ReadAllLines(OrderedPacksFile);
                if (orderedPackIdStrings.Length == 0)
                    throw new Exception($"The '{OrderedPacksFile}' file must contain at least one valid pack id for the main pack.");

                foreach (string packIdString in orderedPackIdStrings)
                {
                    PackId packId = new(packIdString);
                    if (exclusivePackIds.Add(packId))
                        orderedPackIds.Add(packId);
                }
                foreach (string packIdString in Directory.EnumerateDirectories(PacksDirectory))
                {
                    PackId packId = new(Path.GetFileName(packIdString));
                    if (exclusivePackIds.Add(packId))
                        orderedPackIds.Add(packId);
                }
                

                Dictionary<ulong, CreatorData> creatorIds = [];
                Dictionary<PackId, PackData> packIds = [];
                Dictionary<OriginId, OriginData> originIds = [];

                Dictionary<ulong, HashSet<PackId>> creatorIdToPackIds = [];
                List<OriginId> orderedOriginIds = [];

                PackLang mainLang = PackLang.FromPackId(orderedPackIds[0]);

                foreach (PackId packId in orderedPackIds)
                    try
                    {
                        PackData pack = PackData.FromPackId(packId, mainLang);

                        foreach (ulong creatorId in pack.CreatorIds)
                            if (creatorIdToPackIds.TryGetValue(creatorId, out HashSet<PackId>? packs))
                                packs.Add(packId);
                            else
                                creatorIdToPackIds.Add(creatorId, [packId]);
                        
                        packIds.Add(packId, pack);
                        foreach (OriginData origin in pack.OrderedOrigins)
                        {
                            originIds.Add(origin.Id, origin);
                            orderedOriginIds.Add(origin.Id);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"The following exception was thrown by the pack '{packId.Value}' during reloading:\n{ex}");
                        invalidPackIds.Add(packId, ex);
                    }


                foreach (ulong creatorId in orderedCreatorIds)
                    try
                    {
                        if (!creatorIdToPackIds.TryGetValue(creatorId, out HashSet<PackId>? creatorPackIds))
                            creatorPackIds = [];

                        IEnumerable<PackData> creatorPacks = orderedPackIds
                            .Where(packId => creatorPackIds.Contains(packId))
                            .Select(packId => packIds[packId]);

                        CreatorData creator = CreatorData.FromCreatorId(creatorId, creatorPacks);

                        creatorIds.Add(creatorId, creator);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"The following exception was thrown by the creator '{creatorId}' during reloading:\n{ex}");
                        invalidCreatorIds.Add(creatorId, ex);
                    }

                _data = new(
                    creatorIds,
                    packIds,
                    originIds,

                    invalidCreatorIds,
                    invalidPackIds,

                    orderedCreatorIds,
                    orderedPackIds,
                    orderedOriginIds,

                    orderedCreatorIds
                    .Select(creatorId => new KeyValuePair<ulong, string>(creatorId, creatorIds.TryGetValue(creatorId, out CreatorData? creator) ? creator.Name : $"<@{creatorId}>"))
                    .GroupBy(kv => kv.Value)
                    .SelectMany(group =>
                    {
                        bool isDuplicate = group.Count() > 1;

                        return group.Select(kv => new KeyValuePair<ulong, string>(
                            kv.Key, isDuplicate ? $"{kv.Value} (id: {kv.Key})" : kv.Value));
                    }).ToDictionary(),

                    orderedPackIds
                    .Select(packId => new KeyValuePair<PackId, string>(packId, packIds.TryGetValue(packId, out PackData? pack) ? pack.Name : $"!{packId}!"))
                    .GroupBy(kv => kv.Value)
                    .SelectMany(group =>
                    {
                        bool isDuplicate = group.Count() > 1;

                        return group.Select(kv => new KeyValuePair<PackId, string>(
                            kv.Key, isDuplicate ? $"{kv.Value} (id: {kv.Key.Value})" : kv.Value));
                    }).ToDictionary(),

                    originIds
                    .Select(kv => new KeyValuePair<OriginId, string>(kv.Key, kv.Value.Name))
                    .GroupBy(kv => kv.Value)
                    .SelectMany(group =>
                    {
                        bool isDuplicate = group.Count() > 1;

                        return group.Select(kv => new KeyValuePair<OriginId, string>(
                            kv.Key, isDuplicate ? $"{kv.Value} (id: {kv.Key.GetFullId()})" : kv.Value));
                    }).ToDictionary()
                );

                exception = null;

                Console.WriteLine("Origin data reloaded.");
                return true;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"The following exception was thrown during reloading:\n{ex}");

                exception = ex;

                Console.Error.WriteLine("Origin data reloading failed.");
                return false;
            }
        }


        public static DateTime BackupCreator(ulong creatorId)
        {
            DateTime time = DateTime.UtcNow;

            string creatorJsonFile = Path.Combine(CreatorsDirectory, creatorId.ToString(), CreatorJsonFileName);
            string backupCreatorDirectory = Path.Combine(BackupCreatorsDirectory, creatorId.ToString());
            string backupCreatorJsonFile = Path.Combine(backupCreatorDirectory, $"{time:yyyyMMddHHmm} {CreatorJsonFileName}");

            if (File.Exists(creatorJsonFile) && !File.Exists(backupCreatorJsonFile))
            {
                Directory.CreateDirectory(backupCreatorDirectory);
                File.Copy(creatorJsonFile, backupCreatorJsonFile);
            }

            return time;
        }

        public static DateTime BackupPack(PackId packId)
        {
            DateTime time = DateTime.UtcNow;

            string packJsonFile = Path.Combine(PacksDirectory, packId.Value, PackJsonFileName);
            string backupPackDirectory = Path.Combine(BackupPacksDirectory, packId.Value);
            string backupPackJsonFile = Path.Combine(backupPackDirectory, $"{time:yyyyMMddHHmm} {PackJsonFileName}");

            if (File.Exists(packJsonFile) && !File.Exists(backupPackJsonFile))
            {
                Directory.CreateDirectory(backupPackDirectory);
                File.Copy(packJsonFile, backupPackJsonFile);
            }

            string packLangFile = Path.Combine(PacksDirectory, packId.Value, PackLangFileName);
            string backupPackLangFile = Path.Combine(backupPackDirectory, $"{time:yyyyMMddHHmm} {PackLangFileName}");

            if (File.Exists(packLangFile) && !File.Exists(backupPackLangFile))
            {
                Directory.CreateDirectory(backupPackDirectory);
                File.Copy(packLangFile, backupPackLangFile);
            }

            return time;
        }


        public static void RestoreCreator(ulong creatorId, DateTime time)
        {
            string backupCreatorDirectory = Path.Combine(BackupCreatorsDirectory, creatorId.ToString());
            string backupCreatorJsonFile = Path.Combine(backupCreatorDirectory, $"{time:yyyyMMddHHmm} {CreatorJsonFileName}");

            if (File.Exists(backupCreatorJsonFile))
            {
                string creatorJsonFile = Path.Combine(CreatorsDirectory, creatorId.ToString(), CreatorJsonFileName);
                if (File.Exists(creatorJsonFile))
                    File.Delete(creatorJsonFile);

                File.Copy(backupCreatorJsonFile, creatorJsonFile);
            }
        }

        public static void RestorePack(PackId packId, DateTime time)
        {
            string backupPackDirectory = Path.Combine(BackupPacksDirectory, packId.Value);
            string backupPackJsonFile = Path.Combine(backupPackDirectory, $"{time:yyyyMMddHHmm} {PackJsonFileName}");

            if (File.Exists(backupPackJsonFile))
            {
                string packJsonFile = Path.Combine(PacksDirectory, packId.Value, PackJsonFileName);
                if (File.Exists(packJsonFile))
                    File.Delete(packJsonFile);

                File.Copy(backupPackJsonFile, packJsonFile);
            }

            string backupPackLangFile = Path.Combine(backupPackDirectory, $"{time:yyyyMMddHHmm} {PackLangFileName}");

            if (File.Exists(backupPackLangFile))
            {
                string packLangFile = Path.Combine(PacksDirectory, packId.Value, PackLangFileName);
                if (File.Exists(packLangFile))
                    File.Delete(packLangFile);

                File.Copy(backupPackLangFile, packLangFile);
            }
        }

        
        public static bool TryAddCreator(ulong creatorId, JsonCreator jsonCreator, out Exception? creatorException)
        {
            creatorException = null;

            string creatorDirectory = Path.Combine(CreatorsDirectory, creatorId.ToString());
            Directory.CreateDirectory(creatorDirectory);

            string creatorJsonFile = Path.Combine(creatorDirectory, CreatorJsonFileName);
            using (FileStream creatorJsonStream = File.Create(creatorJsonFile))
            {
                JsonSerializer.Serialize(creatorJsonStream, jsonCreator, JsonOptions);
            }

            bool success = TryReload(out _);
            if (!success)
                return false;

            if (Data.InvalidCreatorIds.TryGetValue(creatorId, out creatorException))
            {
                if (Directory.Exists(creatorDirectory))
                    Directory.Delete(creatorDirectory, recursive: true);

                if (!TryReload(out _))
                    return false;
            }

            return true;
        }

        public static bool TryMoveCreator(ulong oldCreatorId, ulong newCreatorId, out Exception? creatorException)
        {
            creatorException = null;

            DateTime oldTime = BackupCreator(oldCreatorId);
            DateTime newTime = BackupCreator(newCreatorId);
            
            string newCreatorDirectory = Path.Combine(CreatorsDirectory, newCreatorId.ToString());
            if (Directory.Exists(newCreatorDirectory))
                Directory.Delete(newCreatorDirectory, recursive: true);

            string oldCreatorDirectory = Path.Combine(CreatorsDirectory, oldCreatorId.ToString());
            if (Directory.Exists(oldCreatorDirectory))
                Directory.Move(oldCreatorDirectory, newCreatorDirectory);
            
            bool success = TryReload(out _);
            if (!success)
                return false;

            if (Data.InvalidCreatorIds.TryGetValue(newCreatorId, out creatorException))
            {
                RestoreCreator(oldCreatorId, oldTime);
                if (Directory.Exists(newCreatorDirectory))
                    Directory.Delete(newCreatorDirectory, recursive: true);
                RestoreCreator(newCreatorId, newTime);

                if (!TryReload(out _))
                    return false;
            }

            return true;
        }

        public static bool TryUpdateCreator(ulong creatorId, JsonCreator jsonCreator, out Exception? creatorException)
        {
            creatorException = null;

            DateTime time = BackupCreator(creatorId);

            string creatorJsonFile = Path.Combine(CreatorsDirectory, creatorId.ToString(), CreatorJsonFileName);
            using (FileStream creatorJsonStream = File.Create(creatorJsonFile))
            {
                JsonSerializer.Serialize(creatorJsonStream, jsonCreator, JsonOptions);
            }

            bool success = TryReload(out _);
            if (!success)
                return false;

            if (Data.InvalidCreatorIds.TryGetValue(creatorId, out creatorException))
            {
                RestoreCreator(creatorId, time);

                if (!TryReload(out _))
                    return false;
            }

            return true;
        }

        public static bool TryRemoveCreator(ulong creatorId, out Exception? creatorException)
        {
            creatorException = null;

            DateTime time = BackupCreator(creatorId);

            string creatorDirectory = Path.Combine(CreatorsDirectory, creatorId.ToString());
            if (Directory.Exists(creatorDirectory))
                Directory.Delete(creatorDirectory, recursive: true);

            bool success = TryReload(out _);
            if (!success)
                return false;

            if (Data.InvalidCreatorIds.TryGetValue(creatorId, out creatorException))
            {
                RestoreCreator(creatorId, time);

                if (!TryReload(out _))
                    return false;
            }

            return true;
        }


        public static bool TryAddPack(PackId packId, JsonPack jsonPack, string fullLang, out Exception? packException)
        {
            packException = null;

            string packDirectory = Path.Combine(PacksDirectory, packId.Value);
            Directory.CreateDirectory(Path.Combine(packDirectory, PackIconsDirectoryName));

            string packJsonFile = Path.Combine(packDirectory, PackJsonFileName);
            using (FileStream packJsonStream = File.Create(packJsonFile))
            {
                JsonSerializer.Serialize(packJsonStream, jsonPack, JsonOptions);
            }

            string packLangFile = Path.Combine(packDirectory, PackLangFileName);
            File.WriteAllText(packLangFile, fullLang);

            bool success = TryReload(out _);
            if (!success)
                return false;

            if (Data.InvalidPackIds.TryGetValue(packId, out packException))
            {
                if (Directory.Exists(packDirectory))
                    Directory.Delete(packDirectory, recursive: true);

                if (!TryReload(out _))
                    return false;
            }
            
            return true;
        }
        
        public static bool TryMovePack(PackId oldPackId, PackId newPackId, out Exception? packException)
        {
            packException = null;

            DateTime oldTime = BackupPack(oldPackId);
            DateTime newTime = BackupPack(newPackId);

            string newPackDirectory = Path.Combine(PacksDirectory, newPackId.Value);
            if (Directory.Exists(newPackDirectory))
                Directory.Delete(newPackDirectory, recursive: true);

            string oldPackDirectory = Path.Combine(PacksDirectory, oldPackId.Value);
            if (Directory.Exists(oldPackDirectory))
                Directory.Move(oldPackDirectory, newPackDirectory);

            bool success = TryReload(out _);
            if (!success)
                return false;

            if (Data.InvalidPackIds.TryGetValue(newPackId, out packException))
            {
                RestorePack(oldPackId, oldTime);
                if (Directory.Exists(newPackDirectory))
                    Directory.Delete(newPackDirectory, recursive: true);
                RestorePack(newPackId, newTime);

                if (!TryReload(out _))
                    return false;
            }

            return true;
        }
        
        public static bool TryUpdatePack(PackId packId, JsonPack? jsonPack, string? fullLang, out Exception? packException)
        {
            packException = null;

            string packDirectory = Path.Combine(PacksDirectory, packId.Value);
            Directory.CreateDirectory(Path.Combine(packDirectory, PackIconsDirectoryName));

            if (jsonPack != null)
            {
                string packJsonFile = Path.Combine(packDirectory, PackJsonFileName);
                using FileStream packJsonStream = File.Create(packJsonFile);
                JsonSerializer.Serialize(packJsonStream, jsonPack, JsonOptions);
            }

            if (fullLang != null)
            {
                string packLangFile = Path.Combine(packDirectory, PackLangFileName);
                File.WriteAllText(packLangFile, fullLang);
            }

            bool success = TryReload(out _);
            if (!success)
                return false;

            if (Data.InvalidPackIds.TryGetValue(packId, out packException))
            {
                if (Directory.Exists(packDirectory))
                    Directory.Delete(packDirectory, recursive: true);

                if (!TryReload(out _))
                    return false;
            }

            return true;
        }

        public static bool TryRemovePack(PackId packId, out Exception? packException)
        {
            packException = null;

            DateTime time = BackupPack(packId);

            string packDirectory = Path.Combine(PacksDirectory, packId.Value);
            if (Directory.Exists(packDirectory))
                Directory.Delete(packDirectory, recursive: true);

            bool success = TryReload(out _);
            if (!success)
                return false;

            if (Data.InvalidPackIds.TryGetValue(packId, out packException))
            {
                RestorePack(packId, time);

                if (!TryReload(out _))
                    return false;
            }

            return true;
        }

        public static void UpdateIcons(PackId packId, ZipArchive archive)
        {
            string iconsDirectory = Path.Combine(PacksDirectory, packId.Value, PackIconsDirectoryName);
            if (Directory.Exists(iconsDirectory))
                Directory.Delete(iconsDirectory, recursive: true);

            Directory.CreateDirectory(iconsDirectory);

            foreach (ZipArchiveEntry entry in archive.Entries)
                if (entry.Name.EndsWith(".png"))
                    entry.ExtractToFile(Path.Combine(iconsDirectory, entry.Name));
        }
    }


    public sealed record Data(
        IReadOnlyDictionary<ulong, CreatorData> CreatorIds,
        IReadOnlyDictionary<PackId, PackData> PackIds,
        IReadOnlyDictionary<OriginId, OriginData> OriginIds,
        IReadOnlyDictionary<ulong, Exception> InvalidCreatorIds,
        IReadOnlyDictionary<PackId, Exception> InvalidPackIds,
        IReadOnlyList<ulong> OrderedCreatorIds,
        IReadOnlyList<PackId> OrderedPackIds,
        IReadOnlyList<OriginId> OrderedOriginIds,
        IReadOnlyDictionary<ulong, string> UniqueCreatorNames,
        IReadOnlyDictionary<PackId, string> UniquePackNames,
        IReadOnlyDictionary<OriginId, string> UniqueOriginNames
    )
    {
        public bool TryParseCreatorId(string creatorString, out ulong creatorId)
        {
            if (string.IsNullOrEmpty(creatorString))
            {
                creatorId = 0ul;
                return false;
            }

            if (!ulong.TryParse(creatorString, out creatorId))
                return CreatorIds.Values.Any(creator =>
                creator.Name.Equals(creatorString, StringComparison.InvariantCultureIgnoreCase));

            return CreatorIds.ContainsKey(creatorId) || InvalidCreatorIds.ContainsKey(creatorId);
        }

        public bool TryParsePackId(string packString, out PackId packId)
        {
            if (string.IsNullOrEmpty(packString))
            {
                packId = new(string.Empty);
                return false;
            }

            packId = new(packString);

            if (PackIds.ContainsKey(packId) || InvalidPackIds.ContainsKey(packId))
                return true;

            PackData? pack = PackIds.Values.FirstOrDefault(pack => pack.Name.Equals(packString, StringComparison.InvariantCultureIgnoreCase));
            if (pack == null)
                return false;

            packId = pack.Id;
            return true;
        }

        public bool TryParseOriginId(string originString, out OriginId originId)
        {
            if (string.IsNullOrEmpty(originString))
            {
                originId = new(new(string.Empty), string.Empty);
                return false;
            }

            OriginId? maybeOriginId = OriginId.FromFullId(originString);
            if (!maybeOriginId.HasValue)
            {
                OriginData? origin = OriginIds.Values.FirstOrDefault(origin => origin.Name.Equals(originString, StringComparison.InvariantCultureIgnoreCase));
                if (origin == null)
                {
                    originId = new(new(string.Empty), string.Empty);
                    return false;
                }

                originId = origin.Id;
                return true;
            }

            originId = maybeOriginId.Value;
            return OriginIds.ContainsKey(originId);
        }
    }
    
    
    public sealed record CreatorData(
        ulong Id,
        string Name,
        string Description,
        IReadOnlyList<(string, string)> Info,
        Color Color,
        IReadOnlySet<PackId> PackIds,
        IReadOnlyList<PackData> OrderedPacks
    )
    {
        public static CreatorData FromCreatorId(ulong creatorId, IEnumerable<PackData> packs)
        {
            using FileStream creatorJsonStream = File.OpenRead(Path.Combine(OriginDataService.CreatorsDirectory, creatorId.ToString(), OriginDataService.CreatorJsonFileName));
            JsonCreator? jsonCreator = JsonSerializer.Deserialize<JsonCreator>(creatorJsonStream, OriginDataService.JsonOptions)
                ?? throw new Exception("JsonCreator: Failed to deserialize.");

            if (string.IsNullOrEmpty(jsonCreator.Name))
                throw new Exception("JsonCreator: Empty name.");

            if (string.IsNullOrEmpty(jsonCreator.Name))
                throw new Exception("JsonCreator: Empty description.");

            for (int i = 0; i < jsonCreator.Info.Length; i++)
            {
                if (string.IsNullOrEmpty(jsonCreator.Info[i].Name))
                    throw new Exception($"JsonCreator: Empty info name #{i + 1}.");
                if (string.IsNullOrEmpty(jsonCreator.Info[i].Value))
                    throw new Exception($"JsonCreator: Empty info value #{i + 1}.");
            }

            if (jsonCreator.Color.Length != 3)
                throw new Exception("JsonCreator: Invalid color.");


            return new(
                creatorId,
                jsonCreator.Name,
                jsonCreator.Description,
                [.. jsonCreator.Info.Select(info => (info.Name, info.Value))],
                new Color((byte)jsonCreator.Color[0], (byte)jsonCreator.Color[1], (byte)jsonCreator.Color[2]),
                new HashSet<PackId>(packs.Select(pack => pack.Id)),
                [.. packs]
            );
        }
    }
    

    public sealed record PackData(
        PackId Id,
        string Name,
        string Description,
        IReadOnlySet<ulong> CreatorIds,
        IReadOnlyList<ulong> OrderedCreatorIds,
        string Version,
        IReadOnlyList<string> Requirements,
        IReadOnlyList<(string, string)> Info,
        Color Color,
        IReadOnlySet<OriginId> OriginIds,
        IReadOnlyList<OriginData> OrderedOrigins
    )
    {
        public static PackData FromPackId(PackId packId, PackLang mainLang)
        {
            if (!PackId.IsValid(packId.Value))
                throw new Exception("PackId: Invalid, must be at most 32 characters and only contain lowercase letters, digits, and underscores.");

            PackLang lang = PackLang.FromPackId(packId);

            using FileStream packJsonStream = File.OpenRead(Path.Combine(OriginDataService.PacksDirectory, packId.Value, OriginDataService.PackJsonFileName));
            JsonPack? jsonPack = JsonSerializer.Deserialize<JsonPack>(packJsonStream, OriginDataService.JsonOptions)
                ?? throw new Exception("JsonPack: Failed to deserialize.");

            if (string.IsNullOrEmpty(jsonPack.Name))
                throw new Exception("JsonPack: Empty name.");

            if (string.IsNullOrEmpty(jsonPack.Name))
                throw new Exception("JsonPack: Empty description.");

            if (jsonPack.CreatorIds.Length == 0)
                throw new Exception("JsonPack: No associated creator ids.");

            if (string.IsNullOrEmpty(jsonPack.Version))
                throw new Exception("JsonPack: Empty version.");

            for (int i = 0; i < jsonPack.Requirements.Length; i++)
                if (string.IsNullOrEmpty(jsonPack.Requirements[i]))
                    throw new Exception($"JsonPack: Empty requirement #{i + 1}.");

            for (int i = 0; i < jsonPack.Info.Length; i++)
            {
                if (string.IsNullOrEmpty(jsonPack.Info[i].Name))
                    throw new Exception($"JsonPack: Empty info name #{i + 1}.");
                if (string.IsNullOrEmpty(jsonPack.Info[i].Value))
                    throw new Exception($"JsonPack: Empty info value #{i + 1}.");
            }
            
            if (jsonPack.Color.Length != 3)
                throw new Exception("JsonPack: Invalid color.");
            

            List<OriginData> origins = [];
            foreach (JsonOrigin jsonOrigin in jsonPack.Origins)
                origins.Add(OriginData.FromJson(jsonOrigin, packId, lang, mainLang));

            return new(
                packId,
                jsonPack.Name,
                jsonPack.Description,
                new HashSet<ulong>(jsonPack.CreatorIds),
                jsonPack.CreatorIds,
                jsonPack.Version,
                jsonPack.Requirements,
                [.. jsonPack.Info.Select(info => (info.Name, info.Value))],
                new Color((byte)jsonPack.Color[0], (byte)jsonPack.Color[1], (byte)jsonPack.Color[2]),
                new HashSet<OriginId>(origins.Select(origin => origin.Id)),
                origins
            );
        }
    }


    public sealed record OriginData(
        OriginId Id,
        string Name,
        string Description,
        byte Impact,
        IReadOnlyList<Power> Powers,
        string? Credit,
        Color Color
    )
    {
        public static OriginData FromJson(JsonOrigin jsonOrigin, PackId packId, PackLang lang, PackLang mainLang)
        {
            if (string.IsNullOrEmpty(jsonOrigin.Id))
                throw new Exception("JsonOrigin: Empty id.");

            if (!OriginId.IsValid(jsonOrigin.Id))
                throw new Exception($"OriginId {jsonOrigin.Id}: Invalid, must be at most 32 characters and only contain lowercase letters, digits, and underscores.");
            
            OriginId originId = new(packId, jsonOrigin.Id);

            if (jsonOrigin.Impact > 4)
                throw new Exception($"JsonOrigin {originId.Value}: Invalid impact.");

            for (int i = 0; i < jsonOrigin.PowerIds.Length; i++)
                if (string.IsNullOrEmpty(jsonOrigin.PowerIds[i]))
                    throw new Exception($"JsonOrigin {originId.Value}: Empty power id #{i + 1}.");

            if (jsonOrigin.Color.Length != 3)
                throw new Exception($"JsonOrigin {originId.Value}: Invalid color.");


            return new(
                originId,
                lang.Get($"origins:origin.{originId.GetFullId('.')}.name", mainLang),
                lang.Get($"origins:origin.{originId.GetFullId('.')}.description", mainLang),

                jsonOrigin.Impact,
                [.. jsonOrigin.PowerIds
                .Where(powerId => !string.IsNullOrEmpty(powerId))
                .Select(powerId => new Power(
                    powerId,
                    lang.Get($"origins:power.{powerId}.name", mainLang),
                    lang.Get($"origins:power.{powerId}.description", mainLang)))],

                lang.GetNull($"origins:credit.{originId.GetFullId('.')}", mainLang),
                new Color(
                    (byte)jsonOrigin.Color[0],
                    (byte)jsonOrigin.Color[1],
                    (byte)jsonOrigin.Color[2]
                )
            );
        }
    }


    public readonly record struct PackId(string Value)
    {
        public static bool IsValid(string value)
            => OriginId.IsValid(value);
    }

    public readonly record struct PackLang(Dictionary<string, string> Lang)
    {
        public string Get(string key, PackLang? mainLang = default)
        {
            if (mainLang.HasValue && mainLang.Value.Lang.TryGetValue(key, out string? mainValue))
                return mainValue;

            if (Lang.TryGetValue(key, out string? value))
                return value;

            return key;
        }
        
        public string? GetNull(string key, PackLang? mainLang = default)
        {
            if (mainLang.HasValue && mainLang.Value.Lang.TryGetValue(key, out string? mainValue))
                return mainValue;

            if (Lang.TryGetValue(key, out string? value))
                return value;

            return null;
        }

        public static PackLang FromPackId(PackId packId)
        {
            Dictionary<string, string> lang = [];

            foreach (string line in File.ReadAllLines(Path.Combine(OriginDataService.PacksDirectory, packId.Value, OriginDataService.PackLangFileName)))
            {
                if (line.StartsWith('#'))
                    continue;

                int sep = line.IndexOf('=');
                if (sep == -1)
                    continue;

                string key = line[..sep];
                string val = line[(sep + 1)..];

                if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(val))
                    continue;

                lang[key] = val
                    .Replace("\\\\", "\\")
                    .Replace("\\n", "\n");
            }

            return new(lang);
        }
    }


    public readonly record struct OriginId(PackId PackId, string Value)
    {
        public readonly string GetFullId(char separator = ':')
            => $"{PackId.Value}{separator}{Value}";

        public static OriginId? FromFullId(string fullId, char separator = ':')
        {
            int sep = fullId.IndexOf(separator);
            if (sep == -1)
                return null;

            string packIdString = fullId[..sep];
            string originIdString = fullId[(sep + 1)..];
            
            if (string.IsNullOrEmpty(packIdString) || string.IsNullOrEmpty(originIdString))
                return null;

            return new(new(packIdString), originIdString);
        }

        public static bool IsValid(string value)
            => value.Length <= 32 && value.All(c => char.IsAsciiLetterLower(c) || char.IsAsciiDigit(c) || c == '_');
    }

    public readonly record struct Power(string Id, string Name, string Description);
}