/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_SerializedPropertyClipboard.cs
수정일 : 2026-10-31

# 설명
직렬화 프로퍼티 클립보드의 값·객체 참조·배열 복원 및 관리 참조 링크 계약을 검증한다.

# 테스트 구성
 R: Unity 객체 참조 보존
 S: 목록 복사, 관리 참조와 클립보드 상태
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using NUnit;
using NUnit.Framework;

using inonego;
using inonego.Xeri;
using inonego.Xeri.Serializable;
using inonego.Xeri.Serializable.Editor;

namespace inonego.Xeri.TEST.Serializable
{
    // ============================================================
    /// <summary>
    /// 직렬화 값의 복원과 관리 참조 링크의 안전 조건을 검증한다.
    /// </summary>
    // ============================================================
    public class TEST_SerializedPropertyClipboard
    {

    #region 타입과 상태

        [Serializable]
        private sealed class Row
        {
            public string Label;
            public int Count;
            public Texture2D Icon;
            public GameObject SceneObject;
        }

        private sealed class Target : ScriptableObject
        {
            public Row Entry = new();
            public List<Row> Entries = new();
            [SerializeReference] public Row Reference = new();
            [SerializeReference] public Row LinkedReference;
        }

        private Target source;
        private Target destination;
        private Texture2D texture;
        private GameObject gameObject;

    #endregion

    #region 픽스처

        [SetUp]
        public void SetUp()
        {
            SerializedPropertyClipboard.Clear();
            source = ScriptableObject.CreateInstance<Target>();
            destination = ScriptableObject.CreateInstance<Target>();
            texture = new Texture2D(2, 2);
            gameObject = new GameObject("프로퍼티 복사 테스트");
            source.Entry = new Row
            {
                Label = "원본",
                Count = 42,
                Icon = texture,
                SceneObject = gameObject,
            };
            destination.Entry = new Row
            {
                Label = "대상",
                Count = -1,
            };
            source.Reference = new Row
            {
                Label = "관리 참조 원본",
                Count = 31,
            };
            destination.Reference = new Row
            {
                Label = "관리 참조 대상",
                Count = 0,
            };
        }

        [TearDown]
        public void TearDown()
        {
            SerializedPropertyClipboard.Clear();
            if (source != null)
            {
                UnityEngine.Object.DestroyImmediate(source);
            }

            if (destination != null)
            {
                UnityEngine.Object.DestroyImmediate(destination);
            }

            if (texture != null)
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }

            if (gameObject != null)
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }

    #endregion

    #region 헬퍼

        private void Copy(string fromPath, string toPath)
        {
            using var sourceObject = new SerializedObject(source);
            using var destinationObject = new SerializedObject(destination);
            var from = sourceObject.FindProperty(fromPath);
            var to = destinationObject.FindProperty(toPath);

            Assert.IsNotNull(from);
            Assert.IsNotNull(to);
            Assert.IsTrue(SerializedPropertyClipboard.TryCopyValue(from, out var reason), reason);
            Assert.IsTrue(SerializedPropertyClipboard.CanPasteValue(to, out reason), reason);
            Assert.IsTrue(SerializedPropertyClipboard.TryPasteValue(to, out reason), reason);
            destinationObject.ApplyModifiedProperties();
        }

    #endregion

    #region R-1: 객체 참조 보존

        [Test]
        public void TEST_SerializedPropertyClipboard_중첩객체_값과참조동시복원()
        {
            Copy("Entry", "Entry");

            Assert.AreEqual("원본", destination.Entry.Label);
            Assert.AreEqual(42, destination.Entry.Count);
            Assert.AreSame(texture, destination.Entry.Icon);
            Assert.AreSame(gameObject, destination.Entry.SceneObject);
        }

        [Test]
        public void TEST_SerializedPropertyClipboard_객체참조만복사해도_동일인스턴스를가리킴()
        {
            Copy("Entry.Icon", "Entry.Icon");

            Assert.AreSame(texture, destination.Entry.Icon);
            Assert.AreEqual(-1, destination.Entry.Count);
        }

        [Test]
        public void TEST_SerializedPropertyClipboard_null참조로_기존참조초기화()
        {
            source.Entry.Icon = null;
            source.Entry.SceneObject = null;
            destination.Entry.Icon = texture;
            destination.Entry.SceneObject = gameObject;

            Copy("Entry", "Entry");

            Assert.IsNull(destination.Entry.Icon);
            Assert.IsNull(destination.Entry.SceneObject);
            Assert.AreEqual(42, destination.Entry.Count);
        }

    #endregion

    #region S-1: 목록과 호환성

        [Test]
        public void TEST_SerializedPropertyClipboard_목록의원소와null참조복원()
        {
            source.Entries = new List<Row>
            {
                new()
                {
                    Label = "첫째",
                    Count = 7,
                    Icon = texture,
                    SceneObject = gameObject,
                },
                new()
                {
                    Label = "둘째",
                    Count = 8,
                },
            };
            destination.Entries = new List<Row>
            {
                new()
                {
                    Label = "기존",
                },
            };

            Copy("Entries", "Entries");

            Assert.AreEqual(2, destination.Entries.Count);
            Assert.AreEqual(7, destination.Entries[0].Count);
            Assert.AreSame(texture, destination.Entries[0].Icon);
            Assert.AreSame(gameObject, destination.Entries[0].SceneObject);
            Assert.AreEqual("둘째", destination.Entries[1].Label);
            Assert.IsNull(destination.Entries[1].Icon);
            Assert.IsNull(destination.Entries[1].SceneObject);
        }

        [Test]
        public void TEST_SerializedPropertyClipboard_타입이맞지않으면_붙여넣기를거부()
        {
            using var sourceObject = new SerializedObject(source);
            using var destinationObject = new SerializedObject(destination);
            var from = sourceObject.FindProperty("Entry.Icon");
            var to = destinationObject.FindProperty("Entry.Count");

            Assert.IsTrue(SerializedPropertyClipboard.TryCopyValue(from, out var reason), reason);
            Assert.IsFalse(SerializedPropertyClipboard.CanPasteValue(to, out reason));
            Assert.IsFalse(SerializedPropertyClipboard.TryPasteValue(to, out reason));
            destinationObject.ApplyModifiedProperties();
            Assert.AreEqual(-1, destination.Entry.Count);
        }

    #endregion

    #region S-2: 스냅샷 비교와 통합 복사 상태

        [Test]
        public void TEST_SerializedPropertyClipboard_문자열값의_동일성과변경을구별()
        {
            using var owner = new SerializedObject(source);
            var property = owner.FindProperty("Entry.Label");
            Assert.IsTrue
            (
                SerializedPropertySnapshot.TryCreate(property, out var snapshot, out var reason),
                reason
            );
            Assert.IsTrue(snapshot.Matches(property));

            property.stringValue = "변경";
            Assert.IsFalse(snapshot.Matches(property));
        }

        [Test]
        public void TEST_SerializedPropertyClipboard_관리참조값은_다른루트에_독립복원()
        {
            using var owner = new SerializedObject(source);
            using var receiver = new SerializedObject(destination);
            var from = owner.FindProperty("Reference");
            var to = receiver.FindProperty("Reference");

            Assert.IsTrue(SerializedPropertyClipboard.TryCopyValue(from, out var reason), reason);
            Assert.IsTrue
            (
                SerializedPropertyClipboard.TryPasteValue(to, out reason, typeof(Row)),
                reason
            );
            receiver.ApplyModifiedProperties();

            Assert.AreEqual("관리 참조 원본", destination.Reference.Label);
            Assert.AreEqual(31, destination.Reference.Count);
            Assert.AreNotSame(source.Reference, destination.Reference);
        }

        [Test]
        public void TEST_SerializedPropertyClipboard_링크는_같은루트에만_붙여넣고_값복사로교체()
        {
            using var owner = new SerializedObject(source);
            using var receiver = new SerializedObject(destination);
            var from = owner.FindProperty("Reference");
            var to = owner.FindProperty("LinkedReference");

            Assert.IsTrue(SerializedPropertyClipboard.TryCopyLink(from, out var reason), reason);
            Assert.IsFalse
            (
                SerializedPropertyClipboard.CanPasteLink
                (
                    receiver.FindProperty("Reference"), typeof(Row), out _
                )
            );
            Assert.IsFalse(SerializedPropertyClipboard.CanPasteValue(to, out _));
            Assert.IsTrue
            (
                SerializedPropertyClipboard.TryPasteLink(to, typeof(Row), out reason),
                reason
            );
            owner.ApplyModifiedProperties();
            Assert.AreSame(source.Reference, source.LinkedReference);

            Assert.IsTrue
            (
                SerializedPropertyClipboard.TryCopyValue(owner.FindProperty("Entry"), out reason),
                reason
            );
            Assert.IsFalse
            (
                SerializedPropertyClipboard.CanPasteLink
                (
                    owner.FindProperty("LinkedReference"), typeof(Row), out _
                )
            );
        }

    #endregion

    }
}
