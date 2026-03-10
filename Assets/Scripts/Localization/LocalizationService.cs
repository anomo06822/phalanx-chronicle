using System.Collections.Generic;
using System.Globalization;

namespace PhalanxChronicle.Localization
{
    public static class LocalizationService
    {
        private static readonly IReadOnlyDictionary<GameLocale, IReadOnlyDictionary<string, string>> Tables =
            new Dictionary<GameLocale, IReadOnlyDictionary<string, string>>
            {
                [GameLocale.English] = new Dictionary<string, string>
                {
                    ["stage.frontier_pass"] = "Frontier Pass",
                    ["ui.stage"] = "Stage: {0}",
                    ["ui.seed.value"] = "Seed: {0}",
                    ["ui.seed.fixed"] = "Seed: Fixed",
                    ["ui.turn.player"] = "Turn: Player Phase",
                    ["ui.turn.enemy"] = "Turn: Enemy Phase",
                    ["ui.turn.end"] = "Turn: Battle End",
                    ["ui.log.select_player"] = "Select a blue officer to act.",
                    ["ui.log.select_to_move"] = "Select a blue officer to move.",
                    ["ui.log.choose_destination"] = "Choose a destination, click an enemy to auto-attack, or click the unit to hold position.",
                    ["ui.log.choose_action"] = "Choose Attack, Skill, or Wait. You can also click an enemy in range to attack.",
                    ["ui.log.select_target"] = "Select an enemy target.",
                    ["ui.log.select_skill_target"] = "Select a skill target.",
                    ["ui.log.enemy_acting"] = "Enemy forces are acting.",
                    ["ui.log.unit_waited"] = "{0} held position.",
                    ["ui.log.enemy_advanced"] = "{0} advanced.",
                    ["ui.log.enemy_held"] = "{0} held position.",
                    ["ui.log.victory"] = "All enemies defeated.",
                    ["ui.log.defeat"] = "All player units have fallen.",
                    ["ui.selected.none"] = "Selected: None\nPick a blue officer to act.\nPassive skills change movement, range, and combat.",
                    ["ui.selected.summary"] = "Selected: {0}\nRole {1}\nPassive {2}  Active {3}\nHP {4}/{5}  ATK {6}  DEF {7}  MOVE {8}  RANGE {9}  {10}",
                    ["ui.status.ready"] = "READY",
                    ["ui.status.done"] = "DONE",
                    ["ui.status.none"] = "None",
                    ["ui.result.victory"] = "Victory",
                    ["ui.result.defeat"] = "Defeat",
                    ["ui.button.attack"] = "Attack",
                    ["ui.button.skill"] = "Skill",
                    ["ui.button.wait"] = "Wait",
                    ["ui.button.reroll"] = "Reroll",
                    ["ui.button.end_turn"] = "End Turn",
                    ["ui.label.cooldown"] = "Cooldown",
                    ["ui.label.status"] = "Status",
                    ["ui.role.short.commander"] = "CMD",
                    ["ui.role.short.guardian"] = "GDN",
                    ["ui.role.short.ranger"] = "RNG",
                    ["ui.role.short.scout"] = "SCT",
                    ["ui.role.short.raider"] = "RDR",
                    ["ui.role.short.unknown"] = "UNIT",
                    ["ui.cooldown.ready"] = "Ready",
                    ["ui.cooldown.value"] = "CD {0}",
                    ["ui.cooldown.none"] = "-",
                    ["ui.combat.banner"] = "{0} strikes {1}",
                    ["ui.combat.damage"] = "-{0} HP   {1} left",
                    ["ui.combat.ko"] = "-{0} HP   KO",
                    ["ui.combat.popup_ko"] = "KO",
                    ["ui.combat.log.damage"] = "{0} dealt {1} damage to {2}.",
                    ["ui.combat.log.ko"] = "{0} defeated {1}.",
                    ["ui.skill.banner"] = "{0} uses {1}",
                    ["ui.skill.detail.heal"] = "+{0} HP",
                    ["ui.skill.detail.damage"] = "-{0} HP",
                    ["ui.skill.detail.multi"] = "{0} targets hit",
                    ["ui.skill.log.use"] = "{0} used {1}.",
                    ["unit.liu_bei"] = "Liu Bei",
                    ["unit.guan_yu"] = "Guan Yu",
                    ["unit.huang_zhong"] = "Huang Zhong",
                    ["unit.zhang_bao"] = "Zhang Bao",
                    ["unit.raider_han"] = "Raider Han",
                    ["unit.zhang_liang"] = "Zhang Liang",
                    ["role.commander"] = "Commander",
                    ["role.guardian"] = "Guardian",
                    ["role.ranger"] = "Ranger",
                    ["role.scout"] = "Scout",
                    ["role.raider"] = "Raider",
                    ["skill.command_aura.name"] = "Command Aura",
                    ["skill.command_aura.desc"] = "Adjacent allies gain +2 ATK.",
                    ["skill.shield_wall.name"] = "Shield Wall",
                    ["skill.shield_wall.desc"] = "Receives 2 less damage.",
                    ["skill.long_shot.name"] = "Long Shot",
                    ["skill.long_shot.desc"] = "Attack range increases by 1.",
                    ["skill.rapid_march.name"] = "Rapid March",
                    ["skill.rapid_march.desc"] = "Move range increases by 1.",
                    ["skill.armor_break.name"] = "Armor Break",
                    ["skill.armor_break.desc"] = "Ignores 2 points of defense.",
                    ["skill.none.name"] = "None",
                    ["skill.none.desc"] = "No special effect.",
                    ["active.none.name"] = "None",
                    ["active.none.desc"] = "No active skill.",
                    ["skill.royal_aid.name"] = "Royal Aid",
                    ["skill.royal_aid.desc"] = "Restore 8 HP to an ally within 2 tiles.",
                    ["skill.power_strike.name"] = "Power Strike",
                    ["skill.power_strike.desc"] = "Deal 4 bonus damage with a heavy blow.",
                    ["skill.volley.name"] = "Volley",
                    ["skill.volley.desc"] = "Fire on a target and splash nearby enemies.",
                    ["status.inspired.name"] = "Inspired",
                    ["status.inspired.desc"] = "+2 ATK until the end of this side's turn.",
                    ["status.shattered_armor.name"] = "Shattered Armor",
                    ["status.shattered_armor.desc"] = "-2 DEF until the end of this side's turn.",
                },
                [GameLocale.TraditionalChinese] = new Dictionary<string, string>
                {
                    ["stage.frontier_pass"] = "界橋前線",
                    ["ui.stage"] = "關卡：{0}",
                    ["ui.seed.value"] = "種子：{0}",
                    ["ui.seed.fixed"] = "種子：固定地圖",
                    ["ui.turn.player"] = "回合：我軍行動",
                    ["ui.turn.enemy"] = "回合：敵軍行動",
                    ["ui.turn.end"] = "回合：戰鬥結束",
                    ["ui.log.select_player"] = "請選擇一名藍色武將行動。",
                    ["ui.log.select_to_move"] = "請選擇一名藍色武將移動。",
                    ["ui.log.choose_destination"] = "請選擇移動位置，直接點擊可攻擊敵將可自動出手，或再次點擊原武將原地待命。",
                    ["ui.log.choose_action"] = "請選擇攻擊、技能或待命；若敵將已在範圍內，也可直接點擊敵將攻擊。",
                    ["ui.log.select_target"] = "請選擇攻擊目標。",
                    ["ui.log.select_skill_target"] = "請選擇技能目標。",
                    ["ui.log.enemy_acting"] = "敵軍開始行動。",
                    ["ui.log.unit_waited"] = "{0} 原地待命。",
                    ["ui.log.enemy_advanced"] = "{0} 向前推進。",
                    ["ui.log.enemy_held"] = "{0} 留在原地。",
                    ["ui.log.victory"] = "敵軍已全數擊破。",
                    ["ui.log.defeat"] = "我軍已全數戰敗。",
                    ["ui.selected.none"] = "未選擇單位\n請選擇藍色武將行動。\n被動技能會影響移動、射程與戰鬥。",
                    ["ui.selected.summary"] = "已選：{0}\n兵種 {1}\n被動 {2}  主動 {3}\nHP {4}/{5}  攻 {6}  防 {7}  移 {8}  射程 {9}  {10}",
                    ["ui.status.ready"] = "可行動",
                    ["ui.status.done"] = "已行動",
                    ["ui.status.none"] = "無",
                    ["ui.result.victory"] = "勝利",
                    ["ui.result.defeat"] = "敗北",
                    ["ui.button.attack"] = "攻擊",
                    ["ui.button.skill"] = "技能",
                    ["ui.button.wait"] = "待命",
                    ["ui.button.reroll"] = "重抽地圖",
                    ["ui.button.end_turn"] = "結束回合",
                    ["ui.label.cooldown"] = "冷卻",
                    ["ui.label.status"] = "狀態",
                    ["ui.role.short.commander"] = "統",
                    ["ui.role.short.guardian"] = "甲",
                    ["ui.role.short.ranger"] = "弓",
                    ["ui.role.short.scout"] = "先",
                    ["ui.role.short.raider"] = "騎",
                    ["ui.role.short.unknown"] = "兵",
                    ["ui.cooldown.ready"] = "可用",
                    ["ui.cooldown.value"] = "冷卻 {0}",
                    ["ui.cooldown.none"] = "-",
                    ["ui.combat.banner"] = "{0} 攻擊 {1}",
                    ["ui.combat.damage"] = "-{0} HP   剩餘 {1}",
                    ["ui.combat.ko"] = "-{0} HP   擊破",
                    ["ui.combat.popup_ko"] = "擊破",
                    ["ui.combat.log.damage"] = "{0} 對 {2} 造成 {1} 點傷害。",
                    ["ui.combat.log.ko"] = "{0} 擊破了 {1}。",
                    ["ui.skill.banner"] = "{0} 施展 {1}",
                    ["ui.skill.detail.heal"] = "+{0} HP",
                    ["ui.skill.detail.damage"] = "-{0} HP",
                    ["ui.skill.detail.multi"] = "命中 {0} 名目標",
                    ["ui.skill.log.use"] = "{0} 施展了 {1}。",
                    ["unit.liu_bei"] = "劉備",
                    ["unit.guan_yu"] = "關羽",
                    ["unit.huang_zhong"] = "黃忠",
                    ["unit.zhang_bao"] = "張寶",
                    ["unit.raider_han"] = "韓賊兵",
                    ["unit.zhang_liang"] = "張梁",
                    ["role.commander"] = "統帥",
                    ["role.guardian"] = "重甲",
                    ["role.ranger"] = "弓將",
                    ["role.scout"] = "先鋒",
                    ["role.raider"] = "突騎",
                    ["skill.command_aura.name"] = "統御",
                    ["skill.command_aura.desc"] = "相鄰友軍攻擊力 +2。",
                    ["skill.shield_wall.name"] = "鐵壁",
                    ["skill.shield_wall.desc"] = "受到傷害 -2。",
                    ["skill.long_shot.name"] = "遠射",
                    ["skill.long_shot.desc"] = "攻擊射程 +1。",
                    ["skill.rapid_march.name"] = "疾行",
                    ["skill.rapid_march.desc"] = "移動力 +1。",
                    ["skill.armor_break.name"] = "破甲",
                    ["skill.armor_break.desc"] = "無視 2 點防禦。",
                    ["skill.none.name"] = "無",
                    ["skill.none.desc"] = "沒有特殊效果。",
                    ["active.none.name"] = "無",
                    ["active.none.desc"] = "沒有主動技能。",
                    ["skill.royal_aid.name"] = "仁德",
                    ["skill.royal_aid.desc"] = "為 2 格內友軍恢復 8 點生命。",
                    ["skill.power_strike.name"] = "猛擊",
                    ["skill.power_strike.desc"] = "以重擊額外造成 4 點傷害。",
                    ["skill.volley.name"] = "箭雨",
                    ["skill.volley.desc"] = "射擊目標並波及周圍敵軍。",
                    ["status.inspired.name"] = "鼓舞",
                    ["status.inspired.desc"] = "本方回合結束前攻擊 +2。",
                    ["status.shattered_armor.name"] = "破甲",
                    ["status.shattered_armor.desc"] = "本方回合結束前防禦 -2。",
                },
            };

        public static GameLocale CurrentLocale { get; private set; } = GameLocale.TraditionalChinese;

        public static void SetLocale(GameLocale locale)
        {
            CurrentLocale = locale;
        }

        public static string Text(string key, string fallback = null)
        {
            if (Tables.TryGetValue(CurrentLocale, out IReadOnlyDictionary<string, string> currentTable) &&
                currentTable.TryGetValue(key, out string value))
            {
                return value;
            }

            if (Tables.TryGetValue(GameLocale.English, out IReadOnlyDictionary<string, string> defaultTable) &&
                defaultTable.TryGetValue(key, out value))
            {
                return value;
            }

            return fallback ?? key;
        }

        public static string Format(string key, string fallback, params object[] args)
        {
            return string.Format(CultureInfo.InvariantCulture, Text(key, fallback), args);
        }
    }
}
