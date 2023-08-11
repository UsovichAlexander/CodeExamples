using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using vandrouka.m2.app.SpriteManager;

namespace vandrouka.m2.m2.Animations
{
    [RequireComponent(typeof(ParticleSystem))]
	public class ResourceFlightAnimation : MonoBehaviour
	{
        [Serializable]
        private struct AmountToParticles
        {
            public int amount;
            public int particles;
        }

        [SerializeField] private float delayBeforeFlight;
		[SerializeField] private int[] levelsToCoinsArray;
        [SerializeField] private List<AmountToParticles> amountToParticlesArray;

        private IndicatorGetResourceAnimation _indicator;
		private ParticleSystem particles;
        private int particlesNumber;

		private void Awake()
        {
			particles = GetComponent<ParticleSystem>();
		}

        public void PlaySinglePropAnimation(string propTextId, IndicatorGetResourceAnimation targetIndicator, bool shouldActivateInput = false)
        {
            _indicator = targetIndicator;
            _indicator.shouldActivateInput = shouldActivateInput;
            particles.trigger.SetCollider(0, _indicator.GetComponent<BoxCollider2D>());

            var propSprite = SpriteManager.getTileSprite(propTextId);
            particles.GetComponent<Renderer>().material.mainTexture = propSprite.texture;
            particles.textureSheetAnimation.SetSprite(0,propSprite);

            StartCoroutine(PlayParticles(1));
        }

        public void PlayAnimationByResAmount(int amount, IndicatorGetResourceAnimation targetIndicator)
        {
            _indicator = targetIndicator;
            _indicator.chipLevel = levelsToCoinsArray.Length - 1;
            particles.trigger.SetCollider(0, _indicator.GetComponent<BoxCollider2D>());

            particlesNumber = ConvertAmountToNumberOfParticles(amount);
            StartCoroutine(PlayParticles(particlesNumber));
        }

		public void PlayAnimation(int chipLevel, IndicatorGetResourceAnimation targetIndicator, bool shouldActivateInput = false)
        {
            _indicator = targetIndicator;
            _indicator.shouldActivateInput = shouldActivateInput;
            _indicator.chipLevel = chipLevel;
            particles.trigger.SetCollider(0, _indicator.GetComponent<BoxCollider2D>());

            particlesNumber = ConvertLevelToNumberOfParticles(chipLevel);
            StartCoroutine(PlayParticles(particlesNumber));
		}
        
		IEnumerator PlayParticles(int particlesCount)
		{
			particles.Emit(particlesCount);
            yield return new WaitForSeconds(ConvertNumberOfParticlesToDelay());

            particles.externalForces.AddInfluence(_indicator.GetComponent<ParticleSystemForceField>());
        }

        private int ConvertLevelToNumberOfParticles(int chipLevel)
        {
            return levelsToCoinsArray[Mathf.Clamp(chipLevel - 1, 0, levelsToCoinsArray.Length - 1)];
        }

        private int ConvertAmountToNumberOfParticles(int amount)
        {
            if (amountToParticlesArray.Count == 0) return 0;

            for (var i = 0; i < amountToParticlesArray.Count; i++)
            {
                if (amountToParticlesArray[i].amount > amount)
                {
                    return amountToParticlesArray[Mathf.Clamp(i - 1, 0, amountToParticlesArray.Count - 1)].particles;
                }
            }

            return amountToParticlesArray[amountToParticlesArray.Count - 1].particles;
        }

        private float ConvertNumberOfParticlesToDelay()
        {
            return Mathf.Clamp(0.03f * particlesNumber, 0.1f, 0.6f);
        }

        private List<ParticleSystem.Particle> particlesList = new List<ParticleSystem.Particle>();
        private void OnParticleTrigger()
        {
            int numEnter = particles.GetTriggerParticles(ParticleSystemTriggerEventType.Enter, particlesList);
            if (numEnter > 0)
            {
                _indicator.PlayAnimation(gameObject.GetInstanceID().ToString());
            }
        }

    }
}
