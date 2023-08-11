using deVoid.Utils;
using Doozy.Engine.Progress;
using System.Collections.Generic;
using Doozy.Engine.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using vandrouka.m2.app.SpriteManager;
using vandrouka.m2.db.orm;
using vandrouka.m2.util;
using SpecialEvent = vandrouka.m2.db.propSO.SpecialEvent;
using vandrouka.m2.app;

namespace vandrouka.m2.ui
{
    public class BeachChillOutMainView : SpecialEventMainView
    {
        [SerializeField] private TextMeshProUGUI timerLabel;
        [SerializeField] private TextMeshProUGUI expValueLabel;
        [SerializeField] private List<Image> currentRewards;
        [SerializeField] private Progressor expProgress;
        [SerializeField] private Progressor rewardsProgress;
        [SerializeField] private SpecialEventRewardPoint rewardPointPrefab;
        [SerializeField] private SpecialEventRewardPoint starPoint;
        [SerializeField] private Transform rewardsGroup;
        [SerializeField] private UIButton onwardButton;

        private void OnEnable()
        {
            Signals.Get<signals.time.SpecialEventTimerTickSignal>().AddListener(SpecialEventTimerTickSignalHandler);
            onwardButton.OnClick.OnTrigger.Event.AddListener(OnwardButtonClickHandler);
        }

        private void OnDisable()
        {
            Signals.Get<signals.time.SpecialEventTimerTickSignal>().RemoveListener(SpecialEventTimerTickSignalHandler);
            onwardButton.OnClick.OnTrigger.Event.RemoveListener(OnwardButtonClickHandler);
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

        public override void Init(db.orm.SpecialEvent specialEventORM, SpecialEventMainPopup popUp, bool showAsRewardPopup)
        {
            base.Init(specialEventORM, popUp, showAsRewardPopup);
            onwardButton.Interactable = false;
            SetExpProgress();
            SetRewardsProgress();
        }

        private void SetExpProgress()
        {
            int currentLevel = _specialEventORM.level;
            var eventElement = _specialEvent.eventDictionary[currentLevel];
            var nextLevelElement = _specialEvent.eventDictionary[currentLevel + 1];
            float levelExp = eventElement.ExpSE;
            float currentExp = _specialEventORM.experience;

            expValueLabel.text = $"{currentExp - levelExp}/{nextLevelElement.ExpSE - levelExp}";
            float value = (currentExp - levelExp) / (nextLevelElement.ExpSE - levelExp);
            expProgress.SetValue(value);
            expProgress.UpdateProgress();

            foreach (var reward in currentRewards)
            {
                reward.gameObject.SetActive(false);
                reward.transform.parent.gameObject.SetActive(false);
            }

            for (int i = 0; i < nextLevelElement.Rewards.Count; i++)
            {
                currentRewards[i].sprite = SpriteManager.getTileSprite(nextLevelElement.Rewards[i]);
                currentRewards[i].gameObject.SetActive(true);
                currentRewards[i].transform.parent.gameObject.SetActive(true);
            }
        }

        private void SetRewardsProgress()
        {
            int currentLevel = _specialEventORM.level;
            ClearRewardsGroup();

            for (int i = 1; i < _specialEvent.eventDictionary.Count; i++)
            {
                var rewardPoint = i == _specialEvent.eventDictionary.Count - 1 ? starPoint : Instantiate(rewardPointPrefab, rewardsGroup);
                rewardPoint.SetRewards(_specialEvent.eventDictionary[i].Rewards);

                if (i <= currentLevel)
                {
                    rewardPoint.SetCompletePoint();
                }
                else if (i == currentLevel + 1)
                {
                    rewardPoint.SetActivePoint();
                }
            }

            float step = 1f / (_specialEvent.eventDictionary.Count - 1);
            rewardsProgress.SetValue(currentLevel * step);
            rewardsProgress.UpdateProgress();
        }

        private void ClearRewardsGroup()
        {
            foreach (Transform child in rewardsGroup)
            {
                Destroy(child.gameObject);
            }
        }

        public override void OnShowAnimationFinished()
        {
            base.OnShowAnimationFinished();
            onwardButton.Interactable = true;
        }
    }
}
