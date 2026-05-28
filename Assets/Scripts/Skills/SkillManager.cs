using System;
using System.Collections.Generic;
using DefaultNamespace;
using UnityEngine;

namespace Skills
{
    public static class SkillManager
    {
        public static MatchGroup UseSkill(Tile[,] grid, Vector2Int position, SpecialSkillType skillType)
        {
            switch (skillType)
            {
                case SpecialSkillType.StripedHorizontal:
                case SpecialSkillType.StripedVertical:
                    return UseStripedSkill(grid, position, skillType);
                default:
                    throw new ArgumentException("Skill not implemented.", nameof(skillType));
            }
        }

        private static MatchGroup UseStripedSkill(Tile[,] grid, Vector2Int skillPos, SpecialSkillType skillType)
        {
            int cols = grid.GetLength(0);
            int rows = grid.GetLength(1);
            var tiles = new List<Vector2Int>();

            if (skillType == SpecialSkillType.StripedHorizontal)
            {
                for (int x = 0; x < cols; x++)
                {
                    if (grid[x, skillPos.y].Petal == null) continue;
                    tiles.Add(new Vector2Int(x, skillPos.y));
                }
            }
            else if (skillType == SpecialSkillType.StripedVertical)
            {
                for (int y = 0; y < rows; y++)
                {
                    if (grid[skillPos.x, y].Petal == null) continue;
                    tiles.Add(new Vector2Int(skillPos.x, y));
                }
            }
            else
            {
                throw new ArgumentException("Not a striped skill type.", nameof(skillType));
            }

            return new MatchGroup(tiles, MatchShape.None);
        }
    }
}