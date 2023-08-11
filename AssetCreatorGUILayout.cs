#define M2TRACE
using UnityEditor;
using UnityEngine;

namespace vandrouka.m2.util
{
    public class AssetCreatorGUILayout : UnityEditor.EditorWindow
    {
        private static bool isGUIEnabled = true;

        [MenuItem("Merge2/UpdateAssets")]
        static void Init()
        {
            AssetCreatorGUILayout window = (AssetCreatorGUILayout)EditorWindow.GetWindow(typeof(AssetCreatorGUILayout), true, "Asset Creator Window");
            window.minSize = new Vector2(400f, 400f);
            window.maxSize = new Vector2(400f, 850);
            window.Show();
            isGUIEnabled = true;
            SetAllToggles(false);
        }

        private static void SetAllToggles(bool isEnabled)
        {
            AssetCreator.downloadConfig = isEnabled;
            AssetCreator.createChips = isEnabled;
            AssetCreator.createCycles = isEnabled;
            AssetCreator.createGenerators = isEnabled;
            AssetCreator.createResources = isEnabled;
            AssetCreator.createBubbleMerge = isEnabled;
            AssetCreator.createTotems = isEnabled;
            AssetCreator.createTasks = isEnabled;
            AssetCreator.createOrders = isEnabled;
            AssetCreator.createDialogs = isEnabled;
            AssetCreator.createBuildings = isEnabled;
            AssetCreator.createLocks = isEnabled;
            AssetCreator.setFieldChips = isEnabled;
            AssetCreator.setFieldLocks = isEnabled;
            AssetCreator.createStartConfig = isEnabled;
            AssetCreator.createShopBlocks = isEnabled;
            AssetCreator.createShopOffers = isEnabled;
            AssetCreator.createSpecialOffers = isEnabled;
            AssetCreator.createChapters = isEnabled;
            AssetCreator.updateLocalizations = isEnabled;
            AssetCreator.updateABTests = isEnabled;
            AssetCreator.createTutorialSteps = isEnabled;
            AssetCreator.createStorehouseCellOffers = isEnabled;
            AssetCreator.createSpecialEvents = isEnabled;
            AssetCreator.updateMatchCardsConfig = isEnabled;
            AssetCreator.createTimeOrders = isEnabled;
            AssetCreator.createPayPass = isEnabled;
        }

        private Vector2 scrollPos;
        void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos,GUILayout.Width(400), GUILayout.ExpandHeight(true));

            GUILayout.Space(10);

            GUI.enabled = isGUIEnabled;
            if (GUILayout.Button("Set All True"))
            {
                SetAllToggles(true);
            }
            if (GUILayout.Button("Set All False"))
            {
                SetAllToggles(false);
            }

            GUILayout.Space(10);

            EditorGUILayout.LabelField("-----Download dbConfig from Google-----", EditorStyles.boldLabel);
            AssetCreator.downloadConfig = EditorGUILayout.Toggle("Download dbConfig", AssetCreator.downloadConfig);

            EditorGUILayout.LabelField("-----SimpleChips-----", EditorStyles.boldLabel);
            AssetCreator.createChips = EditorGUILayout.Toggle("Update Chips", AssetCreator.createChips);
            AssetCreator.createCycles = EditorGUILayout.Toggle("Update Cycles", AssetCreator.createCycles);
            AssetCreator.createGenerators = EditorGUILayout.Toggle("Update Generators", AssetCreator.createGenerators);
            AssetCreator.createResources = EditorGUILayout.Toggle("Update Resources", AssetCreator.createResources);

            EditorGUILayout.LabelField("-----Boosters-----", EditorStyles.boldLabel);
            AssetCreator.createBubbleMerge = EditorGUILayout.Toggle("Update BubbleMerge", AssetCreator.createBubbleMerge);
            AssetCreator.createTotems = EditorGUILayout.Toggle("Update Totems", AssetCreator.createTotems);

            EditorGUILayout.LabelField("-----Tasks&Map-----", EditorStyles.boldLabel);
            AssetCreator.createTasks = EditorGUILayout.Toggle("Update Tasks", AssetCreator.createTasks);
            AssetCreator.createOrders = EditorGUILayout.Toggle("Update Orders", AssetCreator.createOrders);
            AssetCreator.createTimeOrders = EditorGUILayout.Toggle("Update TimeOrders", AssetCreator.createTimeOrders);
            AssetCreator.createDialogs = EditorGUILayout.Toggle("Update Dialogs", AssetCreator.createDialogs);
            AssetCreator.createBuildings = EditorGUILayout.Toggle("Update All Buildings", AssetCreator.createBuildings);
            AssetCreator.createChapters = EditorGUILayout.Toggle("Update Chapters", AssetCreator.createChapters);

            EditorGUILayout.LabelField("-----StartField-----", EditorStyles.boldLabel);
            AssetCreator.setFieldChips = EditorGUILayout.Toggle("Update FieldChips", AssetCreator.setFieldChips);
            AssetCreator.setFieldLocks = EditorGUILayout.Toggle("Update FieldLocks", AssetCreator.setFieldLocks);
            AssetCreator.createStartConfig = EditorGUILayout.Toggle("Update StartConfig", AssetCreator.createStartConfig);
            AssetCreator.createLocks = EditorGUILayout.Toggle("Update Locks", AssetCreator.createLocks);

            EditorGUILayout.LabelField("-----MiniGames-----", EditorStyles.boldLabel);
            AssetCreator.updateMatchCardsConfig = EditorGUILayout.Toggle("Update MatchCardsConfig", AssetCreator.updateMatchCardsConfig);

            EditorGUILayout.LabelField("-----Shop-----", EditorStyles.boldLabel);
            AssetCreator.createShopBlocks = EditorGUILayout.Toggle("Update ShopBlocks", AssetCreator.createShopBlocks);
            AssetCreator.createShopOffers = EditorGUILayout.Toggle("Update ShopOffers", AssetCreator.createShopOffers);
            AssetCreator.createSpecialOffers = EditorGUILayout.Toggle("Update SpecialOffers", AssetCreator.createSpecialOffers);
            AssetCreator.createStorehouseCellOffers = EditorGUILayout.Toggle("Update StorehouseConfig", AssetCreator.createStorehouseCellOffers);
            AssetCreator.updateABTests = EditorGUILayout.Toggle("Update ABTests", AssetCreator.updateABTests);

            EditorGUILayout.LabelField("-----Tutorial&SpecialEvents-----", EditorStyles.boldLabel);
            AssetCreator.createTutorialSteps = EditorGUILayout.Toggle("Update TutorialSteps", AssetCreator.createTutorialSteps);
            AssetCreator.createSpecialEvents = EditorGUILayout.Toggle("Update SpecialEvents", AssetCreator.createSpecialEvents);
            AssetCreator.createPayPass = EditorGUILayout.Toggle("Update PayPass", AssetCreator.createPayPass);

            EditorGUILayout.LabelField("-----Localizations-----", EditorStyles.boldLabel);
            AssetCreator.updateLocalizations = EditorGUILayout.Toggle("Update Localizations", AssetCreator.updateLocalizations);

            GUILayout.Space(10);

            if (GUILayout.Button("Start Updating Assets"))
            {
                isGUIEnabled = false;
                Util.DebugLog("Start Updating Assets");
                AssetCreator.DownloadDBConfigAndUpdateAssets();
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }
    }
}
