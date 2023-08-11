//#define M2TRACE
using System;
using System.Collections.Generic;
using System.Linq;
using deVoid.Utils;
using Doozy.Engine.Progress;
using Doozy.Engine.UI;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;
using vandrouka.m2.app;
using vandrouka.m2.app.conf;
using vandrouka.m2.app.SpriteManager;
using vandrouka.m2.db.orm;
using vandrouka.m2.mg;
using vandrouka.m2.signals.meta;
using vandrouka.m2.util;

namespace vandrouka.m2.ui
{
    public class StarFallNightMainView : SpecialEventMainView
    {
        [SerializeField] private ConstellationWidget constellationWidget;
        [SerializeField] private TextMeshProUGUI timerLabel;
        [SerializeField] private List<Image> currentRewards;
        [SerializeField] private GameObject nextLevelRewardsGroupGO;
        [SerializeField] private List<Image> nextLevelRewards;
        [SerializeField] private Progressor currentProgress;
        [SerializeField] private Progressor doneProgress;
        [SerializeField] private UIButton onwardButton;
        [SerializeField] private UIButton tapToContinueButton;
        [SerializeField] private PlayableDirector effectsDirector;
        [SerializeField] private TimelineAsset constellationDone;
        [SerializeField] private TimelineAsset constellationNext;

        private ConstellationView _currentConstellation;
        private ConstellationView _nextConstellation;
        private const string constellationStartTrackName = "ConstellationStartTrack";
        private const string constellationFinishTrackName = "ConstellationFinishTrack";
        private const string constellationStartGOTrackName = "ConstellationStartGO";
        private const string constellationFinishGOTrackName = "ConstellationFinishGO";
        private const string nextLevelRewardsTrackName = "NextLevelRewardsTrack";
        private const string nextLevelRewardsGOTrackName = "NextLevelRewardsGO";

        private bool _shownAsRewardPopup;

        private void OnEnable()
        {
            Signals.Get<signals.time.SpecialEventTimerTickSignal>().AddListener(SpecialEventTimerTickSignalHandler);
            onwardButton.OnClick.OnTrigger.Event.AddListener(OnwardButtonClickHandler);
            tapToContinueButton.OnClick.OnTrigger.Event.AddListener(TapToContinueClickHandler);
        }

        void Start()
        {
            if (_shownAsRewardPopup)
            {
                AppInfo.soundSystem.SetAudioMixerSnapshot(AppInfo.soundSystem.config.chapterScreen, 0.5f);
                AppInfo.soundSystem.playSfx(AppInfo.soundSystem.config.SE_BeachChillOutFinish_Jingle);
            }
        }

        private void OnDisable()
        {
            Signals.Get<signals.time.SpecialEventTimerTickSignal>().RemoveListener(SpecialEventTimerTickSignalHandler);
            onwardButton.OnClick.OnTrigger.Event.RemoveListener(OnwardButtonClickHandler);
            tapToContinueButton.OnClick.OnTrigger.Event.RemoveListener(TapToContinueClickHandler);

            if (_shownAsRewardPopup)
            {
                AppInfo.soundSystem.SetAudioMixerSnapshot(AppInfo.soundSystem.config.general, 0.5f);
                Signals.Get<signals.gamestate.PocketWasChangedSignal>().Dispatch();
            }
        }

        private void SpecialEventTimerTickSignalHandler(M2Timer timer)
        {
            if (timer != null)
                timerLabel.text = Util.fmtTimeInSecondsWithHours(timer.getRemainingSecs());
        }

        private void OnwardButtonClickHandler()
        {
            if (GameModeSelector.IsShowMeta)
            {
                TaskBarScrollController.shouldJumpToSeWidget = true;
            }
            _specialEventMainPopup.OnClickClose();
            Signals.Get<signals.SwitchToMergeSignal>().Dispatch();
        }

        private void TapToContinueClickHandler()
        {
            tapToContinueButton.Interactable = false;
            SetNextLevelValues();
            PlayNextConstellationAnimation();
        }

        public override void Init(db.orm.SpecialEvent specialEventORM, SpecialEventMainPopup popUp, bool showAsRewardPopup)
        {
            base.Init(specialEventORM, popUp, showAsRewardPopup);

            var currentLevel = _specialEventORM.level;
            var currentExp = _specialEventORM.experience;
            var nextLevelElement = _specialEvent.eventDictionary[currentLevel + 1];

            currentProgress.SetValue(currentLevel + 1);
            doneProgress.SetValue(currentLevel);

            Action onConstellationCompleteAction = null;
            if (showAsRewardPopup)
            {
                Util.DebugLog("Show StarFallMainPopup as reward popup");
                onwardButton.gameObject.SetActive(false);
                closeButton.gameObject.SetActive(false);
                onConstellationCompleteAction = PlayConstellationCompleteAnimation;
                _shownAsRewardPopup = true;
            }
            else
            {
                onwardButton.Interactable = false;
            }
            constellationWidget.SetCurrentConstellation(currentLevel, currentExp, onConstellationCompleteAction);
            _currentConstellation = constellationWidget.currentConstellation;

            foreach (var reward in currentRewards)
            {
                reward.gameObject.SetActive(false);
            }

            for (var i = 0; i < nextLevelElement.Rewards.Count; i++)
            {
                currentRewards[i].sprite = SpriteManager.getTileSprite(nextLevelElement.Rewards[i]);
                currentRewards[i].gameObject.SetActive(true);
            }
        }

        private void PlayConstellationCompleteAnimation()
        {
            effectsDirector.playableAsset = constellationDone;
            TimelineAsset asset = effectsDirector.playableAsset as TimelineAsset;
            var tracksList = asset.GetOutputTracks();
            foreach (var track in tracksList)
            {
                if (track.name == constellationFinishTrackName)
                {
                    var constellationAnimator = _currentConstellation.GetComponent<Animator>();
                    effectsDirector.SetGenericBinding(track, constellationAnimator);
                }
            }

            effectsDirector.Play();
        }

        private void SetNextLevelValues()
        {
            var seList = AppInfo.specialEventsSystem.GetCurrentSpecialEvents();
            var se = seList.FirstOrDefault(x => x.textID == _specialEventORM.textID);

            if (se == null)
            {
                _specialEventMainPopup.OnClickClose();
                return;
            }
            _specialEventORM = se;

            var currentLevel = _specialEventORM.level;

            if (!_specialEvent.eventDictionary.ContainsKey(currentLevel + 1))
            {
                _specialEventMainPopup.UnsubscribeOnHideComplete();
                Signals.Get<vandrouka.m2.signals.meta.StartNextAction>().Dispatch(true);
                return;
            }
            var nextLevelElement = _specialEvent.eventDictionary[currentLevel + 1];

            _nextConstellation = constellationWidget.GetConstellationByLevel(currentLevel);
            _nextConstellation.ShowConstellationWithStars(0, null);
            currentProgress.SetValue(currentLevel + 1);
            doneProgress.SetValue(currentLevel);

            foreach (var reward in nextLevelRewards)
            {
                reward.gameObject.SetActive(false);
            }

            for (var i = 0; i < nextLevelElement.Rewards.Count; i++)
            {
                nextLevelRewards[i].sprite = SpriteManager.getTileSprite(nextLevelElement.Rewards[i]);
                nextLevelRewards[i].gameObject.SetActive(true);
            }
        }

        private void PlayNextConstellationAnimation()
        {
            if (_nextConstellation == null) return;

            effectsDirector.playableAsset = constellationNext;
            TimelineAsset asset = effectsDirector.playableAsset as TimelineAsset;
            var tracksList = asset.GetOutputTracks();
            foreach (var track in tracksList)
            {
                switch (track.name)
                {
                    case constellationStartTrackName:
                    {
                        var constellationAnimator = _currentConstellation.GetComponent<Animator>();
                        effectsDirector.SetGenericBinding(track, constellationAnimator);
                        break;
                    }
                    case constellationFinishTrackName:
                    {
                        var constellationAnimator = _nextConstellation.GetComponent<Animator>();
                        effectsDirector.SetGenericBinding(track, constellationAnimator);
                        break;
                    }
                    case nextLevelRewardsTrackName:
                    {
                        var rewardsAnimator = nextLevelRewardsGroupGO.GetComponent<Animator>();
                        effectsDirector.SetGenericBinding(track, rewardsAnimator);
                        break;
                    }
                    case constellationStartGOTrackName:
                    {
                        var constellationGO = _currentConstellation.gameObject;
                        effectsDirector.SetGenericBinding(track, constellationGO);
                        break;
                    }
                    case constellationFinishGOTrackName:
                    {
                        var constellationGO = _nextConstellation.gameObject;
                        effectsDirector.SetGenericBinding(track, constellationGO);
                        break;
                    }
                    case nextLevelRewardsGOTrackName:
                    {
                        var rewardsGO = nextLevelRewardsGroupGO;
                        effectsDirector.SetGenericBinding(track, rewardsGO);
                        break;
                    }
                }
            }

            effectsDirector.Play();
        }

        public override void OnShowAnimationFinished()
        {
            base.OnShowAnimationFinished();
            onwardButton.Interactable = true;
        }
    }
}
