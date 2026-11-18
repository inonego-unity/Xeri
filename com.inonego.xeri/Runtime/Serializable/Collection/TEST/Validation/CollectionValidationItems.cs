/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionValidationItems.cs
수정일 : 2026-11-18

# 설명
컬렉션 Editor 수동 검증 씬에서 원소로 쓰는 검증용 타입.
단순 값 필드, 중첩 컬렉션, Unity 오브젝트 참조를 가진 SerializeReference 구현체와 직접 편집 열 검증용 값 타입을 제공한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections.Generic;

using UnityEngine;

namespace inonego.Xeri.Serializable.Validation
{
    // ============================================================
    /// <summary>
    /// SerializeReference 원소의 선언 형식. 형식 선택기 후보의 기준이 된다.
    /// </summary>
    // ============================================================
    public interface IValidationItem
    {
        string Describe();
    }

    // ============================================================
    /// <summary>
    /// 숫자 하나만 가진 가장 단순한 구현체.
    /// </summary>
    // ============================================================
    [Serializable]
    public sealed class ValidationNumberItem : IValidationItem
    {
        public int Value;

        public ValidationNumberItem() { }

        public ValidationNumberItem(int value)
        {
            Value = value;
        }

        public string Describe() => $"Number {Value}";
    }

    // ============================================================
    /// <summary>
    /// 문자열과 열거형, 플래그를 가진 구현체.
    /// </summary>
    // ============================================================
    [Serializable]
    public sealed class ValidationTextItem : IValidationItem
    {
        public string Text = "TEXT";
        public ValidationKind Kind = ValidationKind.Alpha;
        public bool Enabled = true;

        public ValidationTextItem() { }

        public ValidationTextItem(string text, ValidationKind kind = ValidationKind.Alpha)
        {
            Text = text;
            Kind = kind;
        }

        public string Describe() => $"Text {Text}";
    }

    // ============================================================
    /// <summary>
    /// 중첩 컬렉션과 Unity 오브젝트 참조를 가진 복합 구현체. 상세 편집창 검증용이다.
    /// </summary>
    // ============================================================
    [Serializable]
    public sealed class ValidationGroupItem : IValidationItem
    {
        public string Name = "GROUP";
        public List<int> Values = new() { 1, 2, 3 };
        public GameObject Target;
        public Color Tint = Color.white;

        public ValidationGroupItem() { }

        public ValidationGroupItem(string name, params int[] values)
        {
            Name = name;
            Values = new List<int>(values);
        }

        public string Describe() => $"Group {Name} ({Values?.Count ?? 0})";
    }

    // ============================================================
    /// <summary>
    /// 열거형 검증용 값.
    /// </summary>
    // ============================================================
    public enum ValidationKind
    {
        Alpha,
        Beta,
        Gamma,
        Delta,
    }

    // ============================================================
    /// <summary>
    /// [CollectionEditor(Columns)]로 직접 편집 열을 지정하는 값 타입 원소.
    /// </summary>
    // ============================================================
    [Serializable]
    public struct ValidationRow
    {
        public string Label;
        public int Amount;
        public float Ratio;
        public bool Enabled;
        public ValidationKind Kind;

        public ValidationRow(string label, int amount, float ratio, bool enabled, ValidationKind kind)
        {
            Label = label;
            Amount = amount;
            Ratio = ratio;
            Enabled = enabled;
            Kind = kind;
        }
    }
}
