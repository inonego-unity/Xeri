/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : MonoSingleton.cs
수정일 : 2026-08-03

# 설명
Singleton<T>와 구조·API 동일한 MonoBehaviour 슬롯 싱글톤 기반 클래스.
MonoBehaviour 단일 상속 제약으로 Singleton<T>를 상속할 수 없어 독립 구현한다.
Register로 수동 등록하며, OnDestroy 시 등록된 모든 슬롯에서 자동 해제된다.
Register / TryRegister / Unregister / Scope / OpenScope / CloseScope / Current / Named 정적 API를 제공한다.
========================================================================= BLOCK_HEADER_END */

using System;

using UnityEngine;

namespace inonego.Xeri
{
    // ======================================================================
    /// <summary>
    /// <br/> 슬롯 기반 MonoBehaviour 싱글톤 기본 클래스.
    /// <br/> Register로 수동 등록하며, OnDestroy 시 모든 슬롯에서 자동 해제된다.
    /// </summary>
    // ======================================================================
    public abstract class MonoSingleton<T> : MonoBehaviour
    where T : MonoSingleton<T>
    {

    #region 필드

        // T마다 독립된 레지스트리 인스턴스
        private static readonly InstanceRegistry<T> registry = new();

        // ------------------------------------------------------------
        /// <summary>
        /// 기본 슬롯 키.
        /// </summary>
        // ------------------------------------------------------------
        public const string DEFAULT_SLOT = InstanceRegistry.DEFAULT_SLOT;

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 컨텍스트의 슬롯 인스턴스. 미등록 슬롯 접근 시 예외.
        /// </summary>
        // ------------------------------------------------------------
        public static T Current => registry.Current;

        // ------------------------------------------------------------
        /// <summary>
        /// 이름으로 슬롯에 직접 접근한다.
        /// </summary>
        // ------------------------------------------------------------
        public static InstanceRegistry<T>.NamedAccessor Named => registry.Named;

    #endregion

    #region 메서드

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 컨텍스트의 슬롯 인스턴스를 안전하게 가져온다.
        /// </summary>
        // ------------------------------------------------------------
        public static bool TryCurrent(out T instance) => registry.TryCurrent(out instance);

        // ------------------------------------------------------------
        /// <summary>
        /// DEFAULT_SLOT에 인스턴스를 등록한다.
        /// </summary>
        // ------------------------------------------------------------
        public static void Register(T instance)
        {
            registry.Register(instance);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 슬롯에 인스턴스를 등록한다.
        /// </summary>
        // ------------------------------------------------------------
        public static void Register(string key, T instance)
        {
            registry.Register(key, instance);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> DEFAULT_SLOT 등록을 시도한다.
        /// <br/> 살아 있는 다른 인스턴스가 점유 중이면 기존 소유자를 유지하고 false를 반환한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static bool TryRegister(T instance)
        {
            return TryRegister(DEFAULT_SLOT, instance);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 지정한 슬롯 등록을 시도한다.
        /// <br/> 살아 있는 다른 인스턴스가 점유 중이면 기존 소유자를 유지하고 false를 반환한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static bool TryRegister(string key, T instance)
        {
            // 파괴된 Unity Object는 소유자가 아니므로 남은 fake-null 슬롯을 먼저 비운다.
            if (Named.TryGet(key, out var existing) && existing == null)
            {
                Unregister(key);
            }

            return registry.TryRegister(key, instance);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> DEFAULT_SLOT 에 등록을 시도하고, 이미 다른 인스턴스가 점유 중이면
        /// <br/> 호출자의 GameObject 를 파괴한다. 점유 성공 시 true, 양보 시 false.
        /// </summary>
        // ----------------------------------------------------------------------
        public static bool TryRegisterOrDestroy(T instance)
        {
            return TryRegisterOrDestroy(DEFAULT_SLOT, instance);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 지정한 슬롯에 등록을 시도하고, 이미 다른 인스턴스가 점유 중이면
        /// <br/> 호출자의 GameObject 를 파괴한다. 점유 성공 시 true, 양보 시 false.
        /// </summary>
        // ----------------------------------------------------------------------
        public static bool TryRegisterOrDestroy(string key, T instance)
        {
            // 비파괴 점유가 실패한 경우에만 호출자의 GameObject를 정리한다.
            if (!TryRegister(key, instance))
            {
                if (Application.isPlaying)
                {
                    Destroy(instance.gameObject);
                }
                else
                {
                    DestroyImmediate(instance.gameObject);
                }

                return false;
            }

            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 지정한 슬롯 등록을 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        public static void Unregister(string key)
        {
            registry.Unregister(key);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 인스턴스가 등록된 모든 슬롯을 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        public static void Unregister(T instance)
        {
            registry.Unregister(instance);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 지정한 슬롯을 현재 컨텍스트로 설정하는 스코프를 반환한다.
        /// <br/> Dispose 시 이전 슬롯으로 자동 복원된다.
        /// </summary>
        // ------------------------------------------------------------
        public static IDisposable Scope(string key)
        {
            return registry.Scope(key);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 지정한 슬롯을 현재 컨텍스트로 설정한다.
        /// <br/> CloseScope()로 명시적으로 닫을 때까지 유지된다.
        /// <br/> 프로덕션 코드에서는 Scope() + using을 사용할 것.
        /// </summary>
        // ------------------------------------------------------------
        public static void OpenScope(string key)
        {
            registry.OpenScope(key);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 가장 최근에 열린 스코프를 닫고 이전 슬롯으로 복원한다.
        /// <br/> 열린 스코프가 없을 시 InvalidOperationException이 발생한다.
        /// </summary>
        // ------------------------------------------------------------
        public static void CloseScope()
        {
            registry.CloseScope();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 모든 슬롯을 초기화한다.
        /// </summary>
        // ------------------------------------------------------------
        public static void Clear()
        {
            registry.Clear();
        }

    #endregion

    #region 유니티 이벤트

        // ------------------------------------------------------------
        /// <summary>
        /// 등록된 모든 슬롯에서 자동 해제한다.
        /// </summary>
        // ------------------------------------------------------------
        protected virtual void OnDestroy()
        {
            registry.Unregister(this as T);
        }

    #endregion

    }
}
