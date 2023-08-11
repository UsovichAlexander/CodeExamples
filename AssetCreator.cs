#define M2TRACE
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using I2.Loc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using vandrouka.m2.app.conf;
using vandrouka.m2.db.propSO;
using vandrouka.m2.mg;
using vandrouka.m2.shop;
using vandrouka.m2.tutorial;
using vandrouka.m2.ui;

namespace vandrouka.m2.util
{
    public class AssetCreator
    {
        #region Path Const Strings
        private const string ROOT = "Assets/vandrouka/m2Config";
        private const string APP_CONF_PATH = ROOT + "/AppConf.asset";
        private const string PATH = "Assets/vandrouka/m2Config/chips/hierarchies";
        private const string PATH_CYCLES = "Assets/vandrouka/m2Config/cycles";
        private const string CHIPS_HIERARCHY_CSV = "Assets/vandrouka/m2Config/csv/Chips_Hierarchy.csv";
        private const string CHIPS_CYCLES_CSV = "Assets/vandrouka/m2Config/csv/Chips_Cycles.csv";
        private const string CHIPS_GENERATORS_CSV = "Assets/vandrouka/m2Config/csv/Chips_Generators.csv";
        #endregion

        #region Checkmark Booleans
        public static bool downloadConfig;
        public static bool createChips;
        public static bool createCycles;
        public static bool createGenerators;
        #endregion

        public static async void DownloadDBConfigAndUpdateAssets()
        {
            if (downloadConfig)
            {
                await CsvDownloader.DownloadDBConfig();
                if (CsvDownloader.isError)
                {
                    return;
                }
                if (!CsvDownloader.isActualVersion())
                {
                    Util.DebugLog("dbConfig version is not actual. Updating csv files...");
                    CsvDownloader.ConvertDBConfigToCsv(CsvDownloader.DB_CONFIG_EXCEL_PATH);
                    UpdateAssets();
                }
                else
                {
                    Util.DebugLog("dbConfig version is actual.");
                }
            }
            else
            {
                UpdateAssets();
            }
        }

        private static void UpdateAssets()
        {
            if (createChips)
            {
                Debug.Log($"CreateMainFolders time is: {Time(CreateMainFolders)}");
                Debug.Log($"CreateChips time is: {Time(CreateChips)}");
                Debug.Log($"CreateHierarchiesChips time is: {Time(CreateHierarchiesChips)}");
            }
            if (createCycles) Debug.Log($"CreateCycles time is: {Time(CreateCycles)}");
            if (createGenerators) Debug.Log($"CreateGenerators time is: {Time(CreateGenerators)}");
            Util.DebugLog("Finished updating assets.");
        }

        private static string Time(Action action)
        {
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            action();
            stopwatch.Stop();
            TimeSpan time = stopwatch.Elapsed;

            return String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                time.Hours, time.Minutes, time.Seconds,
                time.Milliseconds / 10);
        }

        private static void CreateMainFolders()
        {
            if (!AssetDatabase.IsValidFolder($"{ROOT}/chips")) AssetDatabase.CreateFolder(ROOT, "chips");
            if (!AssetDatabase.IsValidFolder($"{PATH}")) AssetDatabase.CreateFolder($"{ROOT}/chips", "hierarchies");
            if (!AssetDatabase.IsValidFolder(PATH_CYCLES)) AssetDatabase.CreateFolder(ROOT, "cycles");
        }

        private static void CreateChips()
        {
            using var streamReader = new StreamReader(CHIPS_HIERARCHY_CSV);
            using var csvReader = new CsvReader(streamReader, CultureInfo.InvariantCulture);
            var records = csvReader.GetRecords<SimpleChipsData>().ToList();

            AssetDatabase.StartAssetEditing();
            Chip chip;
            foreach (var record in records)
            {
                var folderName = Regex.Replace(record.chipTextID, @"[\d_]", string.Empty);
                var filePath = Path.Combine(PATH, folderName, $"{record.chipTextID}.asset");
                bool exists = File.Exists(filePath);

                if (!AssetDatabase.IsValidFolder($"{PATH}/{folderName}"))
                {
                    AssetDatabase.CreateFolder(PATH, folderName);
                }

                if (exists)
                {
                    chip = AssetDatabase.LoadAssetAtPath<Chip>(filePath);
                }
                else
                {
                    chip = ScriptableObject.CreateInstance<Chip>();
                    AssetDatabase.CreateAsset(chip, filePath);
                }

                if (chip == null)
                {
                    Debug.LogError("chip is NULL");
                    continue;
                }
                
                chip.sellPrice = record.sellPrice;
                chip.unlockPrice = record.unlockPrice;
                chip.isBubbleable = record.IsBubbleable > 0;
                chip.isTotemable = record.IsTotemable > 0;
                chip.isMirrorable = record.IsMirrorable > 0;
                chip.unlockBubblePrice = record.UnlockBubblePrice;
                chip.SE_Text_ID = record.seTextId;
                chip.adsAbleForBubble = record.adsAbleForBubble > 0;
                chip.extraMergeable = record.extraMergeable > 0;
                chip.isInventoriable = record.IsInventoriable > 0;

                var chipFollowerTimerList = Regex.Split(record.chipFollowerTimer, @",\s*").ToList().FindAll(s => !String.IsNullOrEmpty(s));
                if (chipFollowerTimerList.Count > 1)
                {
                    chip.chipFollower = chipFollowerTimerList[0];
                    if (int.TryParse(chipFollowerTimerList[1], out var r1))
                    {
                        chip.chipFollowerTimer = r1;
                    }
                }
                else
                {
                    chip.chipFollower = string.Empty;
                    chip.chipFollowerTimer = 0;
                }

                chip.OnEnable();
                EditorUtility.SetDirty(chip);
            }

            foreach (var record in records)
            {
                var folderName = Regex.Replace(record.chipTextID, @"[\d_]", string.Empty);
                var filePath = Path.Combine(PATH, folderName, $"{record.chipTextID}.asset");

                var nextLevelFolder = Regex.Replace(record.chipTextIDAfterMerge, @"[\d_]", string.Empty);
                var nextLevelPath = Path.Combine(PATH, nextLevelFolder, $"{record.chipTextIDAfterMerge}.asset");
                chip = AssetDatabase.LoadAssetAtPath<Chip>(filePath);

                chip.nextLevelChip = AssetDatabase.LoadAssetAtPath<Chip>(nextLevelPath);
                EditorUtility.SetDirty(chip);
            }

            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreateHierarchiesChips()
        {
            using var streamReader = new StreamReader(CHIPS_HIERARCHY_CSV);
            using var csvReader = new CsvReader(streamReader, CultureInfo.InvariantCulture);
            var records = csvReader.GetRecords<SimpleChipsData>().ToList();

            AssetDatabase.StartAssetEditing();
            AppConf appConf = AssetDatabase.LoadAssetAtPath<AppConf>(APP_CONF_PATH);
            var chipHierarchies = appConf.mergeSystem.chipHierarhies;
            chipHierarchies.Clear();

            foreach (var record in records)
            {
                var hierarchyName = Regex.Replace(record.chipTextID, @"[\d_]", string.Empty);
                var folderName = hierarchyName;
                var filePath = Path.Combine(PATH, folderName, $"{hierarchyName}.asset");
                bool exists = File.Exists(filePath);

                var hierarchy = ScriptableObject.CreateInstance<Hierarchy>();
                if (!exists)
                {
                    AssetDatabase.CreateAsset(hierarchy, filePath);
                    hierarchy.chips = new List<Chip>();
                    hierarchy.generators = new List<Generator>();
                    hierarchy.resources = new List<Resource>();
                    hierarchy.bubbleMerges = new List<BubbleMerge>();
                    hierarchy.totems = new List<Totem>();
                }
                else
                {
                    hierarchy = AssetDatabase.LoadAssetAtPath<Hierarchy>(filePath);
                    hierarchy.chips.Clear();
                }

                if (!chipHierarchies.Contains(hierarchy))
                {
                    appConf.mergeSystem.chipHierarhies.Add(hierarchy);
                }

                EditorUtility.SetDirty(appConf.mergeSystem);
                EditorUtility.SetDirty(hierarchy);
            }

            AddChipsToHierarchy(records);

            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void AddChipsToHierarchy(List<SimpleChipsData> records)
        {
            foreach (var record in records)
            {
                var hierarchyName = Regex.Replace(record.chipTextID, @"[\d_]", string.Empty);
                var folderName = hierarchyName;
                var filePath = Path.Combine(PATH, folderName, $"{hierarchyName}.asset");

                var chipPath = Path.Combine(PATH, folderName, $"{record.chipTextID}.asset");
                var hierarchy = AssetDatabase.LoadAssetAtPath<Hierarchy>(filePath);
                var chip = AssetDatabase.LoadAssetAtPath<Chip>(chipPath);
                hierarchy.chips.Add(chip);

                EditorUtility.SetDirty(hierarchy);
            }
        }

        private static void CreateCycles()
        {
            using var streamReader = new StreamReader(CHIPS_CYCLES_CSV);
            using var csvReader = new CsvReader(streamReader, CultureInfo.InvariantCulture);
            var records = csvReader.GetRecords<ChipsCyclesData>().ToList();

            AssetDatabase.StartAssetEditing();
            foreach(var record in records)
            {
                var cyclePath = Path.Combine(PATH_CYCLES, $"{record.cycleTextId}.asset");
                bool exists = File.Exists(cyclePath);

                Cycle cycle;
                if (exists)
                {
                    cycle = AssetDatabase.LoadAssetAtPath<Cycle>(cyclePath);
                    cycle.chips.Clear();
                }
                else
                {
                    cycle = ScriptableObject.CreateInstance<Cycle>();
                    AssetDatabase.CreateAsset(cycle, cyclePath);
                    cycle.chips = new List<Chip>();
                }

                var cycleChipsStringList = Regex.Split(record.chipsInCycle, @",\s*").ToList().FindAll(s => !String.IsNullOrEmpty(s));
                foreach (var stringField in cycleChipsStringList)
                {
                    if (!string.IsNullOrEmpty(stringField) && stringField != "0")
                    {
                        var folderName = Regex.Replace(stringField, @"[\d_]", string.Empty);
                        string filePath = Path.Combine(PATH, folderName, $"{stringField}.asset");
                        var chip = AssetDatabase.LoadAssetAtPath<Chip>(filePath);
                        cycle.chips.Add(chip);
                    }
                }
                EditorUtility.SetDirty(cycle);
            }

            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreateGenerators()
        {
            using var streamReader = new StreamReader(CHIPS_GENERATORS_CSV);
            using var csvReader = new CsvReader(streamReader, CultureInfo.InvariantCulture);
            var records = csvReader.GetRecords<ChipsGeneratorsData>().ToList();

            AssetDatabase.StartAssetEditing();
            foreach (var record in records)
            {
                var folderName = Regex.Replace(record.generatorTextID, @"[\d_]", string.Empty);
                var filePath = Path.Combine(PATH, folderName, $"generators/{record.generatorTextID}.asset");
                bool exists = File.Exists(filePath);

                if (!AssetDatabase.IsValidFolder($"{PATH}/{folderName}/generators"))
                {
                    AssetDatabase.CreateFolder($"{PATH}/{folderName}", "generators");
                }

                var generator = ScriptableObject.CreateInstance<Generator>();
                if (exists)
                {
                    generator = AssetDatabase.LoadAssetAtPath<Generator>(filePath);
                }
                else
                {
                    AssetDatabase.CreateAsset(generator, filePath);
                }

                UpdateGeneratorValues(generator, record);
                UpdateGeneratorFollowers(generator, record);
                UpdateGeneratorCycle(generator, record);

                EditorUtility.SetDirty(generator);
            }

            AddGeneratorsToHierarchy(records);

            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

        }

        private static void UpdateGeneratorValues(Generator generator, ChipsGeneratorsData record)
        {
            generator.generatorEffect = record.generatorEffect;
            generator.useEnergy = record.useEnergy > 0;
            generator.lifetime = record.generatorLifetime;
            generator.autoCreationChip = record.autoCreationChip > 0;
            generator.autoInit = record.autoInit > 0;
            generator.initialRebootTime = record.initialReboot;
            generator.rebootTime = record.generatorRebootTime;
            generator.minCapacity = record.generatorMinCapacity;
            generator.maxCapacity = record.generatorMaxCapacity;
            generator.capacityIncreaseStepTime = record.capacityIncreaseStepTime;
            generator.capacityIncreaseStepAmount = record.capacityIncreaseStepAmount;
            generator.skipRebootForCrystals = record.skipRebootForCrystals;
            generator.reusable = record.reusable > 0;
            generator.adsChipsReward = record.adsChipsReward;
            generator.capacityScale = record.capacityScale;
            generator.capacityTaskTrigger = record.capacityTaskTrigger;
            generator.adsBoost = record.adsBoost;
        }

        private static void UpdateGeneratorFollowers(Generator generator, ChipsGeneratorsData record)
        {
            if (!string.IsNullOrEmpty(record.chipFollower) && record.chipFollower != "0")
            {
                var chipFolderName = Regex.Replace(record.chipFollower, @"[\d_]", string.Empty);
                var filePath = Path.Combine(PATH, chipFolderName, $"{record.chipFollower}.asset");
                generator.chipFollower = AssetDatabase.LoadAssetAtPath<Chip>(filePath);
            }
        }

        private static void UpdateGeneratorCycle(Generator generator, ChipsGeneratorsData record)
        {
            var cycle = AssetDatabase.LoadAssetAtPath<Cycle>($"{PATH_CYCLES}/{record.generatorChipsCycle}.asset");
            if(cycle == null)
            {
                Debug.Log($"generator cycle {record.generatorChipsCycle} not found");
            }
            generator.cycle = cycle;
        }

        private static void AddGeneratorsToHierarchy(List<ChipsGeneratorsData> records)
        {
            foreach (var record in records)
            {
                var hierarchyName = Regex.Replace(record.generatorTextID, @"[\d_]", string.Empty);
                var folderName = hierarchyName;
                var filePath = Path.Combine(PATH, folderName, $"{hierarchyName}.asset");

                var hierarchy = AssetDatabase.LoadAssetAtPath<Hierarchy>(filePath);
                hierarchy.generators.Clear();
            }

            foreach (var record in records)
            {
                var hierarchyName = Regex.Replace(record.generatorTextID, @"[\d_]", string.Empty);
                var folderName = hierarchyName;
                var filePath = Path.Combine(PATH, folderName, $"{hierarchyName}.asset");
                var generatorPath = Path.Combine(PATH, folderName, $"generators/{record.generatorTextID}.asset");

                var hierarchy = AssetDatabase.LoadAssetAtPath<Hierarchy>(filePath);
                var generator = AssetDatabase.LoadAssetAtPath<Generator>(generatorPath);
                hierarchy.generators.Add(generator);

                EditorUtility.SetDirty(hierarchy);
            }
        }

        //Etc.  
        //The end of the code example
    }


    #region DataMapping

    public class SimpleChipsData
    {
        [Name("Text_ID")]
        public string chipTextID { get; set; }

        [Name("Text_ID_ChipAfterMerge")]
        public string chipTextIDAfterMerge { get; set; }

        [Name("SellPrice")]
        public int sellPrice { get; set; }

        [Name("UnlockPrice")]
        public int unlockPrice { get; set; }

        [Name("IsBubbleable")]
        public int IsBubbleable { get; set; }

        [Name("UnlockBubblePrice")]
        public int UnlockBubblePrice { get; set; }

        [Name("IsTotemable")]
        public int IsTotemable { get; set; }

        [Name("IsMirrorable")]
        public int IsMirrorable { get; set; }

        [Name("SE_Text_ID")]
        public string seTextId { get; set; }

        [Name("AdsAbleForBubble")]
        public int adsAbleForBubble { get; set; }

        [Name("ExtraMergeable")]
        public int extraMergeable { get; set; }

        [Name("ChipFollowerTimer")]
        public string chipFollowerTimer { get; set; }

        [Name("IsInventoriable")]
        public int IsInventoriable { get; set; }
    }

    public class ChipsCyclesData
    {
        [Name("Text_ID_OrderCycle")]
        public string cycleTextId { get; set; }

        [Name("ChipsInCycle")]
        public string chipsInCycle { get; set; }
    }

    public class ChipsGeneratorsData
    {
        [Name("Text_ID")]
        public string generatorTextID { get; set; }

        [Name("GeneratorEffect")]
        public int generatorEffect { get; set; }

        [Name("UseEnergy")]
        public int useEnergy { get; set; }

        [Name("GeneratorLifetime")]
        public int generatorLifetime { get; set; }

        [Name("AutoCreationChip")]
        public int autoCreationChip { get; set; }

        [Name("AutoInit")]
        public int autoInit { get; set; }

        [Name("InitialReboot")]
        public int initialReboot { get; set; }

        [Name("Reboot")]
        public int generatorRebootTime { get; set; }

        [Name("ChipFollower")]
        public string chipFollower { get; set; }

        [Name("MinCapacity")]
        public int generatorMinCapacity { get; set; }

        [Name("Text_ID_OrderCycle")]
        public string generatorChipsCycle { get; set; }

        [Name("MaxCapacity")]
        public int generatorMaxCapacity { get; set; }

        [Name("CapacityIncreaseStepAmount")]
        public int capacityIncreaseStepAmount { get; set; }

        [Name("CapacityIncreaseStepTime")]
        public int capacityIncreaseStepTime { get; set; }

        [Name("SkipRebootForCrystals")]
        public int skipRebootForCrystals { get; set; }

        [Name("Reusable")]
        public int reusable { get; set; }

        [Name("AdsChipsReward")]
        public int adsChipsReward { get; set; }

        [Name("CapacityScale")]
        public int capacityScale { get; set; }

        [Name("CapacityTaskTrigger")]
        public string capacityTaskTrigger { get; set; }

        [Name("AdsBoost")]
        public int adsBoost { get; set; }
    }

    #endregion
}


