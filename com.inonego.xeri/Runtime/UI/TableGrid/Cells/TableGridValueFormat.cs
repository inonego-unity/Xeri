/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TableGridValueFormat.cs
수정일 : 2026-11-22

# 설명
셀 값을 데이터로 다루는 값 규칙을 정의한다. 글자로 나타내기(ToText), 글자에서 읽기(TryParse),
검증(Validate), 비교(Compare), 지울 때의 값(ClearValue), 클립보드 값 묶음 복사(CopyValue)를 맡는다.
비제네릭 기반은 등록소·열·필터 UI가 다루는 면이고, 값은 제네릭 하위 형식에서 박싱 없이 다룬다.

# 특이사항, 제약사항
값 규칙은 상태를 두지 않는다(모든 셀과 열이 공유한다).
비제네릭 기반의 박싱 경로(internal)는 기반 형식에 등록한 값 규칙을 하위 형식 열에 쓸 때와
편집·클립보드 경계에서만 쓴다. 외부 어셈블리는 제네릭 하위 형식을 상속해 값 규칙을 만든다.
기본 복사(CopyValue)는 값 형식·글자는 그대로, [Serializable] 클래스는 Xeri Core UnityJsonSerializer로 깊은 복사,
그 밖(UnityEngine.Object, 배열·컬렉션, 추상·하위 형식 값)은 참조를 그대로 쓴다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Serializable;
using inonego.Xeri.UI;

namespace inonego.Xeri.UI.TableGrid
{
    // ============================================================
    /// <summary>
    /// 등록소·열이 다루는 값 규칙의 비제네릭 면.
    /// </summary>
    // ============================================================
    public abstract class TableGridValueFormat
    {

    #region 사양

        // ------------------------------------------------------------
        /// <summary>
        /// 값 규칙이 다루는 값 형식.
        /// </summary>
        // ------------------------------------------------------------
        public abstract Type ValueType { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 필터 편집 UI가 고를 입력 종류. 기본은 Text다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual TableGridColumnValueKind Kind => TableGridColumnValueKind.Text;

        // ------------------------------------------------------------
        /// <summary>
        /// 선택형 값의 후보. 칩 색은 이 순서로 정한다. 기본은 비어 있다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual IReadOnlyList<string> Choices => Array.Empty<string>();

        // ------------------------------------------------------------
        /// <summary>
        /// 글자에서 값을 읽을 수 있는지 여부. 없으면 밖에서 온 글자를 건너뛴다.
        /// </summary>
        // ------------------------------------------------------------
        public abstract bool CanParse { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 지우기를 받는지 여부. 받으면 지울 때 ClearValue를 요청한다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual bool CanClear => false;

    #endregion

    #region 박싱 경로

        internal abstract string ToTextBoxed(object value);

        internal abstract bool TryParseBoxed(string text, out object value, out string error);

        internal abstract bool ValidateBoxed(object value, out string error);

        internal abstract int CompareBoxed(object left, object right);

        internal abstract object ClearValueBoxed { get; }

        internal abstract object CopyValueBoxed(object value);

    #endregion

    }

    // ============================================================
    /// <summary>
    /// TValue 값을 박싱 없이 다루는 값 규칙.
    /// </summary>
    // ============================================================
    public abstract class TableGridValueFormat<TValue> : TableGridValueFormat
    {

    #region 필드

        private const string NOT_PARSEABLE = "글자에서 값을 읽을 수 없는 열입니다";

        private static readonly bool isComparable = IsComparableType(typeof(TValue));

    #endregion

    #region 사양

        // ------------------------------------------------------------
        /// <summary>
        /// 값 규칙이 다루는 값 형식(TValue).
        /// </summary>
        // ------------------------------------------------------------
        public sealed override Type ValueType => typeof(TValue);

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 글자에서 값을 읽을 수 있는지 여부. 기본은 읽지 않는다.
        /// <br/> TryParse를 재정의하는 하위 형식은 이것도 true로 재정의한다.
        /// </summary>
        // ------------------------------------------------------------
        public override bool CanParse => false;

        // ------------------------------------------------------------
        /// <summary>
        /// 지울 때 요청할 값. CanClear가 true일 때만 쓴다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual TValue ClearValue => default;

    #endregion

    #region 값 규칙

        // ------------------------------------------------------------
        /// <summary>
        /// 값을 화면·검색·복사에 쓰는 글자로 나타낸다.
        /// </summary>
        // ------------------------------------------------------------
        public abstract string ToText(TValue value);

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 글자에서 값을 읽는다. 읽지 못하면 false와 이유를 돌려준다.
        /// <br/> 기본은 읽을 수 없다(재정의하면 CanParse도 true로 재정의한다).
        /// </summary>
        // ----------------------------------------------------------------------
        public virtual bool TryParse(string text, out TValue value, out string error)
        {
            value = default;
            error = NOT_PARSEABLE;
            return false;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 값이 열에 들어갈 수 있는지 확인한다. 기본은 모두 받는다.
        /// </summary>
        // ------------------------------------------------------------
        public virtual bool Validate(TValue value, out string error)
        {
            error = null;
            return true;
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 두 값을 비교한다. 기본은 비교 가능한 형식이면 Comparer.Default,
        /// <br/> 아니면 ToText 글자를 현재 문화권 대소문자 무시로 비교한다.
        /// </summary>
        // ----------------------------------------------------------------------
        public virtual int Compare(TValue left, TValue right)
        {
            if (isComparable)
            {
                return Comparer<TValue>.Default.Compare(left, right);
            }

            return string.Compare(ToText(left), ToText(right), StringComparison.CurrentCultureIgnoreCase);
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 클립보드 값 묶음에 담을 복사본을 만든다. 기본은 값 형식·글자는 그대로,
        /// <br/> [Serializable] 클래스는 깊은 복사, 그 밖은 참조를 그대로 쓴다.
        /// </summary>
        // ----------------------------------------------------------------------
        public virtual TValue CopyValue(TValue value)
        {
            return TableGridValueCopy.Copy(value);
        }

        private static bool IsComparableType(Type type)
        {
            var underlying = Nullable.GetUnderlyingType(type) ?? type;
            return typeof(IComparable<>).MakeGenericType(underlying).IsAssignableFrom(underlying)
                || typeof(IComparable).IsAssignableFrom(underlying);
        }

    #endregion

    #region 박싱 경로

        internal sealed override string ToTextBoxed(object value)
        {
            return ToText(TableGridBoxing.Unbox<TValue>(value));
        }

        internal sealed override bool TryParseBoxed(string text, out object value, out string error)
        {
            var parsed = TryParse(text, out var typed, out error);
            value = typed;
            return parsed;
        }

        internal sealed override bool ValidateBoxed(object value, out string error)
        {
            return Validate(TableGridBoxing.Unbox<TValue>(value), out error);
        }

        internal sealed override int CompareBoxed(object left, object right)
        {
            return Compare(TableGridBoxing.Unbox<TValue>(left), TableGridBoxing.Unbox<TValue>(right));
        }

        internal sealed override object ClearValueBoxed => ClearValue;

        internal sealed override object CopyValueBoxed(object value)
        {
            return value == null ? null : CopyValue(TableGridBoxing.Unbox<TValue>(value));
        }

    #endregion

    }

    // ======================================================================
    /// <summary>
    /// <br/> 기반 형식(또는 인터페이스)에 등록한 값 규칙을 하위 형식 열에 쓰도록 감싼다.
    /// <br/> 값은 박싱 경로로 넘긴다.
    /// </summary>
    // ======================================================================
    internal sealed class TableGridFormatAdapter<TValue> : TableGridValueFormat<TValue>
    {

    #region 필드

        private readonly TableGridValueFormat inner;

    #endregion

    #region 생성

        // ------------------------------------------------------------
        /// <summary>
        /// TValue를 받을 수 있는 값 규칙을 감싼다.
        /// </summary>
        // ------------------------------------------------------------
        public TableGridFormatAdapter(TableGridValueFormat inner)
        {
            this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
            if (!inner.ValueType.IsAssignableFrom(typeof(TValue)))
            {
                throw new ArgumentException($"값 규칙 형식 {inner.ValueType}이 {typeof(TValue)} 값을 받지 않습니다.", nameof(inner));
            }
        }

    #endregion

    #region 값 규칙

        // ------------------------------------------------------------
        /// <summary>
        /// 감싼 값 규칙의 입력 종류.
        /// </summary>
        // ------------------------------------------------------------
        public override TableGridColumnValueKind Kind => inner.Kind;

        // ------------------------------------------------------------
        /// <summary>
        /// 감싼 값 규칙의 선택형 후보.
        /// </summary>
        // ------------------------------------------------------------
        public override IReadOnlyList<string> Choices => inner.Choices;

        // ------------------------------------------------------------
        /// <summary>
        /// 감싼 값 규칙이 글자를 읽을 수 있는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public override bool CanParse => inner.CanParse;

        // ------------------------------------------------------------
        /// <summary>
        /// 감싼 값 규칙이 지우기를 받는지 여부.
        /// </summary>
        // ------------------------------------------------------------
        public override bool CanClear => inner.CanClear;

        // ------------------------------------------------------------
        /// <summary>
        /// 감싼 값 규칙의 지울 때 값. TValue가 아니면 기본값이다.
        /// </summary>
        // ------------------------------------------------------------
        public override TValue ClearValue => inner.ClearValueBoxed is TValue typed ? typed : default;

        // ------------------------------------------------------------
        /// <summary>
        /// 감싼 값 규칙으로 글자를 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public override string ToText(TValue value)
        {
            return inner.ToTextBoxed(value);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 감싼 값 규칙으로 읽고, TValue가 아닌 값은 거부한다.
        /// </summary>
        // ------------------------------------------------------------
        public override bool TryParse(string text, out TValue value, out string error)
        {
            value = default;
            if (!inner.TryParseBoxed(text, out var parsed, out error))
            {
                return false;
            }

            if (parsed is TValue typed)
            {
                value = typed;
                return true;
            }

            // 기반 형식 값 규칙이 하위 형식이 아닌 값을 읽었으면 이 열에 넣을 수 없다.
            if (parsed != null)
            {
                error = "열 형식에 맞지 않는 값입니다";
                return false;
            }

            return true;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 감싼 값 규칙으로 검증한다.
        /// </summary>
        // ------------------------------------------------------------
        public override bool Validate(TValue value, out string error)
        {
            return inner.ValidateBoxed(value, out error);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 감싼 값 규칙으로 비교한다.
        /// </summary>
        // ------------------------------------------------------------
        public override int Compare(TValue left, TValue right)
        {
            return inner.CompareBoxed(left, right);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 감싼 값 규칙으로 복사한다. TValue가 아니면 원래 값을 쓴다.
        /// </summary>
        // ------------------------------------------------------------
        public override TValue CopyValue(TValue value)
        {
            return inner.CopyValueBoxed(value) is TValue typed ? typed : value;
        }

    #endregion

    }

    // ================================================================================
    /// <summary>
    /// 값 규칙 기본 복사. 값 형식·글자는 그대로, [Serializable] 클래스는 깊은 복사한다.
    /// </summary>
    // ================================================================================
    internal static class TableGridValueCopy
    {

    #region 복사

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 값을 복사한다. 깊은 복사는 값의 실제 형식이 T와 같은
        /// <br/> 구체 [Serializable] 클래스일 때만 하고, 그 밖은 참조를 그대로 돌려준다.
        /// <br/> 깊은 복사가 실패하면 예외를 그대로 낸다(참조로 바꿔 값을 공유하지 않는다).
        /// </summary>
        // ----------------------------------------------------------------------
        public static T Copy<T>(T value)
        {
            if (value == null)
            {
                return value;
            }

            var type = value.GetType();
            if (!CanDeepCopy(type, typeof(T)))
            {
                return value;
            }

            var serializer = UnityJsonSerializer.Default;
            return serializer.Deserialize<T>(serializer.Serialize(value));
        }

        private static bool CanDeepCopy(Type type, Type declared)
        {
            if (type.IsValueType || type == typeof(string) || type != declared || type.IsAbstract || type.IsArray)
            {
                return false;
            }

            if (typeof(UnityEngine.Object).IsAssignableFrom(type) || typeof(IEnumerable).IsAssignableFrom(type))
            {
                return false;
            }

            return type.IsSerializable;
        }

    #endregion

    }
}
