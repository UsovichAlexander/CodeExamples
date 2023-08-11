using deVoid.Utils;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using vandrouka.m2.app.conf;
using vandrouka.m2.pushnotifications;
using vandrouka.m2.util;
using vandrouka.util;

namespace vandrouka.m2.ui
{
    public class SettingsPopup : BasePopup<SettingsPopup>
    {
        private const string FACEBOOK_LOGGED_LABEL = "label_LoggedFacebook";
        private const string FACEBOOK_LOGIN_LABEL = "label_LogInFacebook";
        private const string GOOGLE_LOGGED_LABEL = "label_LoggedGoogle";
        private const string GOOGLE_LOGIN_LABEL = "label_LogInGoogle";

        [SerializeField] private TwoStateButton musicSwitcher;
        [SerializeField] private TwoStateButton sfxSwitcher;
        [SerializeField] private TwoStateButton notificationsSwitcher;
        [SerializeField] private Button supportButton;
        [SerializeField] private Button notificationsButton;
        [SerializeField] private Button privacyPolicyButton;
        [SerializeField] private Button termsOfServiceButton;
        [SerializeField] private Button languageButton;
        [SerializeField] private Button facebookButton;
        [SerializeField] private Button googleButton;

        [SerializeField] private TextMeshProUGUI facebookButtonLabel;
        [SerializeField] private TextMeshProUGUI googleButtonLabel;
        [SerializeField] private TextMeshProUGUI buildVersionLabel;
        [SerializeField] private TextMeshProUGUI userIdLabel;

        [InjectField] private PushNotificationsManager _pushNotificationsManager;

        protected override void OnEnable()
        {
            base.OnEnable();
            supportButton.onClick.AddListener(SupportButtonClickHandler);
            notificationsButton.onClick.AddListener(NotificationsButtonClickHandler);
            privacyPolicyButton.onClick.AddListener(PrivacyPolicyButtonClickHandler);
            termsOfServiceButton.onClick.AddListener(TermsOfServiceButtonClickHandler);
            languageButton.onClick.AddListener(LanguageButtonClickHandler);
            googleButton.onClick.AddListener(GoogleButtonClickHandler);
            facebookButton.onClick.AddListener(FacebookButtonClickHandler);
            Signals.Get<vandrouka.m2.signals.playfab.OnPlayFabLink>().AddListener(OnPlayFabLinkHandler);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            supportButton.onClick.RemoveListener(SupportButtonClickHandler);
            notificationsButton.onClick.RemoveListener(NotificationsButtonClickHandler);
            privacyPolicyButton.onClick.RemoveListener(PrivacyPolicyButtonClickHandler);
            termsOfServiceButton.onClick.RemoveListener(TermsOfServiceButtonClickHandler);
            languageButton.onClick.RemoveListener(LanguageButtonClickHandler);
            googleButton.onClick.RemoveListener(GoogleButtonClickHandler);
            facebookButton.onClick.RemoveListener(FacebookButtonClickHandler);
            Signals.Get<vandrouka.m2.signals.playfab.OnPlayFabLink>().RemoveListener(OnPlayFabLinkHandler);
        }

        private void OnPlayFabLinkHandler(Authtypes authtype)
        {
            InitAccountButtons();
        }

        void Start()
        {
            InjectService.BindFields(this);
            
            musicSwitcher.config(
                () => AppInfo.soundSystem.isBgMusicOn,
                state => AppInfo.soundSystem.isBgMusicOn = state
            );
            sfxSwitcher.config(
                () => AppInfo.soundSystem.isSFXOn,
                state => AppInfo.soundSystem.isSFXOn = state
            );
            
            notificationsSwitcher.config(
                () => _pushNotificationsManager.IsOn,
                state => _pushNotificationsManager.IsOn = state
            );

            InitAccountButtons();

            buildVersionLabel.text = $"Build Version: {Application.version}";
            userIdLabel.text = $"User ID: {PlayFabAuthService.Instance.PlayFabId}";
        }

        private void InitAccountButtons()
        {
            var isFacebookLoggedIn = PlayFabAuthService.Instance.IsFbLoggedIn;
            var isGoogleLoggedIn = PlayFabAuthService.Instance.IsGoogleLoggedIn;
            facebookButton.interactable = !isFacebookLoggedIn;
            googleButton.interactable = !isGoogleLoggedIn;
            facebookButtonLabel.text = isFacebookLoggedIn ? Util.localizeOrSrc(FACEBOOK_LOGGED_LABEL) : Util.localizeOrSrc(FACEBOOK_LOGIN_LABEL);
            googleButtonLabel.text = isGoogleLoggedIn ? Util.localizeOrSrc(GOOGLE_LOGGED_LABEL) : Util.localizeOrSrc(GOOGLE_LOGIN_LABEL);
            StartCoroutine(SetSameFontSizeCoroutine());
        }

        IEnumerator SetSameFontSizeCoroutine()
        {
            yield return null;

            if (googleButtonLabel.fontSize != facebookButtonLabel.fontSize)
            {
                var fontSize = Mathf.Min(googleButtonLabel.fontSize, facebookButtonLabel.fontSize);
                googleButtonLabel.enableAutoSizing = false;
                googleButtonLabel.fontSize = fontSize;

                facebookButtonLabel.enableAutoSizing = false;
                facebookButtonLabel.fontSize = fontSize;
            }
        }

        protected override void SetButtonCallbacks()
        {
            _closeButton.OnClick.OnTrigger.Event.AddListener(OnClickClose);
        }

        private void PrivacyPolicyButtonClickHandler()
        {
            AppInfo.soundSystem.playSfx(AppInfo.soundSystem.config.common_Click_Sound);
            Application.OpenURL(Const.PRIVACY_POLICY_URL);
        }

        private void TermsOfServiceButtonClickHandler()
        {
            AppInfo.soundSystem.playSfx(AppInfo.soundSystem.config.common_Click_Sound);
            Application.OpenURL(Const.TERMS_OF_SERVICE_URL);
        }

        private void LanguageButtonClickHandler()
        {
            AppInfo.soundSystem.playSfx(AppInfo.soundSystem.config.common_Click_Sound);
            ChangeLanguagePopup.Show(true);
        }

        private void FacebookButtonClickHandler()
        {
            Interaction.BlockUserInputWithThrobber();

            AppInfo.soundSystem.playSfx(AppInfo.soundSystem.config.common_Click_Sound);
            Signals.Get<vandrouka.m2.signals.playfab.OnFaceBookLogin>().Dispatch();
        }

        private void GoogleButtonClickHandler()
        {
            Interaction.BlockUserInputWithThrobber();

            AppInfo.soundSystem.playSfx(AppInfo.soundSystem.config.common_Click_Sound);
            Signals.Get<vandrouka.m2.signals.playfab.OnGooglePlayLogin>().Dispatch();
        }

    }
}
