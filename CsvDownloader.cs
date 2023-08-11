#define M2TRACE
using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using CsvHelper;
using ExcelDataReader;
using UnityEditor;
using UnityEngine.Networking;
using vandrouka.m2.app.conf;

namespace vandrouka.m2.util
{
    public class CsvDownloader
    {
        public const string ENV_VAR_CONFIG_URL = "ConfigURL";
        public const string RECOVERY_DB_CONFIG_URL = "https://docs.google.com/spreadsheets/d/1JfLwegtUy2ORm2b88jyq-LivQDi-liHHVOQYvnZ-eK4/export?format=xlsx";
        public const string DB_CONFIG_EXCEL_PATH = "Assets/vandrouka/m2Config/csv/dbConfig.xlsx";
        public static bool isError;

        private const string APP_CONF_PATH = "Assets/vandrouka/m2Config/AppConf.asset";
        private const string PATH = "Assets/vandrouka/m2Config/csv/";
        private const string VERSION_TABLE_NAME = "Version";
        private const string EMPTY_COLUMN_PREFIX = "Empty";

        public static async Task DownloadDBConfig()
        {
            string dbConfigURL = GetDownloadLink();

            Util.DebugLog($"Creating WebRequest...");
            UnityWebRequest webRequest = new UnityWebRequest(dbConfigURL);
            webRequest.downloadHandler = new DownloadHandlerFile(DB_CONFIG_EXCEL_PATH);
            var operation = webRequest.SendWebRequest();

            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                Util.DebugLog($"Request Success");
            }
            else
            {
                Util.DebugLogError($"Request Failed: {webRequest.error}");
                isError = true;
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static string GetDownloadLink()
        {
            string configURL;
            AppConf appConf = AssetDatabase.LoadAssetAtPath<AppConf>(APP_CONF_PATH);

#if UNITY_CLOUD_BUILD
            configURL = Environment.GetEnvironmentVariable(ENV_VAR_CONFIG_URL); 
            if (string.IsNullOrEmpty(configURL))
            {
                appConf.dbConfigLinkSource = "Bad url. Got from direct link";
                return RECOVERY_DB_CONFIG_URL;
            }

            appConf.dbConfigLinkSource = "Success. Got from environment variable";
            return configURL;
#else
            configURL = RECOVERY_DB_CONFIG_URL;
            appConf.dbConfigLinkSource = "Got from direct link";
#endif

            EditorUtility.SetDirty(appConf);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return configURL;
        }

        public static void ConvertDBConfigToCsv(string excelFilePath)
        {
            Util.DebugLog("Converting DBConfig to csv");
            using (var stream = new FileStream(excelFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                IExcelDataReader reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                if (excelFilePath.EndsWith(".xls"))
                {
                    reader = ExcelReaderFactory.CreateBinaryReader(stream);
                }
                else if (excelFilePath.EndsWith(".xlsx"))
                {
                    reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                }

                if (reader == null)
                {
                    Util.DebugLogError("ExcelDataReader is null");
                    return;
                }

                DataSet dataSet = reader.AsDataSet(new ExcelDataSetConfiguration()
                {
                    ConfigureDataTable = (tableReader) =>
                    {
                        return new ExcelDataTableConfiguration()
                        {
                            EmptyColumnNamePrefix = EMPTY_COLUMN_PREFIX,
                            UseHeaderRow = true,
                        };
                    },
                });

                foreach (DataTable table in dataSet.Tables)
                {
                    var csvContent = GetDataFromTable(table);

                    StreamWriter csv = new StreamWriter(Path.Combine(PATH, table.TableName + ".csv"), false);
                    csv.Write(csvContent);
                    csv.Close();
                }
            }
            Util.DebugLog("Finished conversion");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static bool isActualVersion()
        {
            string currentVersion = GetCurrentVersion();
            string newVersion = GetNewVersion();
            Util.DebugLog($"Current dbConfig version is: {currentVersion}. New version is: {newVersion}");
            if (currentVersion != newVersion)
            {
                SaveNewVersion(newVersion);
                return false;
            }

            return true;
        }

        private static string GetDataFromTable(DataTable table)
        {
            using var writer = new StringWriter();
            using var csvWriter = new CsvWriter(writer, CultureInfo.InvariantCulture);

            foreach (DataColumn dataColumn in table.Columns)
            {
                if (!dataColumn.ColumnName.Contains(EMPTY_COLUMN_PREFIX))
                {
                    csvWriter.WriteField(dataColumn.ColumnName);
                }
            }
            csvWriter.NextRecord();

            foreach (DataRow dataRow in table.Rows)
            {
                foreach (DataColumn dataColumn in table.Columns)
                {
                    if (!dataColumn.ColumnName.Contains(EMPTY_COLUMN_PREFIX))
                    {
                        csvWriter.WriteField(dataRow[dataColumn]);
                    }
                }

                csvWriter.NextRecord();
            }

            return writer.ToString().Trim();
        }

        private static string GetCurrentVersion()
        {
            string currentVersion = AppInfo.I.GetDBConfigInfo().dbConfigVersion;
            if (string.IsNullOrEmpty(currentVersion))
            {
                Util.DebugLog("Current version is empty");
            }
            return currentVersion;
        }

        private static string GetNewVersion()
        {
            string excelFilePath = DB_CONFIG_EXCEL_PATH;
            if (!File.Exists(excelFilePath))
            {
                Util.DebugLogError("Can't find dbConfig.xlsx to check it's version");
            }

            using (var stream = new FileStream(excelFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                IExcelDataReader reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                if (excelFilePath.EndsWith(".xls"))
                {
                    reader = ExcelReaderFactory.CreateBinaryReader(stream);
                }
                else if (excelFilePath.EndsWith(".xlsx"))
                {
                    reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
                }

                if (reader == null)
                {
                    Util.DebugLogError("ExcelDataReader is null");
                    return null;
                }

                DataSet dataSet = reader.AsDataSet(new ExcelDataSetConfiguration()
                {
                    ConfigureDataTable = (tableReader) => new ExcelDataTableConfiguration()
                    {
                        UseHeaderRow = true
                    }
                });

                if (!dataSet.Tables.Contains(VERSION_TABLE_NAME))
                {
                    Util.DebugLogError("Version table is not found in dbConfig");
                }
                else
                {
                    var table = dataSet.Tables[dataSet.Tables.IndexOf(VERSION_TABLE_NAME)];
                    var version = GetDataFromTable(table);
                    return version;
                }

                return null;
            }
        }

        private static void SaveNewVersion(string version)
        {
            AppConf appConf = AssetDatabase.LoadAssetAtPath<AppConf>(APP_CONF_PATH);
            appConf.dbConfigVersion = version;

            EditorUtility.SetDirty(appConf);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

    }
}