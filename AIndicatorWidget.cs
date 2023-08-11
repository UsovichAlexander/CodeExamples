using deVoid.Utils;
using Doozy.Engine.Progress;
using Doozy.Engine.UI;
using System.Collections.Generic;
using UnityEngine;
using vandrouka.m2.app.conf;
using vandrouka.m2.db.orm;
using vandrouka.m2.m2.Animations;
using vandrouka.m2.signals.gamestate;

namespace vandrouka.m2.ui
{
    public abstract class AIndicatorWidget : MonoBehaviour
    {
        public Progressor progressor;
        public Transform FlightAnimationTargetTransform => flightAnimationTargetTransform;
        [SerializeField] protected UIButton uiButton;
        [SerializeField] protected Transform flightAnimationTargetTransform;
        [SerializeField] protected ResourceFlightAnimation resourceParticles;
        [SerializeField] protected ResourceFlightAnimation shopResourceParticles;
        [SerializeField] protected ResourceFlightAnimation popupParticles;
        [SerializeField] protected ResourceFlightAnimation totemFlightParticles;
        [SerializeField] protected ResourceFlightAnimation buildingResourceParticles;
        [SerializeField] protected IndicatorGetResourceAnimation indicatorAnimation;
        private Camera mergeCamera => AppInfo.cameraMerge;
        private Camera uiCamera => AppInfo.cameraUI;
        private Camera metaCamera => AppInfo.cameraMeta;

        protected virtual void OnEnable()
        {
            uiButton.Button.onClick.AddListener(ButtonClickHandler);
            flightAnimationTargetTransform = indicatorAnimation.transform;
            Signals.Get<signals.gamestate.RequestPlayerProfileChangeVisualisationSignal>().AddListener(RequestPlayerProfileChangeVisualisationSignalHandler);
        }

        protected virtual void OnDisable()
        {
            uiButton.Button.onClick.RemoveListener(ButtonClickHandler);
            Signals.Get<signals.gamestate.RequestPlayerProfileChangeVisualisationSignal>().RemoveListener(RequestPlayerProfileChangeVisualisationSignalHandler);
        }

        protected virtual void ButtonClickHandler()
        {
            AppInfo.soundSystem.playSfx(AppInfo.soundSystem.config.common_Click_Sound);
            ShowDialog();
        }

        public virtual void RequestPlayerProfileChangeVisualisationSignalHandler(PlayerProfileDiff ppDiff, VisualisationInfo fcvVisInfo)
        {
            if (!shouldGetThisResource(ppDiff, fcvVisInfo)) return;

            if (fcvVisInfo.shouldBlockInputDuringVis)
            {
                Interaction.SetPlayerInputActivity(false);
            }

            indicatorAnimation.EnqueueResource(ppDiff, fcvVisInfo);

            switch (fcvVisInfo.type)
            {
                case VisInfoType.collectResource:
                    PlayParticles(fcvVisInfo);
                    break;
                case VisInfoType.shopAnimation:
                    PlayShopParticles(fcvVisInfo, ppDiff);
                    break;
                case VisInfoType.orderCompleteCoinsAnimation:
                    PlayOrderCompleteParticles(fcvVisInfo, ppDiff);
                    break;
                case VisInfoType.popupAnimation:
                    PlayPopupParticles(fcvVisInfo, ppDiff);
                    break;
                case VisInfoType.sellChip:
                    PlaySellParticles(fcvVisInfo);
                    break;
                case VisInfoType.totemFlightAnimation:
                    PlayTotemFlightParticles(fcvVisInfo);
                    break;
                case VisInfoType.seLevelComplete:
                    PlayParticles(fcvVisInfo);
                    break;
                case VisInfoType.collectBuildingResource:
                    PlayBuildingCollectResourceParticles(fcvVisInfo, ppDiff);
                    break;
            }

            PlaySfx();
        }

        protected virtual void PlayParticles(VisualisationInfo fcvVisInfo)
        {
            var values = GetVisualizationValues(fcvVisInfo);
            var particlesGO = Instantiate(resourceParticles.gameObject, values.uiWorldSrcPos, Quaternion.identity);
            var resourceAnimation = particlesGO.GetComponent<ResourceFlightAnimation>();
            resourceAnimation.PlayAnimation(values.chipLevel, indicatorAnimation, fcvVisInfo.shouldBlockInputDuringVis);
        }
        
        protected virtual void PlayShopParticles(VisualisationInfo fcvVisInfo, PlayerProfileDiff ppDiff)
        {
            var particlesGO = Instantiate(shopResourceParticles.gameObject, fcvVisInfo.worldPos, Quaternion.identity);
            var resourceAnimation = particlesGO.GetComponent<ResourceFlightAnimation>();
            int level = AppInfo.getChipConfig(fcvVisInfo.propTypeTextID).level;
            resourceAnimation.PlayAnimation(level, indicatorAnimation);
        }

        protected virtual void PlayPopupParticles(VisualisationInfo fcvVisInfo, PlayerProfileDiff ppDiff)
        {
            Vector3 newPos = uiCamera.ScreenToWorldPoint(fcvVisInfo.worldPos);
            Vector3 srcPos = new Vector3(newPos.x, newPos.y, indicatorAnimation.transform.position.z);

            var particlesGO = Instantiate(popupParticles.gameObject, srcPos, Quaternion.identity);
            var resourceAnimation = particlesGO.GetComponent<ResourceFlightAnimation>();

            var amount = 0;
            if (ppDiff.energy != 0)
            {
                amount = ppDiff.energy;
            }
            else if (ppDiff.coins != 0)
            {
                amount = ppDiff.coins;
            }
            else if (ppDiff.gems != 0)
            {
                amount = ppDiff.gems;
            }
            resourceAnimation.PlayAnimationByResAmount(amount, indicatorAnimation);
        }

        protected virtual void PlayTotemFlightParticles(VisualisationInfo fcvVisInfo)
        {
            var values = GetVisualizationValues(fcvVisInfo);
            var particlesGO = Instantiate(totemFlightParticles.gameObject, values.uiWorldSrcPos, Quaternion.identity);
            var resourceAnimation = particlesGO.GetComponent<ResourceFlightAnimation>();
            resourceAnimation.PlayAnimation(values.chipLevel, indicatorAnimation, fcvVisInfo.shouldBlockInputDuringVis);
        }

        protected virtual void PlayBuildingCollectResourceParticles(VisualisationInfo fcvVisInfo, PlayerProfileDiff ppDiff)
        {
            var dstPosition = flightAnimationTargetTransform.position;
            Vector3 newPos = metaCamera.WorldToViewportPoint(fcvVisInfo.worldPos);
            Vector3 srcPos = uiCamera.ViewportToWorldPoint(newPos);
            srcPos = new Vector3(srcPos.x, srcPos.y, dstPosition.z);

            var particlesGO = Instantiate(buildingResourceParticles.gameObject, srcPos, Quaternion.identity);
            var resourceAnimation = particlesGO.GetComponent<ResourceFlightAnimation>();
            resourceAnimation.PlayAnimationByResAmount(ppDiff.energy, indicatorAnimation);
        }

        protected virtual void PlaySellParticles(VisualisationInfo fcvVisInfo)
        {
        }

        protected virtual void PlayOrderCompleteParticles(VisualisationInfo fcvVisInfo, PlayerProfileDiff ppDiff)
        {
        }

        protected virtual void ShowDialog()
        {
        }

        protected abstract void PlaySfx();

        protected (Vector3 uiWorldSrcPos, int chipLevel) GetVisualizationValues(VisualisationInfo fcvVisInfo)
        {
            var tileWorldPos = AppInfo.topPanelMB.mergeFieldMB.getChipTileWorldPos(fcvVisInfo.tilePos);
            var dstPosition = flightAnimationTargetTransform.position;
            var srcPosition = tileWorldPos + Vector3.one / 2;
            srcPosition = new Vector3(srcPosition.x, srcPosition.y, dstPosition.z + mergeCamera.transform.position.z);

            var viewPortSrcPos = mergeCamera.WorldToViewportPoint(srcPosition);
            var uiWorldSrcPos = uiCamera.ViewportToWorldPoint(viewPortSrcPos);
            var chipLevel = AppInfo.getChipConfig(fcvVisInfo.propTypeTextID).level;

            return (uiWorldSrcPos, chipLevel);
        }

        protected virtual bool shouldGetThisResource(PlayerProfileDiff ppDiff)
        {
            return false;
        }

        protected virtual bool shouldGetThisResource(PlayerProfileDiff ppDiff, VisualisationInfo fcvVisInfo)
        {
            return shouldGetThisResource(ppDiff);
        }

        public void SetIndicatorValue(int indicatorValue, bool instantValueChange = true)
        {
            progressor.SetValue(indicatorValue, instantValueChange);
        }

        public bool TryDequeueResource(VisInfoType type)
        {
           return indicatorAnimation.TryDequeueResource(type);
        }

        public Queue<PlayerProfileDiff> GetPPDiffQueue()
        {
            return indicatorAnimation.GetPPDiffQueue();
        }

        public Queue<VisualisationInfo> GetVisInfoQueue()
        {
            return indicatorAnimation.GetVisInfoQueue();
        }
    }
}
