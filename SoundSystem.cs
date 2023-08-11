//#define M2TRACE 

using System;
using System.Collections;
using deVoid.Utils;
using Doozy.Engine.Soundy;
using UnityEngine;
using UnityEngine.Audio;
using vandrouka.m2.app.conf;
using vandrouka.m2.db.orm;
using vandrouka.m2.util;

namespace vandrouka.m2.app
{
    [CreateAssetMenu(fileName = "NewSoundSystem", menuName = Const.MENU_NAME + "New SoundSystem config", order = 0)]
    public class SoundSystem : ScriptableObject
    {
        public SoundConfigSO config;
        [SerializeField] private AudioMixerGroup sfxAMG;
        [SerializeField] private AudioMixerGroup ambientAMG;
        [SerializeField] private AudioMixerGroup musicAMG;
        private AudioSource backgroundAudioSource;
        private AudioSource ambientAudioSource;

        
        private const float MAX_CHIP_LEVEL = 12;

        private void OnEnable()
        {
            Signals.Get<signals.merge.MergeSignal>().AddListener(MergeSignalHandler);
            Signals.Get<signals.merge.OnNewChipOnFieldSignal>().AddListener(OnNewChipOnFieldSignalHandler);
            Signals.Get<signals.SwitchToMergeSignal>().AddListener(SwitchToMergeSignalHandler);
            Signals.Get<signals.SwitchToMetaSignal>().AddListener(SwitchToMetaSignalHandler);
        }

        private void OnDisable()
        {
            Signals.Get<signals.merge.MergeSignal>().RemoveListener(MergeSignalHandler);
            Signals.Get<signals.merge.OnNewChipOnFieldSignal>().RemoveListener(OnNewChipOnFieldSignalHandler);
            Signals.Get<signals.SwitchToMergeSignal>().RemoveListener(SwitchToMergeSignalHandler);
            Signals.Get<signals.SwitchToMetaSignal>().RemoveListener(SwitchToMetaSignalHandler);
        }

        private void MergeSignalHandler(FieldCellView fcvSrc, FieldCellView fcvDst)
        {
            var chip = AppInfo.getChipConfig(fcvSrc.propTypeTextID);

            if (chip.isMirror)
            {
                AppInfo.soundSystem.playSfx(AppInfo.soundSystem.config.common_UseMirror_Sound);
            }
            else if (chip.nextLevelChip == null || !AppInfo.isChipRequiredForTask(chip.nextLevelChip.name))
            {
                playSfx(config.common_MergeChip_Sound, fcvSrc.propTypeTextID);
            }
            else if (chip.isSundial)
            {
                AppInfo.soundSystem.playSfx(AppInfo.soundSystem.config.common_UseSundial_Sound);
            }
        }

        private void OnNewChipOnFieldSignalHandler(string textId, bool playSound)
        {
            if (!GameModeSelector.IsShowMerge) return;
            if (!playSound) return;
            if (AppInfo.isChipRequiredForTask(textId) || AppInfo.isChipRequiredForTimeOrder(textId))
            {
                AppInfo.soundSystem.playSfx(AppInfo.soundSystem.config.common_GotQuestItem_Sound);
            }
        }

        private void SwitchToMetaSignalHandler()
        {
            playAmbientSfx(config.common_Ambient_Music);
            playBackgroundMusic(config.common_Meta_Music);
        }

        private void SwitchToMergeSignalHandler()
        {
            stopAmbientSfx();
            playBackgroundMusic(config.common_Merge_Music);
        }
        
        private float ConvertTextIdToPitch(string propTypeTextId)
        {
            int level = AppInfo.getChipConfig(propTypeTextId).level;
            float pitchValue = Mathf.Pow(2, ((level - 1) / MAX_CHIP_LEVEL));
            return pitchValue;
        }

        #region Public Methods
        public bool isBgMusicOn
        {
            get
            {
                if (!PlayerPrefs.HasKey(Prefs.MUSIC_PREFS_KEY))
                {
                    isBgMusicOn = true;
                    return true;
                }
                return PlayerPrefs.GetInt(Prefs.MUSIC_PREFS_KEY) == 1;
            }

            set
            {
                PlayerPrefs.SetInt(Prefs.MUSIC_PREFS_KEY, value ? 1 : 0);
                PlayerPrefs.Save();
                if (value)
                {
                    backgroundAudioSource.Play();
                    ambientAudioSource.Play();
                }
                else
                {
                    stopBackgroundMusic();
                    stopAmbientSfx();
                }
            }
        }

        public bool isSFXOn
        {
            get
            {
                if (!PlayerPrefs.HasKey(Prefs.SFX_PREFS_KEY))
                {
                    isSFXOn = true;
                    return true;
                }
                return PlayerPrefs.GetInt(Prefs.SFX_PREFS_KEY) == 1;
            }
            set
            {
                PlayerPrefs.SetInt(Prefs.SFX_PREFS_KEY, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public void playBackgroundMusic(AudioClip ac)
        {
            if (backgroundAudioSource.clip != ac)
            {
                backgroundAudioSource.clip = ac;
                if (isBgMusicOn)
                {
                    backgroundAudioSource.Play();
                }
            }
        }

        public void stopBackgroundMusic()
        {
            backgroundAudioSource.Stop();
        }

        public void playSfx(AudioClip ac)
        {
            if (isSFXOn && ac != null)
            {
                SoundyManager.Play(ac).SetOutputAudioMixerGroup(sfxAMG);
            }
        }

        public void playSfx(AudioClip ac, string propTypeTextId)
        {
            if (isSFXOn)
            {
                var pitchValue = ConvertTextIdToPitch(propTypeTextId);
                SoundyManager.Play(ac,sfxAMG,null,1f,pitchValue);
            }
        }

        public IEnumerator playLoopedSfxCoroutine(AudioClip ac, float loopTime)
        {
            if (isSFXOn)
            {
                var soundController = SoundyManager.Play(ac, sfxAMG, null, 1f, 1f, true);
                yield return new WaitForSeconds(loopTime);
                soundController.Stop();
            }
        }

        public void playAmbientSfx(AudioClip ac)
        {
            if (!ambientAudioSource.isPlaying)
            {
                ambientAudioSource.clip = ac;
                if (isBgMusicOn)
                {
                    ambientAudioSource.Play();
                }
            }
        }

        public void stopAmbientSfx()
        {
            ambientAudioSource.Stop();
        }

        public void configure(Camera camera)
        {
            backgroundAudioSource = camera.gameObject.AddComponent<AudioSource>();
            backgroundAudioSource.outputAudioMixerGroup = musicAMG;
            backgroundAudioSource.loop = true;

            ambientAudioSource = camera.gameObject.AddComponent<AudioSource>();
            ambientAudioSource.outputAudioMixerGroup = ambientAMG;
            ambientAudioSource.loop = true;
        }

        public void SetAudioMixerSnapshot(AudioMixerSnapshot snapshot, float time)
        {
            snapshot.TransitionTo(time);
        }

        public void PlayShopSfx(SoundConfigSO.ShopSoundType soundType)
        {
            AudioClip sfxToPlay = null;

            switch (soundType)
            {
                case SoundConfigSO.ShopSoundType.NONE:
                    break;

                case SoundConfigSO.ShopSoundType.COINS:
                    sfxToPlay = config.common_AddingCoins_Sound;
                    break;

                case SoundConfigSO.ShopSoundType.GEMS:
                    sfxToPlay = config.common_AddingGems_Sound;
                    break;

                case SoundConfigSO.ShopSoundType.ENERGY:
                    sfxToPlay = config.common_CollectChip_Sound;
                    break;

                case SoundConfigSO.ShopSoundType.MONEY:
                    sfxToPlay = config.common_Click_Sound;
                    break;

                case SoundConfigSO.ShopSoundType.ADS:
                    sfxToPlay = config.common_CollectChip_Sound;
                    break;

                case SoundConfigSO.ShopSoundType.FREE:
                    sfxToPlay = config.common_CollectChip_Sound;
                    break;

                case SoundConfigSO.ShopSoundType.CANT_BUY:
                    sfxToPlay = config.common_Click_Sound;
                    break;
                case SoundConfigSO.ShopSoundType.CLICK:
                    sfxToPlay = config.common_Click_Sound;
                    break;
                default:
                    break;
            }

            if (sfxToPlay != null)
            {
                playSfx(sfxToPlay);
            }
        }

        public void PlayTaskTextHintSfx(SoundConfigSO.TaskTextHintSoundType soundSoundType)
        {
            AudioClip sfxToPlay = null;

            switch (soundSoundType)
            {
                case SoundConfigSO.TaskTextHintSoundType.NONE:
                    break;
                case SoundConfigSO.TaskTextHintSoundType.DONE:
                    sfxToPlay = config.TaskTextHintDoneSound;
                    break;
                case SoundConfigSO.TaskTextHintSoundType.GREAT:
                    sfxToPlay = config.TaskTextHintGreatSound;
                    break;
                case SoundConfigSO.TaskTextHintSoundType.YUMMY:
                    sfxToPlay = config.TaskTextHintYummySound;
                    break;
                case SoundConfigSO.TaskTextHintSoundType.CURIOUS:
                    sfxToPlay = config.TaskTextHintCuriousSound;
                    break;
                default:
                    break;
                    
            }

            if (sfxToPlay != null)
            {
                playSfx(sfxToPlay);
            }
        }

        #endregion
    }
}