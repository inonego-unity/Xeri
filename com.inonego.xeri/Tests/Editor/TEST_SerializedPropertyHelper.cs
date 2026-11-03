/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_SerializedPropertyHelper.cs
수정일 : 2026-11-03

# 설명
SerializedProperty 경로의 방향별 포함 관계, 직계 부모·자식 및 경계 규칙을 검증한다.

# 테스트 구성
 P: 상위·하위·동일 경로의 대칭성
 D: 직계 자식과 Unity 배열 경로
========================================================================= BLOCK_HEADER_END */

using NUnit;
using NUnit.Framework;

using inonego;
using inonego.Xeri;

namespace inonego.Xeri.TEST
{
    // ============================================================
    /// <summary>
    /// Unity 직렬화 경로 관계 API의 방향성과 경계를 검증한다.
    /// </summary>
    // ============================================================
    public class TEST_SerializedPropertyHelper
    {

    #region P-1: 경로 포함 관계

        [Test]
        public void TEST_SerializedPropertyHelper_동일경로는_포함관계만성립()
        {
            const string path = "inventory.items";

            Assert.IsTrue(SerializedPropertyHelper.IsSameOrDescendantPath(path, path));
            Assert.IsTrue(SerializedPropertyHelper.IsSameOrAncestorPath(path, path));
            Assert.IsTrue(SerializedPropertyHelper.IsRelatedPath(path, path));

            Assert.IsFalse(SerializedPropertyHelper.IsDescendantPath(path, path));
            Assert.IsFalse(SerializedPropertyHelper.IsAncestorPath(path, path));
            Assert.IsFalse(SerializedPropertyHelper.IsDirectChildPath(path, path));
            Assert.IsFalse(SerializedPropertyHelper.IsDirectParentPath(path, path));
        }

        [Test]
        public void TEST_SerializedPropertyHelper_상위하위경로는_방향별로대칭()
        {
            const string ancestor = "inventory";
            const string descendant = "inventory.items.Array.data[0].value";

            Assert.IsTrue(SerializedPropertyHelper.IsDescendantPath(descendant, ancestor));
            Assert.IsTrue(SerializedPropertyHelper.IsAncestorPath(ancestor, descendant));
            Assert.IsTrue(SerializedPropertyHelper.IsSameOrDescendantPath(descendant, ancestor));
            Assert.IsTrue(SerializedPropertyHelper.IsSameOrAncestorPath(ancestor, descendant));
            Assert.IsTrue(SerializedPropertyHelper.IsRelatedPath(descendant, ancestor));
            Assert.IsTrue(SerializedPropertyHelper.IsRelatedPath(ancestor, descendant));

            Assert.IsFalse(SerializedPropertyHelper.IsAncestorPath(descendant, ancestor));
            Assert.IsFalse(SerializedPropertyHelper.IsDescendantPath(ancestor, descendant));
        }

        [Test]
        public void TEST_SerializedPropertyHelper_빈경로와유사이름은_관계없음()
        {
            Assert.IsFalse(SerializedPropertyHelper.IsRelatedPath(null, null));
            Assert.IsFalse(SerializedPropertyHelper.IsSameOrDescendantPath("", ""));
            Assert.IsFalse(SerializedPropertyHelper.IsSameOrAncestorPath("", ""));
            Assert.IsFalse(SerializedPropertyHelper.IsDescendantPath(null, "inventory"));
            Assert.IsFalse(SerializedPropertyHelper.IsAncestorPath("inventory", null));
            Assert.IsFalse(SerializedPropertyHelper.IsDirectChildPath("", "inventory"));
            Assert.IsFalse(SerializedPropertyHelper.IsDirectParentPath("inventory", ""));

            Assert.IsFalse(SerializedPropertyHelper.IsRelatedPath("items", "itemsCount"));
            Assert.IsFalse(SerializedPropertyHelper.IsRelatedPath("items", "itemsArray.data[0]"));
            Assert.IsFalse(SerializedPropertyHelper.IsRelatedPath("left.items", "right.items"));
        }

    #endregion

    #region D-1: 직계 부모와 자식

        [Test]
        public void TEST_SerializedPropertyHelper_일반경로의직계와중첩자식을구별()
        {
            Assert.IsTrue(SerializedPropertyHelper.IsDirectChildPath("inventory.items", "inventory"));
            Assert.IsTrue(SerializedPropertyHelper.IsDirectParentPath("inventory", "inventory.items"));

            Assert.IsFalse(SerializedPropertyHelper.IsDirectChildPath("inventory.items.value", "inventory"));
            Assert.IsFalse(SerializedPropertyHelper.IsDirectParentPath("inventory", "inventory.items.value"));
            Assert.IsFalse(SerializedPropertyHelper.IsDirectChildPath("inventoryX.items", "inventory"));
        }

        [Test]
        public void TEST_SerializedPropertyHelper_배열요소와크기는_직계자식으로판정()
        {
            const string parent = "inventory.items";
            const string element = "inventory.items.Array.data[12]";

            Assert.IsTrue(SerializedPropertyHelper.IsDirectChildPath(element, parent));
            Assert.IsTrue(SerializedPropertyHelper.IsDirectParentPath(parent, element));
            Assert.IsFalse(SerializedPropertyHelper.IsDirectChildPath(parent, element));
            Assert.IsFalse(SerializedPropertyHelper.IsDirectParentPath(element, parent));
            Assert.IsTrue(SerializedPropertyHelper.IsDirectChildPath("inventory.items.Array.size", parent));
            Assert.IsTrue(SerializedPropertyHelper.IsDirectParentPath(parent, "inventory.items.Array.size"));

            Assert.IsFalse(SerializedPropertyHelper.IsDirectChildPath(element + ".value", parent));
            Assert.IsTrue(SerializedPropertyHelper.IsDirectChildPath(element + ".value", element));
            Assert.IsFalse(SerializedPropertyHelper.IsDirectChildPath("inventory.items.Array.data[x]", parent));
            Assert.IsFalse(SerializedPropertyHelper.IsDirectChildPath("inventory.items.Array.data[]", parent));
            Assert.IsFalse(SerializedPropertyHelper.IsDirectChildPath("inventory.items.Array.data[-1]", parent));
            Assert.IsFalse(SerializedPropertyHelper.IsDirectChildPath("inventory.items.Array.data[1].value", parent));
            Assert.IsFalse(SerializedPropertyHelper.IsDirectChildPath("inventory.items.Array.random", parent));
        }

    #endregion

    }
}
