/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : XeriContextMenuItem.cs
수정일 : 2026-11-18

# 설명
공용 컨텍스트 메뉴의 명령·라벨·구분선 항목을 불변 데이터로 표현한다.
하위 메뉴는 항목 경로의 슬래시("부모/자식")로 만들며, 메뉴를 열 때 평평한 항목 목록을 하위 메뉴 트리로 풀어낸다.
항목은 목록에 들어간 순서대로 표시되고, 하위 메뉴와 구분선도 같은 규칙으로 놓인다.

# 특이사항, 제약사항
경로 조각은 비어 있을 수 없고 라벨에 슬래시를 문자로 쓸 수 없다.
같은 이름이 명령(또는 라벨)이면서 하위 메뉴 경로로도 쓰이면 오류로 처리한다.
풀어내는 단계에서 각 단계의 맨 앞·맨 뒤·연속 구분선은 제거하므로 여러 곳에서 만든 목록을 그대로 합칠 수 있다.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace inonego.Xeri.UI
{
    // ============================================================
    /// <summary>
    /// 공용 컨텍스트 메뉴의 항목 정보.
    /// </summary>
    // ============================================================
    public sealed class XeriContextMenuItem
    {

    #region 내부 타입

        // ============================================================
        /// <summary>
        /// 메뉴에 표시할 항목의 종류.
        /// </summary>
        // ============================================================
        internal enum ItemKind { Command, Label, Separator, Submenu }

    #endregion

    #region 필드 및 속성

        private const char pathSeparator = '/';
        private static readonly IReadOnlyList<XeriContextMenuItem> noChildren = Array.Empty<XeriContextMenuItem>();

        internal ItemKind Kind { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 항목이 놓일 하위 메뉴 경로. 루트 항목은 비어 있다.
        /// </summary>
        // ------------------------------------------------------------
        internal IReadOnlyList<string> ParentPath { get; }

        // ------------------------------------------------------------
        /// <summary>
        /// 메뉴 행에 표시할 이름. 구분선은 null이다.
        /// </summary>
        // ------------------------------------------------------------
        internal string Text { get; }
        internal string Shortcut { get; }
        internal bool Enabled { get; }
        internal bool IsChecked { get; }
        internal Texture2D Icon { get; }
        internal Action Execute { get; }
        internal IReadOnlyList<XeriContextMenuItem> Children => children ?? noChildren;
        private readonly List<XeriContextMenuItem> children;

    #endregion

    #region 항목 생성

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 항목의 종류와 표시·실행 정보를 보관한다.
        /// <br/> 항목은 팩토리와 트리 풀이에서만 만든다.
        /// </summary>
        // ------------------------------------------------------------
        private XeriContextMenuItem
        (
            ItemKind kind,
            IReadOnlyList<string> parentPath,
            string text,
            string shortcut = null,
            bool enabled = true,
            Action execute = null,
            bool isChecked = false,
            Texture2D icon = null
        )
        {
            Kind = kind;
            ParentPath = parentPath;
            Text = text;
            Shortcut = shortcut;
            Enabled = enabled;
            Execute = execute;
            IsChecked = isChecked;
            Icon = icon;
            children = kind == ItemKind.Submenu ? new List<XeriContextMenuItem>() : null;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 실행할 명령 항목을 만든다.
        /// <br/> 경로의 마지막 조각이 표시 이름이고 앞쪽 조각은 하위 메뉴다.
        /// <br/> 예: "키 자동 채우기/추가".
        /// </summary>
        // ------------------------------------------------------------
        public static XeriContextMenuItem Command
        (
            string path, Action execute, bool enabled = true,
            string shortcut = null, bool isChecked = false, Texture2D icon = null
        )
        {
            if (execute == null)
            {
                throw new ArgumentNullException(nameof(execute));
            }

            var segments = SplitPath(path, nameof(path));
            return new XeriContextMenuItem
            (
                ItemKind.Command, ParentOf(segments), segments[segments.Length - 1],
                shortcut, enabled, execute, isChecked, icon
            );
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 선택할 수 없는 텍스트 항목을 만든다. 경로 규칙은 Command와 같다.
        /// </summary>
        // ------------------------------------------------------------
        public static XeriContextMenuItem Label(string path)
        {
            var segments = SplitPath(path, nameof(path));
            return new XeriContextMenuItem
            (
                ItemKind.Label, ParentOf(segments), segments[segments.Length - 1], enabled: false
            );
        }

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 구분선 항목을 만든다.
        /// <br/> 경로를 생략하면 루트 메뉴에, 하위 메뉴 경로를 주면 그 하위 메뉴에 놓인다.
        /// <br/> 예: "키 자동 채우기".
        /// </summary>
        // ----------------------------------------------------------------------
        public static XeriContextMenuItem Separator(string parentPath = null)
        {
            var parents = string.IsNullOrEmpty(parentPath)
                ? Array.Empty<string>()
                : SplitPath(parentPath, nameof(parentPath));
            return new XeriContextMenuItem(ItemKind.Separator, parents, null);
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 경로를 슬래시로 나눈다. 비어 있는 경로나 조각은 허용하지 않는다.
        /// </summary>
        // ------------------------------------------------------------
        private static string[] SplitPath(string path, string argumentName)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("메뉴 경로는 비워둘 수 없습니다.", argumentName);
            }

            var segments = path.Split(pathSeparator);
            foreach (var segment in segments)
            {
                if (segment.Length == 0)
                {
                    throw new ArgumentException($"메뉴 경로 조각이 비어 있습니다: \"{path}\"", argumentName);
                }
            }

            return segments;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 경로 조각에서 마지막(표시 이름)을 뺀 하위 메뉴 경로를 반환한다.
        /// </summary>
        // ------------------------------------------------------------
        private static string[] ParentOf(string[] segments)
        {
            var parents = new string[segments.Length - 1];
            Array.Copy(segments, parents, parents.Length);
            return parents;
        }

    #endregion

    #region 하위 메뉴 트리

        // ----------------------------------------------------------------------
        /// <summary>
        /// <br/> 경로를 가진 평평한 항목 목록을 하위 메뉴 트리로 풀어낸다.
        /// <br/> 항목은 목록 순서대로 놓이고 하위 메뉴는 경로가 처음 나온 자리에 만들어진다.
        /// <br/> 입력 항목은 바꾸지 않으므로 같은 목록을 다시 열어도 된다.
        /// </summary>
        // ----------------------------------------------------------------------
        internal static IReadOnlyList<XeriContextMenuItem> Resolve(IEnumerable<XeriContextMenuItem> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            var root = new List<XeriContextMenuItem>();
            foreach (var item in items)
            {
                if (item == null)
                {
                    throw new ArgumentException("메뉴 항목은 null일 수 없습니다.", nameof(items));
                }

                // 부모 경로를 따라 하위 메뉴를 찾거나 첫 등장 위치에 만든다.
                var level = root;
                foreach (var segment in item.ParentPath)
                {
                    level = FindOrCreateSubmenu(level, segment).children;
                }

                if (item.Kind != ItemKind.Separator && Find(level, item.Text)?.Kind == ItemKind.Submenu)
                {
                    throw new InvalidOperationException
                    (
                        $"\"{item.Text}\"는 하위 메뉴 경로이면서 {item.Kind} 이름으로도 사용됩니다."
                    );
                }

                level.Add(item);
            }

            Normalize(root);
            return root;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 단계에서 이름이 같은 하위 메뉴를 찾고 없으면 현재 위치에 만든다.
        /// </summary>
        // ------------------------------------------------------------
        private static XeriContextMenuItem FindOrCreateSubmenu(List<XeriContextMenuItem> level, string text)
        {
            var existing = Find(level, text);
            if (existing == null)
            {
                var created = new XeriContextMenuItem(ItemKind.Submenu, Array.Empty<string>(), text);
                level.Add(created);
                return created;
            }

            if (existing.Kind != ItemKind.Submenu)
            {
                throw new InvalidOperationException
                (
                    $"\"{text}\"는 명령이면서 하위 메뉴 경로로도 사용됩니다."
                );
            }

            return existing;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// <br/> 단계에서 이름이 같은 첫 항목을 찾는다.
        /// <br/> 구분선은 이름이 없으므로 건너뛴다.
        /// </summary>
        // ------------------------------------------------------------
        private static XeriContextMenuItem Find(List<XeriContextMenuItem> level, string text)
        {
            foreach (var item in level)
            {
                if (item.Kind != ItemKind.Separator && string.Equals(item.Text, text, StringComparison.Ordinal))
                {
                    return item;
                }
            }

            return null;
        }

        // ------------------------------------------------------------
        /// <summary>
        /// 각 단계의 맨 앞·맨 뒤·연속 구분선을 제거한다.
        /// </summary>
        // ------------------------------------------------------------
        private static void Normalize(List<XeriContextMenuItem> level)
        {
            // 앞쪽부터 훑으며 맨 앞이나 직전이 구분선인 구분선을 건너뛴다.
            var write = 0;
            for (var read = 0; read < level.Count; read++)
            {
                var item = level[read];
                if (item.Kind == ItemKind.Separator
                    && (write == 0 || level[write - 1].Kind == ItemKind.Separator))
                {
                    continue;
                }

                level[write++] = item;
            }

            // 맨 뒤에 남은 구분선을 걷어낸다.
            while (write > 0 && level[write - 1].Kind == ItemKind.Separator)
            {
                write--;
            }

            level.RemoveRange(write, level.Count - write);

            foreach (var item in level)
            {
                if (item.Kind == ItemKind.Submenu)
                {
                    Normalize(item.children);
                }
            }
        }

    #endregion

    }
}
