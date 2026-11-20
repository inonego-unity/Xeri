/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : CollectionSource.cs
수정일 : 2026-11-19

# 설명
한 SerializedObject 안의 컬렉션 속성에서 행·셀을 읽는다.
행 배열 경로와 셀 경로는 종류 정의(CollectionKind)를 따른다.

# 특이사항, 제약사항
읽기 면만 가진다. 배열 조작, Undo 기록과 변경 확정은 CollectionSession만 한다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Editor;

namespace inonego.Xeri.Serializable.Editor
{
    // ============================================================
    /// <summary>
    /// 직렬화 컬렉션의 행·셀 읽기 면.
    /// </summary>
    // ============================================================
    internal sealed class CollectionSource
    {

    #region 필드

        // ------------------------------------------------------------
        /// <summary>
        /// 컬렉션을 담은 SerializedObject를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public SerializedObject SerializedObject { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 컬렉션 속성 경로를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public string Path { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 컬렉션 종류 정의를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public CollectionKind Kind { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 컬렉션 속성의 표시 이름을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public string DisplayName { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 같은 대상과 경로가 공유하는 변경 세대를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public ulong Revision => RevisionTracker.Get(SerializedObject.targetObject, Path);

        // ------------------------------------------------------------
        /// <summary>
        /// 현재 행 수를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public int Count => GetRows().arraySize;

    #endregion

    #region 생성자

        // ------------------------------------------------------------
        /// <summary>
        /// 컬렉션 속성과 종류 정의로 읽기 면을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public CollectionSource(SerializedObject serializedObject, string path, CollectionKind kind)
        {
            SerializedObject = serializedObject ?? throw new ArgumentNullException(nameof(serializedObject));
            Path = path ?? throw new ArgumentNullException(nameof(path));
            Kind = kind ?? throw new ArgumentNullException(nameof(kind));
            var property = GetProperty() ?? throw new ArgumentException($"속성을 찾을 수 없습니다: {path}", nameof(path));
            DisplayName = property.displayName;

            // 행 배열이 없으면 표를 만들기 전에 실패시킨다.
            _ = GetRows();
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 같은 컬렉션을 다른 SerializedObject로 읽는 면을 만든다.
        /// </summary>
        // ------------------------------------------------------------
        public CollectionSource Reopen(SerializedObject serializedObject)
        {
            return new CollectionSource(serializedObject, Path, Kind);
        }

    #endregion

    #region 행과 셀

        // ------------------------------------------------------------
        /// <summary>
        /// 컬렉션 속성을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public SerializedProperty GetProperty()
        {
            return SerializedObject.FindProperty(Path);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 행 배열 속성을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public SerializedProperty GetRows()
        {
            var property = GetProperty();
            var rows = Kind.RowsPath == null ? property : property?.FindPropertyRelative(Kind.RowsPath);
            if (rows == null || !rows.isArray || rows.propertyType == SerializedPropertyType.String)
            {
                throw new InvalidOperationException($"{Kind.Label}의 직렬화 목록을 찾을 수 없습니다: {Path}");
            }

            return rows;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 행 속성을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public SerializedProperty GetRow(int sourceIndex)
        {
            return GetRows().GetArrayElementAtIndex(sourceIndex);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 셀 속성을 반환하며 없으면 null이다.
        /// </summary>
        // ------------------------------------------------------------
        public SerializedProperty GetCell(int sourceIndex, CollectionColumn column)
        {
            var row = GetRow(sourceIndex);
            return column.RelativePath == null ? row : row.FindPropertyRelative(column.RelativePath);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 셀의 현재 직렬화 값을 boxed로 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public object GetValue(int sourceIndex, CollectionColumn column)
        {
            var cell = GetCell(sourceIndex, column);
            return cell == null ? null : SerializedPropertyHelper.GetValue(cell);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 속성 경로가 가리키는 행과 열을 찾는다.
        /// </summary>
        // ------------------------------------------------------------
        public bool TryLocate(string propertyPath, out int sourceIndex, out CollectionColumn column)
        {
            sourceIndex = -1;
            column = null;
            var rowsPath = GetRows().propertyPath + ".Array.data[";
            if (propertyPath == null || !propertyPath.StartsWith(rowsPath, StringComparison.Ordinal))
            {
                return false;
            }

            var close = propertyPath.IndexOf(']', rowsPath.Length);
            if (close < 0 || !int.TryParse(propertyPath.Substring(rowsPath.Length, close - rowsPath.Length), out sourceIndex))
            {
                sourceIndex = -1;
                return false;
            }

            // 행 아래 경로의 첫 구간이 열의 셀 경로다.
            var rest = close + 1 < propertyPath.Length ? propertyPath.Substring(close + 2) : string.Empty;
            foreach (var candidate in Kind.Columns)
            {
                if (candidate.RelativePath == null
                    || rest == candidate.RelativePath
                    || rest.StartsWith(candidate.RelativePath + ".", StringComparison.Ordinal))
                {
                    column = candidate;
                    return true;
                }
            }

            return true;
        }

    #endregion

    #region 행 식별과 표시 순서

        // ------------------------------------------------------------
        /// <summary>
        /// 정규화 뒤에도 같은 행을 다시 찾기 위한 값을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public object GetIdentity(int sourceIndex)
        {
            return SerializedPropertyHelper.GetValue(GetRow(sourceIndex));
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 식별 값과 일치하는 행을 찾고 없거나 둘 이상이면 -1을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public int Find(object identity)
        {
            if (identity == null)
            {
                return -1;
            }

            // 값 형식과 문자열은 값으로, 나머지는 같은 인스턴스로 비교한다.
            var byValue = identity.GetType().IsValueType || identity is string;
            var result = -1;
            for (var index = 0; index < Count; index++)
            {
                var candidate = GetIdentity(index);
                var matches = byValue ? Equals(identity, candidate) : ReferenceEquals(identity, candidate);
                if (!matches)
                {
                    continue;
                }

                if (result >= 0)
                {
                    return -1;
                }

                result = index;
            }

            return result;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 키 규칙에 따른 행의 키 값을 반환하며 키가 없으면 null이다.
        /// </summary>
        // ------------------------------------------------------------
        public object GetKey(int sourceIndex)
        {
            var rule = Kind.KeyRule;
            if (rule == null)
            {
                return null;
            }

            return rule.ColumnID == null ? GetIdentity(sourceIndex) : GetValue(sourceIndex, Kind.KeyColumn);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 키 값이 같은 유일한 행을 찾고 없거나 둘 이상이면 -1을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public int FindKey(object key)
        {
            var rule = Kind.KeyRule;
            if (rule == null || key == null)
            {
                return -1;
            }

            var result = -1;
            for (var index = 0; index < Count; index++)
            {
                if (!rule.Comparer.Equals(key, GetKey(index)))
                {
                    continue;
                }

                if (result >= 0)
                {
                    return -1;
                }

                result = index;
            }

            return result;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 표에 보일 순서의 행 인덱스 목록을 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        public List<int> GetDisplayRows()
        {
            var count = Count;
            var rows = new List<int>(count);
            for (var index = 0; index < count; index++)
            {
                rows.Add(index);
            }

            var column = Kind.DisplayOrderColumn;
            if (column == null)
            {
                return rows;
            }

            // 표시 순서만 정렬하고 직렬화 순서는 바꾸지 않는다. 같은 값은 저장 순서를 유지한다.
            var values = new object[count];
            for (var index = 0; index < count; index++)
            {
                values[index] = GetValue(index, column);
            }

            rows.Sort
            (
                (left, right) =>
                {
                    var result = Kind.DisplayOrderComparison(values[left], values[right]);
                    return result != 0 ? result : left.CompareTo(right);
                }
            );
            return rows;
        }

    #endregion

    }
}
