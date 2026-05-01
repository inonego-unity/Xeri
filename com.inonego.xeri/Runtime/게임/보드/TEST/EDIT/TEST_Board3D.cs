/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_Board3D.cs
수정일 : 2026-05-01

# 설명
Board3D 시스템의 핵심 기능 테스트. Edit Mode.
========================================================================= BLOCK_HEADER_END */

using System;
using System.Collections;
using System.Collections.Generic;

using NUnit.Framework;

using UnityEngine;

using inonego.Xeri.Game;

// ============================================================
/// <summary>
/// Board3D 시스템의 핵심 기능 테스트 클래스입니다.
/// </summary>
// ============================================================
public class TEST_Board3D
{
    // ------------------------------------------------------------
    /// <summary>
    /// 테스트용 기물입니다.
    /// </summary>
    // ------------------------------------------------------------
    public class TestPiece
    {
        public string Name;
        public TestPiece() {}
        public TestPiece(string name) { Name = name; }
        public override string ToString() => Name;
    }

    // ------------------------------------------------------------
    /// <summary>
    /// 테스트용 공간입니다.
    /// </summary>
    // ------------------------------------------------------------
    [Serializable]
    public class TestSpace : BoardSpace<TestPiece>
    {
        public TestSpace() {}
    }

#region 크기 및 경계

    [Test]
    public void Board3D_01_크기_경계_테스트()
    {
        // ------------------------------------------------------------
        // 테스트 준비
        // ------------------------------------------------------------
        var board = new Board3D<int, TestSpace, TestPiece>(3, 2, 4);

        // ------------------------------------------------------------
        // 크기 확인
        // ------------------------------------------------------------
        Assert.That(board.Width, Is.EqualTo(3));
        Assert.That(board.Height, Is.EqualTo(2));
        Assert.That(board.Depth, Is.EqualTo(4));
        Assert.That(board.Size, Is.EqualTo(new Vector3Int(3, 2, 4)));

        // ------------------------------------------------------------
        // 경계 확인
        // ------------------------------------------------------------
        Assert.That(board[new Vector3Int(+0, +0, +0)], Is.Not.Null);
        Assert.That(board[new Vector3Int(+2, +1, +3)], Is.Not.Null);
        Assert.That(board[new Vector3Int(-1, +0, +0)], Is.Null);
        Assert.That(board[new Vector3Int(+3, +1, +0)], Is.Null);
        Assert.That(board[new Vector3Int(+2, +2, +0)], Is.Null);
        Assert.That(board[new Vector3Int(+0, +0, +4)], Is.Null);
    }

#endregion

#region 배치 및 제거

    [Test]
    public void Board3D_02_배치_및_인덱서_테스트()
    {
        // ------------------------------------------------------------
        // 테스트 준비
        // ------------------------------------------------------------
        var board = new Board3D<int, TestSpace, TestPiece>(2, 2, 2);
        var a = new TestPiece("A");
        var b = new TestPiece("B");

        var p0 = new Vector3Int(0, 0, 0);
        var p1 = new Vector3Int(1, 1, 1);

        // ------------------------------------------------------------
        // Vector와 Index로 배치 및 인덱서 확인
        // ------------------------------------------------------------
        board.Place(p0, 0, a);
        Assert.That(board[p0, 0], Is.EqualTo(a));
        Assert.That(board[p0][0], Is.EqualTo(a));

        // ------------------------------------------------------------
        // 같은 Vector, 같은 Index에 다른 기물 배치 시 예외
        // ------------------------------------------------------------
        Assert.Throws<InvalidOperationException>(() => board.Place(p0, 0, b));

        // ------------------------------------------------------------
        // 같은 Vector, 다른 Index에는 배치 가능
        // ------------------------------------------------------------
        board.Place(p0, 1, b);
        Assert.That(board[p0, 0], Is.EqualTo(a));
        Assert.That(board[p0, 1], Is.EqualTo(b));

        // ------------------------------------------------------------
        // 다른 Vector에 배치
        // ------------------------------------------------------------
        var c = new TestPiece("C");
        board.Place(p1, 0, c);
        Assert.That(board[p1, 0], Is.EqualTo(c));
        Assert.That(board[p1][0], Is.EqualTo(c));
    }

    [Test]
    public void Board3D_03_단일_Index_제거_테스트()
    {
        // ------------------------------------------------------------
        // 테스트 준비
        // ------------------------------------------------------------
        var board = new Board3D<int, TestSpace, TestPiece>(2, 2, 2);
        var a = new TestPiece("A");
        var b = new TestPiece("B");
        var p = new Vector3Int(1, 0, 1);

        // ------------------------------------------------------------
        // 여러 Index 배치
        // ------------------------------------------------------------
        board.Place(p, 0, a);
        board.Place(p, 1, b);

        // ------------------------------------------------------------
        // 특정 Index만 제거 (Vector, Index)
        // ------------------------------------------------------------
        board.Remove(p, 0);
        Assert.That(board[p], Is.Not.Null);  // 공간은 유지
        Assert.That(board[p, 0], Is.Null);   // Index 0 제거됨
        Assert.That(board[p, 1], Is.EqualTo(b));  // Index 1은 유지

        // ------------------------------------------------------------
        // Placeable 객체로 제거
        // ------------------------------------------------------------
        board.Remove(b);
        Assert.That(board[p, 1], Is.Null);
    }

    [Test]
    public void Board3D_04_Vector_전체_제거_테스트()
    {
        // ------------------------------------------------------------
        // 테스트 준비
        // ------------------------------------------------------------
        var board = new Board3D<int, TestSpace, TestPiece>(2, 2, 2);
        var p = new Vector3Int(0, 0, 0);

        // ------------------------------------------------------------
        // 같은 Vector에 여러 Index로 배치
        // ------------------------------------------------------------
        var pieces = new[]
        {
            new TestPiece("P0"),
            new TestPiece("P1"),
            new TestPiece("P2"),
            new TestPiece("P3"),
        };

        for (int i = 0; i < pieces.Length; i++)
        {
            board.Place(p, i, pieces[i]);
        }

        for (int i = 0; i < pieces.Length; i++)
        {
            Assert.That(board[p, i], Is.EqualTo(pieces[i]));
        }

        // ------------------------------------------------------------
        // Vector 전체 제거 (모든 Index 한 번에 제거)
        // ------------------------------------------------------------
        board.Remove(p);

        Assert.That(board[p], Is.Not.Null);  // 공간은 유지
        for (int i = 0; i < pieces.Length; i++)
        {
            Assert.That(board[p, i], Is.Null);  // 모든 Index 제거됨
        }
    }

#endregion

#region 공간 관리

    [Test]
    public void Board3D_05_공간_추가_제거_테스트()
    {
        // ------------------------------------------------------------
        // 테스트 준비
        // ------------------------------------------------------------
        var board = new Board3D<int, TestSpace, TestPiece>(1, 1, 1);
        var pt = new Vector3Int(0, 0, 0);

        Vector3Int? addEventVector = null;
        Vector3Int? removeEventVector = null;

        board.OnAddSpace += (v) => { addEventVector = v; };
        board.OnRemoveSpace += (v) => { removeEventVector = v; };

        // ------------------------------------------------------------
        // 기본 공간 확인
        // ------------------------------------------------------------
        Assert.That(board[pt], Is.Not.Null);

        // ------------------------------------------------------------
        // 공간 제거 및 이벤트 확인
        // ------------------------------------------------------------
        board.RemoveSpace(pt);
        Assert.That(board[pt], Is.Null);
        Assert.That(removeEventVector, Is.EqualTo(pt));

        // ------------------------------------------------------------
        // 공간 다시 추가 및 이벤트 확인
        // ------------------------------------------------------------
        board.AddSpace(pt);
        Assert.That(board[pt], Is.Not.Null);
        Assert.That(addEventVector, Is.EqualTo(pt));

        // ------------------------------------------------------------
        // 객체 배치 후 공간 제거 시 객체도 제거됨
        // ------------------------------------------------------------
        var pieces = new[] { new TestPiece("A"), new TestPiece("B"), new TestPiece("C") };
        for (int i = 0; i < pieces.Length; i++)
        {
            board.Place(pt, i, pieces[i]);
        }

        board.RemoveSpace(pt);
        Assert.That(board[pt], Is.Null);
    }

#endregion

#region 이벤트

    [Test]
    public void Board3D_06_OnPlace_이벤트_테스트()
    {
        // ------------------------------------------------------------
        // 테스트 준비
        // ------------------------------------------------------------
        var board = new Board3D<int, TestSpace, TestPiece>(2, 2, 2);
        var piece = new TestPiece("E");
        var p = new Vector3Int(0, 0, 0);

        Vector3Int? eventVector = null;
        int? eventIndex = null;
        TestPiece eventPiece = null;
        int eventCount = 0;

        board.OnPlace += (v, i, pl) =>
        {
            eventVector = v;
            eventIndex = i;
            eventPiece = pl;
            eventCount++;
        };

        // ------------------------------------------------------------
        // 배치 시 이벤트 발생 확인 (Vector, Index, Placeable)
        // ------------------------------------------------------------
        board.Place(p, 5, piece);

        Assert.That(eventCount, Is.EqualTo(1));
        Assert.That(eventVector, Is.EqualTo(p));
        Assert.That(eventIndex, Is.EqualTo(5));
        Assert.That(eventPiece, Is.EqualTo(piece));
    }

    [Test]
    public void Board3D_07_OnRemove_이벤트_테스트()
    {
        // ------------------------------------------------------------
        // 테스트 준비
        // ------------------------------------------------------------
        var board = new Board3D<int, TestSpace, TestPiece>(2, 2, 2);
        var piece = new TestPiece("R");
        var p = new Vector3Int(1, 0, 1);

        Vector3Int? eventVector = null;
        int? eventIndex = null;
        TestPiece eventPiece = null;
        int eventCount = 0;

        board.OnRemove += (v, i, pl) =>
        {
            eventVector = v;
            eventIndex = i;
            eventPiece = pl;
            eventCount++;
        };

        // ------------------------------------------------------------
        // 제거 시 이벤트 발생 확인 (Vector, Index, Placeable)
        // ------------------------------------------------------------
        board.Place(p, 3, piece);
        board.Remove(p, 3);

        Assert.That(eventCount, Is.EqualTo(1));
        Assert.That(eventVector, Is.EqualTo(p));
        Assert.That(eventIndex, Is.EqualTo(3));
        Assert.That(eventPiece, Is.EqualTo(piece));

        // ------------------------------------------------------------
        // Remove(Vector)로 여러 객체 제거 시 이벤트 여러 번 발생
        // ------------------------------------------------------------
        eventCount = 0;
        board.Place(p, 0, new TestPiece("A"));
        board.Place(p, 1, new TestPiece("B"));
        board.Place(p, 2, new TestPiece("C"));

        board.Remove(p);  // 모든 Index 제거
        Assert.That(eventCount, Is.EqualTo(3));  // 3번 발생
    }

    [Test]
    public void Board3D_08_invokeEvent_파라미터_테스트()
    {
        // ------------------------------------------------------------
        // 테스트 준비
        // ------------------------------------------------------------
        var board = new Board3D<int, TestSpace, TestPiece>(2, 2, 2);
        var piece = new TestPiece("Q");
        var p = new Vector3Int(0, 0, 0);

        int placeEventCount = 0;
        int removeEventCount = 0;

        board.OnPlace += (v, i, pl) => placeEventCount++;
        board.OnRemove += (v, i, pl) => removeEventCount++;

        // ------------------------------------------------------------
        // invokeEvent = false로 배치 및 제거 시 이벤트 미발생
        // ------------------------------------------------------------
        board.Place(p, 0, piece, invokeEvent: false);
        Assert.That(placeEventCount, Is.EqualTo(0));
        Assert.That(board[p, 0], Is.EqualTo(piece));

        board.Remove(p, 0, invokeEvent: false);
        Assert.That(removeEventCount, Is.EqualTo(0));
        Assert.That(board[p, 0], Is.Null);

        // ------------------------------------------------------------
        // invokeEvent = true (기본값)로 배치 및 제거 시 이벤트 발생
        // ------------------------------------------------------------
        board.Place(p, 0, piece);
        Assert.That(placeEventCount, Is.EqualTo(1));

        board.Remove(p, 0);
        Assert.That(removeEventCount, Is.EqualTo(1));
    }

#endregion

#region 예외

    [Test]
    public void Board3D_09_범위밖_배치시_예외()
    {
        // ------------------------------------------------------------
        // 테스트 준비
        // ------------------------------------------------------------
        var board = new Board3D<int, TestSpace, TestPiece>(2, 2, 2);
        var piece = new TestPiece("O");
        var point = new Vector3Int(5, 5, 5);

        // ------------------------------------------------------------
        // 범위 밖 배치 시 예외
        // ------------------------------------------------------------
        Assert.Throws<InvalidOperationException>(() => board.Place(point, 0, piece));
    }

    [Test]
    public void Board3D_10_중복_AddSpace시_예외()
    {
        // ------------------------------------------------------------
        // 테스트 준비
        // ------------------------------------------------------------
        var board = new Board3D<int, TestSpace, TestPiece>(1, 1, 1);
        var point = new Vector3Int(0, 0, 0);

        // ------------------------------------------------------------
        // 중복 공간 추가 시 예외
        // ------------------------------------------------------------
        Assert.Throws<InvalidOperationException>(() => board.AddSpace(point));
    }

#endregion

#region 종합 시나리오

    [Test]
    public void Board3D_11_복합_시나리오_테스트()
    {
        // ------------------------------------------------------------
        // 테스트 준비
        // ------------------------------------------------------------
        var board = new Board3D<int, TestSpace, TestPiece>(3, 3, 3);

        // ------------------------------------------------------------
        // 여러 Vector에 여러 Index로 배치
        // ------------------------------------------------------------
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                for (int z = 0; z < 3; z++)
                {
                    var vector = new Vector3Int(x, y, z);
                    for (int index = 0; index < 2; index++)
                    {
                        var piece = new TestPiece($"[{x},{y},{z}][{index}]");
                        board.Place(vector, index, piece);
                    }
                }
            }
        }

        // 모든 배치 확인
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                for (int z = 0; z < 3; z++)
                {
                    var vector = new Vector3Int(x, y, z);
                    for (int index = 0; index < 2; index++)
                    {
                        Assert.That(board[vector, index], Is.Not.Null);
                        Assert.That(board[vector, index].Name, Is.EqualTo($"[{x},{y},{z}][{index}]"));
                    }
                }
            }
        }

        // ------------------------------------------------------------
        // 특정 Vector의 모든 Index 제거
        // ------------------------------------------------------------
        var targetVector = new Vector3Int(1, 1, 1);
        board.Remove(targetVector);

        Assert.That(board[targetVector], Is.Not.Null);  // 공간 유지
        Assert.That(board[targetVector, 0], Is.Null);
        Assert.That(board[targetVector, 1], Is.Null);

        // 다른 Vector는 영향 없음
        Assert.That(board[new Vector3Int(0, 0, 0), 0], Is.Not.Null);
        Assert.That(board[new Vector3Int(2, 2, 2), 1], Is.Not.Null);
    }

#endregion

}
