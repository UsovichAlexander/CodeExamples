#define M2TRACE
using deVoid.Utils;
using Doozy.Engine.UI;

#if UNITY_ANDROID
    using Google.Play.Review;
#endif
using System.Collections;
using UnityEngine;
using vandrouka.m2.app;
using vandrouka.m2.app.conf;
using vandrouka.m2.mg;
using vandrouka.m2.util;

namespace vandrouka.m2.ui
{
    public class RateUsPopup : BasePopup<RateUsPopup>
    {
        [SerializeField] private UIButton likeButton;
        [SerializeField] private UIButton dislikeButton;
        private int count;
        private const string LIKE = "like";
        private const string DISLIKE = "dislike";
        private const string LATER = "later";

        protected override void OnEnable()
        {
            base.OnEnable();
            likeButton.OnClick.OnTrigger.Event.AddListener(OnClickLikeButtonHandler);
            dislikeButton.OnClick.OnTrigger.Event.AddListener(OnClickDislikeButtonHandler);

            count = PlayerPrefs.GetInt(Prefs.GPS_RATE_US_COUNTER);
            count++;
            PlayerPrefs.SetInt(Prefs.GPS_RATE_US_COUNTER, count);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            likeButton.OnClick.OnTrigger.Event.RemoveListener(OnClickLikeButtonHandler);
            dislikeButton.OnClick.OnTrigger.Event.RemoveListener(OnClickDislikeButtonHandler);
            Signals.Get<signals.meta.MetaActionFinish>().Dispatch(MetaActionType.SHOW_RATE_US_POPUP);
        }

        private void OnClickLikeButtonHandler()
        {
            if (AppInfo.soundSystem != null)
            {
                AppInfo.soundSystem.playSfx(AppInfo.soundSystem.config.common_Close_Sound);
            }
            likeButton.Interactable = false;

            GamePlayStatistics.DialogRateUs(count, LIKE);
            PlayerPrefs.SetInt(Prefs.RATE_US_WAS_SHOWN, 1);
#if UNITY_ANDROID
            StartCoroutine(RequestAndroidRateUsDialog());
#elif UNITY_IOS
            RequestIOSRateUsDialog();
#else
            PlayHideAnimation();
#endif
        }

#if UNITY_ANDROID
        private ReviewManager _reviewManager;
        private IEnumerator RequestAndroidRateUsDialog()
        {
            _reviewManager = new ReviewManager();
            var requestFlowOperation = _reviewManager.RequestReviewFlow();
            yield return requestFlowOperation;
            if (requestFlowOperation.Error != ReviewErrorCode.NoError)
            {
                Util.DebugLogError($"RequestReviewFlow error: {requestFlowOperation.Error}");
                yield break;
            }
            var playReviewInfo = requestFlowOperation.GetResult();

            var launchFlowOperation = _reviewManager.LaunchReviewFlow(playReviewInfo);
            yield return launchFlowOperation;
            playReviewInfo = null; // Reset the object
            if (launchFlowOperation.Error != ReviewErrorCode.NoError)
            {
                Util.DebugLogError($"LaunchReviewFlow error: {requestFlowOperation.Error}");
                yield break;
            }
            // The flow has finished. The API does not indicate whether the user
            // reviewed or not, or even whether the review dialog was shown. Thus, no
            // matter the result, we continue our app flow.

            PlayHideAnimation();
        }
#endif

        private void RequestIOSRateUsDialog()
        {
#if UNITY_IOS
            UnityEngine.iOS.Device.RequestStoreReview();
#endif
            PlayHideAnimation();
        }

        private void OnClickDislikeButtonHandler()
        {
            if (AppInfo.soundSystem != null)
            {
                AppInfo.soundSystem.playSfx(AppInfo.soundSystem.config.common_Close_Sound);
            }
            dislikeButton.Interactable = false;

            GamePlayStatistics.DialogRateUs(count, DISLIKE);
            PlayHideAnimation();
        }

        public override void OnClickClose()
        {
            GamePlayStatistics.DialogRateUs(count, LATER);
            base.OnClickClose();
        }
    }
    
}
