using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HCParticleManager : MonoBehaviour
{
    public ParticleSystem m_particleEmittor;
    public ParticleSystem[] m_particlesAll;

    // 1. 인스펙터를 통해 지정할 AudioSource 변수 추가
    public AudioSource m_audioSource;

    public void EmmitParticle()
    {
        if (m_particleEmittor != null)
        {
            m_particleEmittor.Emit(3);
        }
    }

    public void EmmitParticleInt(int emitCount)
    {
        if (m_particleEmittor != null)
        {
            m_particleEmittor.Emit(emitCount);
        }
    }

    public void PlayParticleOnce()
    {
        if (m_particleEmittor != null)
        {
            m_particleEmittor.Stop();
            m_particleEmittor.Play();
        }
    }

    public void EmitAllParticle()
    {
        if (m_particlesAll != null)
        {
            foreach (ParticleSystem m_currentParticle in m_particlesAll)
            {
                m_currentParticle.Stop();
                m_currentParticle.Play();
            }
        }
    }

    public void PlayParticleArray(int index)
    {
        // 인덱스 범위 초과 에러를 방지하기 위해 간단한 안전 검사 추가
        if (m_particlesAll != null && index >= 0 && index < m_particlesAll.Length)
        {
            m_particlesAll[index].Stop();
            m_particlesAll[index].Play();
        }
    }

    // 2. 해당 오디오소스에 지정된 AudioClip을 1샷 플레이하는 함수
    // 3. public으로 선언되어 애니메이션 이벤트 창에서 함수 이름(PlaySoundOneShot)을 선택해 호출 가능
    public void PlaySoundOneShot()
    {
        if (m_audioSource != null && m_audioSource.clip != null)
        {
            // PlayOneShot을 사용하면 사운드가 겹쳐서 여러 번 호출되어도 소리가 끊기지 않고 자연스럽게 재생됩니다.
            m_audioSource.PlayOneShot(m_audioSource.clip);
        }
    }
}