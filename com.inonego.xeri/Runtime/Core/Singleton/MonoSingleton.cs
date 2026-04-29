/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : MonoSingleton.cs
수정일 : 2026-04-29

# 설명
Singleton<T>와 구조·API 동일한 MonoBehaviour 슬롯 싱글톤 기반 클래스.
MonoBehaviour 단일 상속 제약으로 Singleton<T>를 상속할 수 없어 독립 구현한다.
Register로 수동 등록하며, OnDestroy 시 등록된 모든 슬롯에서 자동 해제된다.
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
