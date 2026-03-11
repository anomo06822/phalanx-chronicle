using System.Collections.Generic;
using System.Linq;
using PhalanxChronicle.Core;
using UnityEngine;

namespace PhalanxChronicle.Data
{
    [CreateAssetMenu(menuName = "Phalanx Chronicle/Stage Definition", fileName = "StageDefinition")]
    public sealed class StageDefinition : ScriptableObject
    {
        [SerializeField] private string stageName = "Frontier Pass";
        [SerializeField] private string stageNameKey = "stage.frontier_pass";
        [SerializeField] private int width = 10;
        [SerializeField] private int height = 10;
        [SerializeField] private bool useRandomMap = true;
        [SerializeField] private int randomSeed;
        [SerializeField] private List<Vector2Int> blockedCells = new List<Vector2Int>();
        [SerializeField] private List<TerrainPlacement> terrainTiles = new List<TerrainPlacement>();
        [SerializeField] private List<UnitSpawnData> unitSpawns = new List<UnitSpawnData>();

        public bool UseRandomMap => useRandomMap;

        public StageDefinitionData ToData()
        {
            return BuildData(randomSeed);
        }

        public StageDefinitionData CreateRerolledData()
        {
            return useRandomMap ? BuildData(0) : ToData();
        }

        private StageDefinitionData BuildData(int seed)
        {
            List<PhalanxChronicle.Core.UnitSpawnData> spawnData = unitSpawns.Select(spawn => spawn.ToData()).ToList();
            if (useRandomMap)
            {
                return RandomStageGenerator.Create(
                    stageName,
                    stageNameKey,
                    width,
                    height,
                    spawnData.Select(spawn => spawn.Definition).ToList(),
                    seed);
            }

            return new StageDefinitionData(
                stageName,
                stageNameKey,
                width,
                height,
                spawnData,
                blockedCells.Select(position => new GridPosition(position.x, position.y)).ToList(),
                terrainTiles.Select(tile => tile.ToData()).ToList(),
                false,
                0);
        }

        public static StageDefinition CreateDefault()
        {
            StageDefinition stage = CreateInstance<StageDefinition>();
            stage.stageName = "Frontier Pass";
            stage.stageNameKey = "stage.frontier_pass";
            stage.width = 10;
            stage.height = 10;
            stage.useRandomMap = true;
            stage.unitSpawns = new List<UnitSpawnData>
            {
                new UnitSpawnData(
                    UnitDefinition.CreateRuntime(
                        "player-1",
                        "Liu Bei",
                        "unit.liu_bei",
                        UnitFaction.Player,
                        UnitRole.Commander,
                        "role.commander",
                        PassiveSkillType.CommandAura,
                        "skill.command_aura.name",
                        "skill.command_aura.desc",
                        ActiveSkillType.RoyalAid,
                        "skill.royal_aid.name",
                        "skill.royal_aid.desc",
                        30,
                        10,
                        5,
                        3,
                        1),
                    new Vector2Int(1, 2)),
                new UnitSpawnData(
                    UnitDefinition.CreateRuntime(
                        "player-2",
                        "Guan Yu",
                        "unit.guan_yu",
                        UnitFaction.Player,
                        UnitRole.Guardian,
                        "role.guardian",
                        PassiveSkillType.ArmorBreak,
                        "skill.armor_break.name",
                        "skill.armor_break.desc",
                        ActiveSkillType.PowerStrike,
                        "skill.power_strike.name",
                        "skill.power_strike.desc",
                        32,
                        11,
                        5,
                        3,
                        1),
                    new Vector2Int(1, 5)),
                new UnitSpawnData(
                    UnitDefinition.CreateRuntime(
                        "player-3",
                        "Huang Zhong",
                        "unit.huang_zhong",
                        UnitFaction.Player,
                        UnitRole.Ranger,
                        "role.ranger",
                        PassiveSkillType.LongShot,
                        "skill.long_shot.name",
                        "skill.long_shot.desc",
                        ActiveSkillType.Volley,
                        "skill.volley.name",
                        "skill.volley.desc",
                        26,
                        9,
                        3,
                        3,
                        2),
                    new Vector2Int(1, 7)),
                new UnitSpawnData(
                    UnitDefinition.CreateRuntime(
                        "enemy-1",
                        "Zhang Bao",
                        "unit.zhang_bao",
                        UnitFaction.Enemy,
                        UnitRole.Raider,
                        "role.raider",
                        PassiveSkillType.RapidMarch,
                        "skill.rapid_march.name",
                        "skill.rapid_march.desc",
                        ActiveSkillType.PowerStrike,
                        "skill.power_strike.name",
                        "skill.power_strike.desc",
                        22,
                        8,
                        3,
                        3,
                        1),
                    new Vector2Int(7, 2)),
                new UnitSpawnData(
                    UnitDefinition.CreateRuntime(
                        "enemy-2",
                        "Raider Han",
                        "unit.raider_han",
                        UnitFaction.Enemy,
                        UnitRole.Raider,
                        "role.raider",
                        PassiveSkillType.ArmorBreak,
                        "skill.armor_break.name",
                        "skill.armor_break.desc",
                        ActiveSkillType.PowerStrike,
                        "skill.power_strike.name",
                        "skill.power_strike.desc",
                        20,
                        8,
                        3,
                        3,
                        1),
                    new Vector2Int(8, 4)),
                new UnitSpawnData(
                    UnitDefinition.CreateRuntime(
                        "enemy-3",
                        "Zhang Liang",
                        "unit.zhang_liang",
                        UnitFaction.Enemy,
                        UnitRole.Scout,
                        "role.scout",
                        PassiveSkillType.ShieldWall,
                        "skill.shield_wall.name",
                        "skill.shield_wall.desc",
                        ActiveSkillType.Volley,
                        "skill.volley.name",
                        "skill.volley.desc",
                        21,
                        8,
                        3,
                        4,
                        1),
                    new Vector2Int(7, 6)),
            };
            return stage;
        }

        [System.Serializable]
        private sealed class TerrainPlacement
        {
            [SerializeField] private TerrainType terrainType = TerrainType.Plain;
            [SerializeField] private Vector2Int position;

            public TerrainTileData ToData()
            {
                return new TerrainTileData(new GridPosition(position.x, position.y), terrainType);
            }
        }
    }
}
