//#define M2TRACE
using deVoid.Utils;
using Doozy.Engine.UI;
using System;
using UnityEngine;
using UnityEngine.UI;
using vandrouka.m2.ads;
using vandrouka.m2.app;
using vandrouka.m2.app.conf;
using vandrouka.m2.db.orm;
using vandrouka.m2.signals.gamestate;
using vandrouka.m2.tutorial;
using vandrouka.m2.util;
using Task = vandrouka.m2.db.propSO.Task;

namespace vandrouka.m2.ui
{
    public class DoubleCoinsPopup : BasePopup<DoubleCoinsPopup>
    {
        [SerializeField] private float onShowClickDelay = 0.3f;
        [SerializeField] private UIButton adsButton;
        [SerializeField] private Image coinsImage;
        private Task _order;
        private string _gpsAction;
        private const string GPS_COMPLETE_STRING = "complete";
        private const string GPS_CLOSE_STRING = "close";

        protected override void OnEnable()
        {
            base.OnEnable();
            adsButton.OnClick.OnTrigger.Event.AddListener(AdsButtonClickHandler);
        }
        
        protected override void OnDisable()
        {
            base.OnDisable();
            adsButton.OnClick.OnTrigger.Event.RemoveListener(AdsButtonClickHandler);
            GamePlayStatistics.AdsOrderDoubleCoins(_order, _gpsAction);
        }

        private async void AdsButtonClickHandler()
        {
            if (_order != null)
            {
                AppInfo.soundSystem.playSfx(AppInfo.soundSystem.config.common_Click_Sound);
                adsButton.Interactable = false;

                var adsPlacement = AAdsWorker.ADS_PLACEMENT.ads_double_order_coins;
                var wasShown = await App.I.adsManager.showADAsync(adsPlacement);
                if (wasShown)
                {
                    PlayResourceFlightAnimation(new PlayerProfileDiff {coins = _order.rewardCoins});
                }

                _gpsAction = GPS_COMPLETE_STRING;
                GamePlayStatistics.BalanceGet(GamePlayStatistics.BalanceType.coins, _order.rewardCoins, "ads_double_reward");
                Util.DebugLog($"Got double coins reward ({_order.rewardCoins} coins) from {_order.name}");
            }

            Hide();
        }

        public void Init(Task order)
        {
            _order = order;
            _gpsAction = GPS_CLOSE_STRING;
        }

        public override void OnShowAnimationFinished()
        {
            SetButtonsInteractable();
        }

        private async void SetButtonsInteractable()
        {
            await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(onShowClickDelay));

            var popupCloseBlocker = gameObject.GetComponent<TutorialPopupCloseBlocker>();
            if (popupCloseBlocker != null && popupCloseBlocker.blockIsActive)
            {
                closeOnOverlayBlockedByTutorial = true;
            }

            if (!closeOnOverlayBlockedByTutorial)
            {
                popUp.HideOnClickOverlay = true;
            }
        }

        private void PlayResourceFlightAnimation(PlayerProfileDiff ppDiff)
        {
            var fcvVisInfo = new VisualisationInfo
            {
                worldPos = coinsImage.transform.position,
                type = VisInfoType.popupAnimation
            };
            Signals.Get<signals.gamestate.RequestPlayerProfileChangeSignal>().Dispatch(ppDiff, false);
            Signals.Get<signals.gamestate.RequestPlayerProfileChangeVisualisationSignal>().Dispatch(ppDiff, fcvVisInfo);
        }

    }
}
