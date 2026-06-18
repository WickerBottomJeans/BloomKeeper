using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class MatchDetectorPlayModeTests
{
    private const int GridSize = 7;

    [TestCase("Three", "2,3;3,3;4,3")]
    [TestCase("Four", "1,3;2,3;3,3;4,3")]
    [TestCase("Five", "1,3;2,3;3,3;4,3;5,3")]
    [TestCase("TShape", "2,4;3,4;4,4;3,3;3,2")]
    [TestCase("LShape", "2,4;2,3;2,2;3,2;4,2")]
    [TestCase("Cross", "2,3;3,3;4,3;3,2;3,4")]
    [TestCase("Square2x2", "2,2;3,2;2,3;3,3")]
    public void Detect_FindsExpectedMatchShape(string expectedShapeName, string encodedCells)
    {
        List<Vector2Int> expectedCells = ParseCells(encodedCells);
        Array grid = CreateGrid(expectedCells);

        Type detectorType = FindType("MatchDetector");
        IEnumerable matches = (IEnumerable)detectorType
            .GetMethod("Detect", BindingFlags.Public | BindingFlags.Static)
            .Invoke(null, new object[] { grid });

        var detected = new List<(string Shape, HashSet<Vector2Int> Cells)>();

        foreach (object match in matches)
        {
            Type matchType = match.GetType();
            string shape = matchType.GetField("Shape").GetValue(match).ToString();
            IEnumerable positions = (IEnumerable)matchType.GetField("TilePositions").GetValue(match);
            var cells = new HashSet<Vector2Int>();

            foreach (Vector2Int position in positions)
                cells.Add(position);

            detected.Add((shape, cells));
        }

        HashSet<Vector2Int> expected = expectedCells.ToHashSet();
        bool found = detected.Any(match =>
            match.Shape == expectedShapeName &&
            match.Cells.SetEquals(expected));

        Assert.That(found, Is.True,
            $"Expected {expectedShapeName} at [{encodedCells}], but detected: {Describe(detected)}");
    }

    private static Array CreateGrid(IEnumerable<Vector2Int> matchingCells)
    {
        Type tileType = FindType("DefaultNamespace.Tile");
        Type inactiveTileType = FindType("DefaultNamespace.InactiveTile");
        Type normalTileType = FindType("DefaultNamespace.NormalTile");
        Type petalType = FindType("Petal");
        Type petalTypeEnum = FindType("DefaultNamespace.PetalType");
        Type skillTypeEnum = FindType("DefaultNamespace.SpecialSkillType");

        Array grid = Array.CreateInstance(tileType, GridSize, GridSize);

        for (int x = 0; x < GridSize; x++)
        for (int y = 0; y < GridSize; y++)
            grid.SetValue(Activator.CreateInstance(inactiveTileType), x, y);

        object rose = Enum.Parse(petalTypeEnum, "Rose");
        object noSkill = Enum.Parse(skillTypeEnum, "None");

        foreach (Vector2Int cell in matchingCells)
        {
            object tile = Activator.CreateInstance(normalTileType);
            object petal = Activator.CreateInstance(petalType, rose, noSkill);
            tileType.GetProperty("Petal").SetValue(tile, petal);
            grid.SetValue(tile, cell.x, cell.y);
        }

        return grid;
    }

    private static List<Vector2Int> ParseCells(string encodedCells)
    {
        return encodedCells
            .Split(';')
            .Select(encoded => encoded.Split(','))
            .Select(parts => new Vector2Int(int.Parse(parts[0]), int.Parse(parts[1])))
            .ToList();
    }

    private static string Describe(IEnumerable<(string Shape, HashSet<Vector2Int> Cells)> detected)
    {
        string result = string.Join(", ", detected.Select(match =>
            $"{match.Shape}[{string.Join(";", match.Cells.Select(cell => $"{cell.x},{cell.y}"))}]") );
        return string.IsNullOrEmpty(result) ? "nothing" : result;
    }

    private static Type FindType(string fullName)
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type type = assembly.GetType(fullName);
            if (type != null)
                return type;
        }

        throw new InvalidOperationException($"Could not find runtime type '{fullName}'.");
    }
}
