/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : Singleton.cs
수정일 : 2026-08-03

# 설명
순수 C# 슬롯 싱글톤 기반 클래스.
T마다 독립된 InstanceRegistry<T>를 static으로 보유하며, 슬롯 로직은 모두 레지스트리에 위임한다.
Register / TryRegister / Unregister / Scope / OpenScope / CloseScope / Current / Named 정적 API를 제공한다.
========================================================================= BLOCK_HEADER_END */

using System;

namespace inonego.Xeri
{
    // ======================================================================
    /// <summary>
    /// <br/> 슬롯 기반 싱글톤 기본 클래스.
    /// <br/> Register로 슬롯에 인스턴스를 등록하고, Scope로 현재 컨텍스트를 전환한다.
    /// </summary>
    // ======================================================================
    public abstract class Singleton<T>
    where T : Singleton<T>
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
        /// <br/> 다른 인스턴스가 점유 중이면 기존 소유자를 유지하고 false를 반환한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static bool TryRegister(T instance)
        {
            return registry.TryRegister(instance);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 지정한 슬롯 등록을 시도한다.
        /// <br/> 다른 인스턴스가 점유 중이면 기존 소유자를 유지하고 false를 반환한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public static bool TryRegister(string key, T instance)
        {
            return registry.TryRegister(key, instance);
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

    }
}
