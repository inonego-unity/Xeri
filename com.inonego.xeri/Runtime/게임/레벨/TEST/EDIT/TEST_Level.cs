/* BLOCK_HEADER_BEGIN =======================================================================
파일명 : TEST_Level.cs
수정일 : 2026-05-01

# 설명
Level 및 LevelxEXP 클래스에 대한 Edit Mode 테스트.
Level: 8개, LevelxEXP: 14개 (총 22개 케이스).
========================================================================= BLOCK_HEADER_END */

using System;

using NUnit.Framework;

using UnityEngine;

using inonego.Xeri.Game;

// ============================================================================
/// <summary>
/// Level 시스템의 종합적인 테스트 클래스입니다.
/// </summary>
// ============================================================================
public class TEST_Level
{

#region Level 클래스 테스트

    // ------------------------------------------------------------
    /// <summary>
    /// Level 클래스의 기본 생성 및 초기값을 테스트합니다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void Level_01_기본_생성_테스트()
    {
        // Arrange & Act
        var level = new Level(10);

        // Assert
        Assert.AreEqual(0, level.Value, "초기 레벨은 0이어야 합니다");
        Assert.AreEqual(0, level.Min, "최소 레벨은 0이어야 합니다");
        Assert.AreEqual(10, level.Max, "최대 레벨은 설정값과 같아야 합니다");
        Assert.AreEqual(10, level.FullMax, "FullMax는 설정값과 같아야 합니다");
        Assert.AreEqual(10, level.LimitMax, "LimitMax는 초기에 FullMax와 같아야 합니다");
        Assert.IsTrue(level.CanLevelUp, "초기에는 레벨업이 가능해야 합니다");
        Assert.IsFalse(level.BlockLevelUp, "초기에는 레벨업 블록이 해제되어야 합니다");
    }

    // ------------------------------------------------------------
    /// <summary>
    /// Level 클래스의 레벨업 기능을 테스트합니다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void Level_02_레벨업_테스트()
    {
        // Arrange
        var level = new Level(5);
        bool eventFired = false;
        int eventLevel = -1;

        level.OnLevelUp += (sender, e) => {
            eventFired = true;
            eventLevel = e.Level;
        };

        // Act
        level.LevelUp();

        // Assert
        Assert.AreEqual(1, level.Value, "레벨업 후 레벨이 1 증가해야 합니다");
        Assert.IsTrue(eventFired, "레벨업 이벤트가 발생해야 합니다");
        Assert.AreEqual(1, eventLevel, "이벤트에서 올바른 레벨을 전달해야 합니다");
    }

    // ------------------------------------------------------------
    /// <summary>
    /// Level 클래스의 다중 레벨업 기능을 테스트합니다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void Level_03_다중_레벨업_테스트()
    {
        // Arrange
        var level = new Level(10);
        int eventCount = 0;

        level.OnLevelUp += (sender, e) => eventCount++;

        // Act
        level.LevelUp(3);

        // Assert
        Assert.AreEqual(3, level.Value, "3번 레벨업하면 레벨 3이 되어야 합니다");
        Assert.AreEqual(3, eventCount, "3번의 레벨업 이벤트가 발생해야 합니다");
    }

    // ------------------------------------------------------------
    /// <summary>
    /// Level 클래스의 최대 레벨 제한을 테스트합니다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void Level_04_최대레벨_제한_테스트()
    {
        // Arrange
        var level = new Level(3);
        level.Value = 3; // 최대 레벨로 설정
        bool eventFired = false;

        level.OnLevelUp += (sender, e) => eventFired = true;

        // Act
        level.LevelUp(); // 최대 레벨에서 레벨업 시도

        // Assert
        Assert.AreEqual(3, level.Value, "최대 레벨에서는 레벨이 증가하지 않아야 합니다");
        Assert.IsFalse(level.CanLevelUp, "최대 레벨에서는 레벨업이 불가능해야 합니다");
        Assert.IsFalse(eventFired, "최대 레벨에서는 레벨업 이벤트가 발생하지 않아야 합니다");
    }

    // ------------------------------------------------------------
    /// <summary>
    /// Level 클래스의 레벨업 차단 기능을 테스트합니다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void Level_05_레벨업_차단_테스트()
    {
        // Arrange
        var level = new Level(5);
        level.BlockLevelUp = true;
        bool eventFired = false;

        level.OnLevelUp += (sender, e) => eventFired = true;

        // Act
        level.LevelUp();

        // Assert
        Assert.AreEqual(0, level.Value, "레벨업이 차단되면 레벨이 증가하지 않아야 합니다");
        Assert.IsFalse(level.CanLevelUp, "레벨업이 차단되면 CanLevelUp이 false여야 합니다");
        Assert.IsFalse(eventFired, "레벨업이 차단되면 레벨업 이벤트가 발생하지 않아야 합니다");
    }

    // ------------------------------------------------------------
    /// <summary>
    /// Level 클래스의 LimitMax 설정 기능을 테스트합니다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void Level_06_LimitMax_설정_테스트()
    {
        // Arrange
        var level = new Level(10);

        // Act
        level.LimitMax = 5;

        // Assert
        Assert.AreEqual(5, level.Max, "Max는 LimitMax와 FullMax 중 작은 값이어야 합니다");
        Assert.AreEqual(5, level.LimitMax, "LimitMax가 올바르게 설정되어야 합니다");
    }

    // ------------------------------------------------------------
    /// <summary>
    /// Level 생성자에 음수 최대 레벨을 전달할 때 예외를 테스트합니다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void Level_07_음수_최대레벨_예외테스트()
    {
        // Act & Assert
        Assert.Throws<Level.InvalidMaxLevelException>(() => new Level(-1));
    }

    // ------------------------------------------------------------
    /// <summary>
    /// Level 클래스의 JSON 직렬화/역직렬화를 테스트합니다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void Level_08_JSON_직렬화_테스트()
    {
        // Arrange - 레벨업된 상태
        var originalLevel = new Level(10);
        originalLevel.LevelUp(3); // 레벨 3으로 설정
        originalLevel.LimitMax = 5;

        // Act - 직렬화/역직렬화
        string json = JsonUtility.ToJson(originalLevel);
        var deserializedLevel = JsonUtility.FromJson<Level>(json);

        // Assert - 상태 복원 확인
        Assert.AreEqual(originalLevel.Value, deserializedLevel.Value, "레벨 값이 올바르게 복원되어야 합니다");
        Assert.AreEqual(originalLevel.Min, deserializedLevel.Min, "최소 레벨이 올바르게 복원되어야 합니다");
        Assert.AreEqual(originalLevel.Max, deserializedLevel.Max, "최대 레벨이 올바르게 복원되어야 합니다");
        Assert.AreEqual(originalLevel.FullMax, deserializedLevel.FullMax, "FullMax가 올바르게 복원되어야 합니다");
        Assert.AreEqual(originalLevel.LimitMax, deserializedLevel.LimitMax, "LimitMax가 올바르게 복원되어야 합니다");
    }

#endregion

#region LevelxEXP 클래스 테스트

    // ------------------------------------------------------------
    /// <summary>
    /// LevelxEXP 클래스의 기본 생성 및 초기값을 테스트합니다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void LevelxEXP_01_기본_생성_테스트()
    {
        // Arrange
        var expTable = new int[] { 10, 20, 30, 40, 50 };

        // Act
        var level = new LevelxEXP(expTable);

        // Assert
        Assert.AreEqual(0, level.Value, "초기 레벨은 0이어야 합니다");
        Assert.AreEqual(0, level.EXP, "초기 경험치는 0이어야 합니다");
        Assert.AreEqual(5, level.FullMax, "FullMax는 경험치 테이블 크기와 같아야 합니다");
        Assert.AreEqual(10, level.MaxEXP, "레벨 0의 최대 경험치는 10이어야 합니다");
        Assert.IsTrue(level.CanLevelUp, "초기에는 레벨업이 가능해야 합니다");
    }

    // ------------------------------------------------------------
    /// <summary>
    /// LevelxEXP 클래스의 경험치 설정 기능을 테스트합니다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void LevelxEXP_02_경험치_설정_테스트()
    {
        // Arrange
        var expTable = new int[] { 10, 20, 30, 40, 50 };
        var level = new LevelxEXP(expTable);

        // Act
        level.EXP = 5;

        // Assert
        Assert.AreEqual(5, level.EXP, "경험치가 올바르게 설정되어야 합니다");
        Assert.AreEqual(0, level.Value, "경험치가 최대치 미만이면 레벨업하지 않아야 합니다");
    }

    // ------------------------------------------------------------
    /// <summary>
    /// LevelxEXP 클래스의 자동 레벨업 기능을 테스트합니다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void LevelxEXP_03_자동_레벨업_테스트()
    {
        // Arrange
        var expTable = new int[] { 10, 20, 30, 40, 50 };
        var level = new LevelxEXP(expTable);
        bool eventFired = false;
        int eventLevel = -1;

        level.OnLevelUp += (sender, e) => {
            eventFired = true;
            eventLevel = e.Level;
        };

        // Act
        level.EXP = 10; // 정확히 레벨업에 필요한 경험치

        // Assert
        Assert.AreEqual(1, level.Value, "경험치가 최대치에 도달하면 레벨업해야 합니다");
        Assert.AreEqual(0, level.EXP, "레벨업 후 경험치는 0이 되어야 합니다");
        Assert.IsTrue(eventFired, "레벨업 이벤트가 발생해야 합니다");
        Assert.AreEqual(1, eventLevel, "이벤트에서 올바른 레벨을 전달해야 합니다");
    }

    // ------------------------------------------------------------
    /// <summary>
    /// LevelxEXP 클래스의 경험치 오버플로우 처리를 테스트합니다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void LevelxEXP_04_경험치_오버플로우_테스트()
    {
        // Arrange
        var expTable = new int[] { 10, 20, 30, 40, 50 };
        var level = new LevelxEXP(expTable);

        // Act
        level.EXP = 15; // 10을 초과하는 경험치

        // Assert
        Assert.AreEqual(1, level.Value, "레벨업이 발생해야 합니다");
        Assert.AreEqual(5, level.EXP, "남은 경험치가 다음 레벨로 이월되어야 합니다");
        Assert.AreEqual(20, level.MaxEXP, "레벨 1의 최대 경험치는 20이어야 합니다");
    }

    // ------------------------------------------------------------
    /// <summary>
    /// LevelxEXP 클래스의 다중 레벨업 처리를 테스트합니다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void LevelxEXP_05_다중_레벨업_테스트()
    {
        // Arrange
        var expTable = new int[] { 10, 20, 30, 40, 50 };
        var level = new LevelxEXP(expTable);
        int eventCount = 0;

        level.OnLevelUp += (sender, e) => eventCount++;

        // Act
        level.EXP = 35; // 10 + 20 + 5 = 레벨 0→1→2, 경험치 5 남음

        // Assert
        Assert.AreEqual(2, level.Value, "35 경험치로 레벨 2가 되어야 합니다");
        Assert.AreEqual(5, level.EXP, "5의 경험치가 남아야 합니다");
        Assert.AreEqual(2, eventCount, "2번의 레벨업 이벤트가 발생해야 합니다");
    }

    // ------------------------------------------------------------
    /// <summary>
    /// LevelxEXP 클래스의 최대 레벨에서 경험치 제한을 테스트합니다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void LevelxEXP_06_최대레벨_경험치_제한_테스트()
    {
        // Arrange
        var expTable = new int[] { 10, 20 }; // 최대 레벨 2
        var level = new LevelxEXP(expTable);
        level.Value = 2; // 최대 레벨로 설정
        bool eventFired = false;

        level.OnLevelUp += (sender, e) => eventFired = true;

        // Act
        level.EXP = 100; // 매우 큰 경험치 설정

        // Assert
        Assert.AreEqual(2, level.Value, "최대 레벨을 초과해서는 안 됩니다");
        Assert.AreEqual(0, level.EXP, "최대 레벨에서는 경험치가 0으로 제한되어야 합니다");
        Assert.IsFalse(level.CanLevelUp, "최대 레벨에서는 레벨업이 불가능해야 합니다");
        Assert.IsFalse(eventFired, "최대 레벨에서는 레벨업 이벤트가 발생하지 않아야 합니다");
    }

    // ------------------------------------------------------------
    /// <summary>
    /// LevelxEXP 클래스의 Value 직접 설정 시 경험치 리셋을 테스트합니다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void LevelxEXP_07_Value_직접_설정_테스트()
    {
        // Arrange
        var expTable = new int[] { 10, 20, 30, 40, 50 };
        var level = new LevelxEXP(expTable);
        level.EXP = 15; // 경험치를 먼저 설정

        // Act
        level.Value = 3; // 레벨을 직접 설정

        // Assert
        Assert.AreEqual(3, level.Value, "레벨이 직접 설정되어야 합니다");
        Assert.AreEqual(0, level.EXP, "레벨을 직접 설정하면 경험치가 리셋되어야 합니다");
        Assert.AreEqual(40, level.MaxEXP, "레벨 3의 최대 경험치는 40이어야 합니다");
    }

    // ------------------------------------------------------------
    /// <summary>
    /// LevelxEXP 클래스의 GetRequiredEXPToLevelUp 메서드를 테스트합니다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void LevelxEXP_08_GetRequiredEXPToLevelUp_테스트()
    {
        // Arrange
        var expTable = new int[] { 10, 20, 30, 40, 50 };
        var level = new LevelxEXP(expTable);

        // Act & Assert
        Assert.AreEqual(10, level.GetRequiredEXPToLevelUp(0), "레벨 0의 필요 경험치는 10이어야 합니다");
        Assert.AreEqual(20, level.GetRequiredEXPToLevelUp(1), "레벨 1의 필요 경험치는 20이어야 합니다");
        Assert.AreEqual(50, level.GetRequiredEXPToLevelUp(4), "레벨 4의 필요 경험치는 50이어야 합니다");
        Assert.AreEqual(0, level.GetRequiredEXPToLevelUp(5), "범위 밖 레벨의 필요 경험치는 0이어야 합니다");
        Assert.AreEqual(0, level.GetRequiredEXPToLevelUp(-1), "음수 레벨의 필요 경험치는 0이어야 합니다");
    }

#endregion

#region 경계값 및 예외 테스트

    // ------------------------------------------------------------
    /// <summary>
    /// 음수 경험치 처리를 테스트합니다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void LevelxEXP_09_음수_경험치_예외테스트()
    {
        // Arrange
        var expTable = new int[] { 10, 20, 30 };
        var level = new LevelxEXP(expTable);
        level.EXP = 5;

        // Act & Assert
        Assert.Throws<LevelxEXP.InvalidEXPException>(() => level.EXP = -10);

        // 기존 경험치는 변경되지 않아야 함
        Assert.AreEqual(5, level.EXP, "예외 발생 시 기존 경험치는 변경되지 않아야 합니다");
        Assert.AreEqual(0, level.Value, "예외 발생 시 레벨도 변경되지 않아야 합니다");
    }

    // ------------------------------------------------------------
    /// <summary>
    /// 빈 경험치 테이블로 생성 시 동작을 테스트합니다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void LevelxEXP_10_빈_경험치_테이블_테스트()
    {
        // Arrange
        var expTable = new int[] { };

        // Act
        var level = new LevelxEXP(expTable);

        // Assert
        Assert.AreEqual(0, level.FullMax, "빈 테이블의 FullMax는 0이어야 합니다");
        Assert.AreEqual(0, level.MaxEXP, "빈 테이블의 MaxEXP는 0이어야 합니다");
        Assert.IsFalse(level.CanLevelUp, "빈 테이블에서는 레벨업이 불가능해야 합니다");
    }

    // ------------------------------------------------------------
    /// <summary>
    /// 경험치 테이블에 0이 포함된 경우를 테스트합니다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void LevelxEXP_11_경험치_테이블_0_포함_테스트()
    {
        // Arrange
        var expTable = new int[] { 0, 10, 0, 20 };
        var level = new LevelxEXP(expTable);

        // Act
        level.EXP = 1; // 0보다 큰 경험치

        // Assert
        Assert.AreEqual(1, level.Value, "레벨 0에서 1로 레벨업하고, 레벨 1에서 10EXP 미만이므로 더 이상 레벨업하지 않습니다");
        Assert.AreEqual(1, level.EXP, "레벨 1에서 1 경험치가 남아야 합니다");
    }

    // ------------------------------------------------------------
    /// <summary>
    /// LevelxEXP 생성자에 null 경험치 테이블을 전달할 때 예외를 테스트합니다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void LevelxEXP_12_생성자_null_테이블_예외테스트()
    {
        // Act & Assert
        Assert.Throws<LevelxEXP.NullEXPTableException>(() => new LevelxEXP(null));
    }

    // ------------------------------------------------------------
    /// <summary>
    /// LevelxEXP 생성자에 음수값이 포함된 경험치 테이블을 전달할 때 예외를 테스트합니다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void LevelxEXP_13_생성자_음수값_포함_예외테스트()
    {
        // Arrange
        var expTable = new int[] { 10, -5, 20 }; // 음수값 포함

        // Act & Assert
        Assert.Throws<LevelxEXP.InvalidEXPTableException>(() => new LevelxEXP(expTable));
    }

    // ------------------------------------------------------------
    /// <summary>
    /// LevelxEXP 클래스의 JSON 직렬화/역직렬화를 테스트합니다.
    /// </summary>
    // ------------------------------------------------------------
    [Test]
    public void LevelxEXP_14_JSON_직렬화_테스트()
    {
        // Arrange - 경험치가 설정된 상태
        var expTable = new int[] { 10, 20, 30, 40, 50 };
        var originalLevelxEXP = new LevelxEXP(expTable);
        originalLevelxEXP.EXP = 25; // 레벨 2, 경험치 5 상태
        originalLevelxEXP.LimitMax = 3;

        // Act - 직렬화/역직렬화
        string json = JsonUtility.ToJson(originalLevelxEXP);
        var deserializedLevelxEXP = JsonUtility.FromJson<LevelxEXP>(json);

        // Assert - 상태 복원 확인
        Assert.AreEqual(originalLevelxEXP.Value, deserializedLevelxEXP.Value, "레벨 값이 올바르게 복원되어야 합니다");
        Assert.AreEqual(originalLevelxEXP.EXP, deserializedLevelxEXP.EXP, "경험치가 올바르게 복원되어야 합니다");
        Assert.AreEqual(originalLevelxEXP.MaxEXP, deserializedLevelxEXP.MaxEXP, "최대 경험치가 올바르게 복원되어야 합니다");
        Assert.AreEqual(originalLevelxEXP.FullMax, deserializedLevelxEXP.FullMax, "FullMax가 올바르게 복원되어야 합니다");
        Assert.AreEqual(originalLevelxEXP.LimitMax, deserializedLevelxEXP.LimitMax, "LimitMax가 올바르게 복원되어야 합니다");

        // Assert - expTable 직렬화 확인
        Assert.IsNotNull(deserializedLevelxEXP.RequiredEXPToLevelUpArray, "경험치 테이블이 직렬화되어야 합니다");
        var deserializedExpTable = deserializedLevelxEXP.RequiredEXPToLevelUpArray;
        Assert.AreEqual(expTable.Length, deserializedExpTable.Count, "경험치 테이블 길이가 올바르게 복원되어야 합니다");
        for (int i = 0; i < expTable.Length; i++)
        {
            Assert.AreEqual(expTable[i], deserializedExpTable[i], $"경험치 테이블[{i}] 값이 올바르게 복원되어야 합니다");
        }
    }

#endregion

}
