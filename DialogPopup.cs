#define M2TRACE
using System;
using System.Collections;
using System.Collections.Generic;
using deVoid.Utils;
using Doozy.Engine.UI;
using UnityEngine;
using vandrouka.m2.app;
using vandrouka.m2.app.conf;
using vandrouka.m2.mg.map;
using vandrouka.m2.ui;
using vandrouka.m2.util;

namespace vandrouka.m2.mg.ui
{
    public class DialogPopup : BasePopup<DialogPopup>
    {
        private static readonly int ShowStringToHash = Animator.StringToHash("show");
        private static readonly int HideStringToHash = Animator.StringToHash("hide");
        private static readonly int IdleStringToHash = Animator.StringToHash("idle");
        private const string CURTAIN_POPUP_NAME = "CurtainPopup";

        #region Serialized Fields
        [SerializeField] private SpeechIconView speechIconLeft;
        [SerializeField] private SpeechIconView speechIconRight;
        [SerializeField] private AnimatedLettersText _dialogText;
        [SerializeField] private UIButton _nextButton;
        [SerializeField] private UIButton _skipButton;
        [SerializeField] private UIButton _overlayButton;

        [SerializeField] private Animator coversAnimator;
        [SerializeField] private Animator speechBubbleAnimator;
        [SerializeField] private Animator skipButtonAnimator;
        [SerializeField] private Animator speechIconsAnimator;
        [SerializeField] private float hideHudDuration = 0.5f;
        #endregion
        
        #region Private Properties
        private int _currentIndex;
        private Dialog _data;
        private Action<Dialog> _completeCallback;
        private Dictionary<string, DialogueBehaviour.ControlsInfo> _controlsDict;
        #endregion

        public CutsceneView cutsceneView;

        protected override void OnEnable()
        {
            Signals.Get<signals.meta.EndCutscenePartSignal>().AddListener(EndCutscenePartHandler);
            Signals.Get<signals.meta.ControlDialogElementFromTimelineSignal>().AddListener(ControlDialogElementFromTimelineSignalHandler);
            Signals.Get<signals.meta.InvertDialogElementStateFromTimelineSignal>().AddListener(InvertDialogElementStateFromTimelineSignalHandler);
            Interaction.SetVisibleHUDElements(false,hideHudDuration);

            base.OnEnable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            
            Signals.Get<signals.meta.EndCutscenePartSignal>().RemoveListener(EndCutscenePartHandler);
            Signals.Get<signals.meta.ControlDialogElementFromTimelineSignal>().RemoveListener(ControlDialogElementFromTimelineSignalHandler);
            Signals.Get<signals.meta.InvertDialogElementStateFromTimelineSignal>().RemoveListener(InvertDialogElementStateFromTimelineSignalHandler);
        }

        #region Public Methods
        public void Init(Dialog data, Dictionary<string, DialogueBehaviour.ControlsInfo> controlsDict, Action<Dialog> completeCallback = null)
        {
            _data = data;
            _completeCallback = completeCallback;
            _controlsDict = controlsDict;

            SetupClipDurations();
            StartCoroutine(StartDialogue());
        }
        #endregion

        private void EndCutscenePartHandler(bool hasToPause)
        {
            if (_currentIndex >= 0 && _currentIndex < _data.Speeches.Count && !hasToPause)
            {
                ToNextSpeech();
            }
        }
        
        private IEnumerator StartDialogue()
        {
            speechIconLeft.Reset();
            speechIconRight.Reset();
            var speechData = _data.GetSpeech(0);
            yield return null;
            Signals.Get<signals.meta.TryAddedCutsceneSignal>().Dispatch(_data.TextId, this);
            ShowSpeech(0);
            GamePlayStatistics.DialogStart(_data.TextId);
            Util.DebugLog($"Dialog {_data.TextId} has started");
            HideUIOnDialogStartDelayed();
        }
        
        private async void HideUIOnDialogStartDelayed()
        {
            await System.Threading.Tasks.Task.Delay(1000);
            Interaction.SetVisibleHUDElements(false,hideHudDuration);
        }

        private void OnClickNext()
        {
            var speechData = _data.GetSpeech(_currentIndex);
            var hasToPause = AppInfo.dialogsSystem.cutsceneControlsDict[speechData.Text].hasToPause;
            
            if(!AppInfo.dialogsSystem.cutsceneControlsDict[speechData.Text].hasToPause) return;

            AppInfo.soundSystem.playSfx(AppInfo.soundSystem.config.common_CutScene_Dialogue_Sound);

            if (_dialogText.IsAnimatingText())
            {
                _dialogText.CompleteTextAnimation();
            }
            else
            {
                ToNextSpeech();
            }
        }

        private void OnClickSkip()
        {
            AppInfo.soundSystem.playSfx(AppInfo.soundSystem.config.common_Close_Sound);
            GamePlayStatistics.SkipDialog(_data.TextId);
            Complete(true);
        }

        private void ToNextSpeech()
        {
            ShowSpeech(_currentIndex + 1);
        }
        
        private void Complete(bool isSkipped = false)
        {
            Hide();
            GamePlayStatistics.DialogEnd(_data.TextId, isSkipped);
        }

        private void ShowSpeech(int index)
        {
            speechIconLeft.ShowLabelEvent();
            speechIconRight.ShowLabelEvent();

            if (_data == null) return;

            _currentIndex = index;
            var speechData = _data.GetSpeech(index);

            if (speechData != null)
            {
                _nextButton.gameObject.SetActive(false);
                _nextButton.Interactable = false;

                if (_controlsDict.ContainsKey(speechData.Text))
                {
                    if (_controlsDict[speechData.Text].showSpeechBubble)
                    {
                        _dialogText.SetText(speechData.Text, ShowNextButton);
                    }

                    if (_controlsDict[speechData.Text].showCharacters)
                    {
                        speechIconLeft.Init(speechData.IconLeft);
                        speechIconRight.Init(speechData.IconRight);
                    }
                    else
                    {
                        speechIconLeft.SetLabel(speechData.IconLeft.CharacterTextId);
                        speechIconLeft.SetIsActiveSpeeker(speechData.IconLeft.IsActive);

                        speechIconRight.SetLabel(speechData.IconRight.CharacterTextId);
                        speechIconRight.SetIsActiveSpeeker(speechData.IconRight.IsActive);
                    }

                    if (!speechData.Visibility)
                    {
                        speechIconLeft.EndAnimatedEvent();
                        speechIconRight.EndAnimatedEvent();
                    }
                }
                else
                {
                    Util.DebugLogError($"Can't find controls for {speechData.Text}. Wrong cutscene part name.");
                }
                if (index != 0)
                    Signals.Get<signals.meta.ShowDialogSpeechSignal>().Dispatch(speechData.Text);
            }
            else
            {
                Complete();
            }
        }

        private void ShowNextButton()
        {
            _nextButton.gameObject.SetActive(true);
            _nextButton.Interactable = true;
        }

        public override void Hide()
        {
            StartCoroutine(CloseCoroutine());
        }

        IEnumerator CloseCoroutine()
        {
            _nextButton.OnClick.OnTrigger.Event.RemoveListener(OnClickNext);
            _skipButton.OnClick.OnTrigger.Event.RemoveListener(OnClickSkip);
            _overlayButton.OnClick.OnTrigger.Event.RemoveListener(OnClickNext);

            var controls = new DialogueBehaviour.ControlsInfo
            {
                showCovers = false,
                showCharacters = false,
                showSpeechBubble = false,
                showSkipButton = false
            };
            yield return StartCoroutine(HideDialogElementsCoroutine(controls));

            Signals.Get<signals.ui.DialogFinishedSignal>().Dispatch(_data.TextId);
            popUp.Hide();
            _completeCallback?.Invoke(_data);

            Interaction.SetVisibleHUDElements(cutsceneView == null || cutsceneView.showTopHudAndTaskIconsOnFinished, hideHudDuration);
        }

        protected override void SetButtonCallbacks()
        {
            _nextButton.OnClick.OnTrigger.Event.AddListener(OnClickNext);
            _skipButton.OnClick.OnTrigger.Event.AddListener(OnClickSkip);
            _overlayButton.OnClick.OnTrigger.Event.AddListener(OnClickNext);
        }

        public enum DialogElements
        {
            COVERS,
            SPEECH_BUBBLE,
            CHARACTERS,
            SKIP_BUTTON
        }

        private void InvertDialogElementStateFromTimelineSignalHandler(DialogElements elementToTrigger)
        {
            var elementAnimator = elementToTrigger switch
            {
                DialogElements.COVERS => coversAnimator,
                DialogElements.SKIP_BUTTON => skipButtonAnimator,
                DialogElements.SPEECH_BUBBLE => speechBubbleAnimator,
                DialogElements.CHARACTERS => speechIconsAnimator,
                _ => null
            };

            if (elementAnimator == null) return;

            elementAnimator.Play(elementAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash == ShowStringToHash
                ? HideStringToHash
                : ShowStringToHash);
        }

        private void  ControlDialogElementFromTimelineSignalHandler(DialogueBehaviour.ControlsInfo controls)
        {
            StopAllCoroutines();
            StartCoroutine(ControlDialogElementsCoroutine(controls));
        }

        private IEnumerator ControlDialogElementsCoroutine(DialogueBehaviour.ControlsInfo controls)
        {
            yield return HideDialogElementsCoroutine(controls);
            yield return ShowDialogElementsCoroutine(controls);
        }

        private IEnumerator HideDialogElementsCoroutine(DialogueBehaviour.ControlsInfo controls)
        {
            if (!controls.showSkipButton)
            {
                SetDialogElementAnimatorState(skipButtonAnimator, false, out var isPlaying);
            }

            if (!controls.showCharacters)
            {
                SetDialogElementAnimatorState(speechIconsAnimator, false, out var isPlaying);
                if (isPlaying)
                {
                    yield return new WaitForSeconds(clipLengthDictionary[AnimationClips.CHARACTERS_HIDE] * 0.5f);
                }
            }

            if (!controls.showSpeechBubble)
            {
                SetDialogElementAnimatorState(speechBubbleAnimator, false, out var isPlaying);
            }

            if (!controls.showCovers)
            {
                SetDialogElementAnimatorState(coversAnimator, false, out var isPlaying);
                if (isPlaying)
                {
                    yield return new WaitForSeconds(clipLengthDictionary[AnimationClips.COVERS_HIDE]);
                }
            }
        }

        private IEnumerator ShowDialogElementsCoroutine(DialogueBehaviour.ControlsInfo controls)
        {
            if (controls.showCovers)
            {
                SetDialogElementAnimatorState(coversAnimator, true, out var isPlaying);
                if (isPlaying)
                {
                    yield return new WaitForSeconds(clipLengthDictionary[AnimationClips.COVERS_SHOW]);
                }
            }

            if (controls.showSpeechBubble)
            {
                SetDialogElementAnimatorState(speechBubbleAnimator, true, out var isPlaying);
                if (isPlaying)
                {
                    yield return new WaitForSeconds(clipLengthDictionary[AnimationClips.SKIP_BUTTON_SHOW] * 0.5f);
                }
            }

            if (controls.showCharacters)
            {
                SetDialogElementAnimatorState(speechIconsAnimator, true, out var isPlaying);
            }

            if (controls.showSkipButton)
            {
                SetDialogElementAnimatorState(skipButtonAnimator, true, out var isPlaying);
            }
        }

        private void SetDialogElementAnimatorState(Animator elementAnimator, bool isShow, out bool isPlaying)
        {
            if (isShow)
            {
                if (elementAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash != ShowStringToHash)
                {
                    elementAnimator.Play(ShowStringToHash);
                    isPlaying = true;
                    return;
                }
            }
            else
            {
                if (elementAnimator.GetCurrentAnimatorStateInfo(0).shortNameHash == ShowStringToHash)
                {
                    elementAnimator.Play(HideStringToHash);
                    isPlaying = true;
                    return;
                }
            }

            isPlaying = false;
        }

        private enum AnimationClips
        {
            COVERS_SHOW,
            COVERS_HIDE,
            SPEECH_BUBBLE_SHOW,
            SPEECH_BUBBLE_HIDE,
            SKIP_BUTTON_SHOW,
            SKIP_BUTTON_HIDE,
            CHARACTERS_SHOW,
            CHARACTERS_HIDE
        }

        private Dictionary<AnimationClips, float> clipLengthDictionary = new Dictionary<AnimationClips, float>();
        private void SetupClipDurations()
        {
            clipLengthDictionary.Add(AnimationClips.COVERS_SHOW, GetAnimationClipDuration(coversAnimator, "dialogCovers_show"));
            clipLengthDictionary.Add(AnimationClips.COVERS_HIDE, GetAnimationClipDuration(coversAnimator, "dialogCovers_hide"));

            clipLengthDictionary.Add(AnimationClips.SPEECH_BUBBLE_SHOW, GetAnimationClipDuration(speechBubbleAnimator, "dialogSpeechBubble_show"));
            clipLengthDictionary.Add(AnimationClips.SPEECH_BUBBLE_HIDE, GetAnimationClipDuration(speechBubbleAnimator, "dialogSpeechBubble_hide"));

            clipLengthDictionary.Add(AnimationClips.SKIP_BUTTON_SHOW, GetAnimationClipDuration(skipButtonAnimator, "dialogSkipButton_show"));
            clipLengthDictionary.Add(AnimationClips.SKIP_BUTTON_HIDE, GetAnimationClipDuration(skipButtonAnimator, "dialogSkipButton_hide"));

            clipLengthDictionary.Add(AnimationClips.CHARACTERS_SHOW, GetAnimationClipDuration(speechIconsAnimator, "dialogSpeechIcons_show"));
            clipLengthDictionary.Add(AnimationClips.CHARACTERS_HIDE, GetAnimationClipDuration(speechIconsAnimator, "dialogSpeechIcons_hide"));
        }

        private float GetAnimationClipDuration(Animator animator, string animationName)
        {
            var animatorClips = animator.runtimeAnimatorController.animationClips;
            foreach (var clip in animatorClips)
            {
                if (clip.name == animationName)
                {
                    return clip.length;
                }
            }

            return 0;
        }

    }
}
