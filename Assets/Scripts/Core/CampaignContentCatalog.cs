using System;
using System.Collections.Generic;
using System.Linq;

namespace PhalanxChronicle.Core
{
    public enum ItemCategory
    {
        Weapon = 0,
        Armor = 1,
        Mount = 2,
        SpecialGood = 3,
    }

    public enum TreasureEffectType
    {
        None = 0,
        SkillDamageBonus = 1,
        StatusDurationBonus = 2,
        GuardOnLowHp = 3,
        IgnoreHazardTick = 4,
        FortHealingBonus = 5,
        MovePlusOneOnFirstThreeTurns = 6,
    }

    [Serializable]
    public sealed class ItemDefinition
    {
        public ItemDefinition(
            string itemId,
            ItemCategory category,
            string nameKey,
            string nameFallback,
            string descriptionKey,
            string descriptionFallback,
            int attackBonus = 0,
            int defenseBonus = 0,
            int hpBonus = 0,
            int moveBonus = 0,
            IReadOnlyList<UnitRole> allowedRoles = null,
            bool isTreasure = false,
            TreasureEffectType treasureEffect = TreasureEffectType.None,
            string recommendedOwnerUnitId = "")
        {
            ItemId = itemId ?? string.Empty;
            Category = category;
            NameKey = nameKey ?? string.Empty;
            NameFallback = nameFallback ?? string.Empty;
            DescriptionKey = descriptionKey ?? string.Empty;
            DescriptionFallback = descriptionFallback ?? string.Empty;
            AttackBonus = attackBonus;
            DefenseBonus = defenseBonus;
            HpBonus = hpBonus;
            MoveBonus = moveBonus;
            AllowedRoles = allowedRoles ?? Array.Empty<UnitRole>();
            IsTreasure = isTreasure;
            TreasureEffect = treasureEffect;
            RecommendedOwnerUnitId = recommendedOwnerUnitId ?? string.Empty;
        }

        public string ItemId { get; }

        public ItemCategory Category { get; }

        public string NameKey { get; }

        public string NameFallback { get; }

        public string DescriptionKey { get; }

        public string DescriptionFallback { get; }

        public int AttackBonus { get; }

        public int DefenseBonus { get; }

        public int HpBonus { get; }

        public int MoveBonus { get; }

        public IReadOnlyList<UnitRole> AllowedRoles { get; }

        public bool IsTreasure { get; }

        public TreasureEffectType TreasureEffect { get; }

        public string RecommendedOwnerUnitId { get; }

        public bool IsEquipable =>
            Category == ItemCategory.Weapon ||
            Category == ItemCategory.Armor ||
            Category == ItemCategory.Mount;

        public bool CanEquip(UnitRole role)
        {
            return AllowedRoles.Count == 0 || AllowedRoles.Contains(role);
        }
    }

    [Serializable]
    public sealed class ShopOfferDefinition
    {
        public ShopOfferDefinition(string itemId, int suppliesCost, int requiredRenown)
        {
            ItemId = itemId ?? string.Empty;
            SuppliesCost = suppliesCost < 0 ? 0 : suppliesCost;
            RequiredRenown = requiredRenown < 0 ? 0 : requiredRenown;
        }

        public string ItemId { get; }

        public int SuppliesCost { get; }

        public int RequiredRenown { get; }
    }

    [Serializable]
    public sealed class PromotionDefinition
    {
        public PromotionDefinition(
            string promotionId,
            string unitId,
            string targetClassId,
            string targetGrowthProfileId,
            PassiveSkillType passiveSkill,
            string passiveSkillNameKey,
            string passiveSkillDescriptionKey,
            ActiveSkillType activeSkill,
            string activeSkillNameKey,
            string activeSkillDescriptionKey,
            int hpBonus,
            int attackBonus,
            int defenseBonus,
            int manaBonus)
        {
            PromotionId = promotionId ?? string.Empty;
            UnitId = unitId ?? string.Empty;
            TargetClassId = targetClassId ?? string.Empty;
            TargetGrowthProfileId = targetGrowthProfileId ?? string.Empty;
            PassiveSkill = passiveSkill;
            PassiveSkillNameKey = passiveSkillNameKey ?? string.Empty;
            PassiveSkillDescriptionKey = passiveSkillDescriptionKey ?? string.Empty;
            ActiveSkill = activeSkill;
            ActiveSkillNameKey = activeSkillNameKey ?? string.Empty;
            ActiveSkillDescriptionKey = activeSkillDescriptionKey ?? string.Empty;
            HpBonus = hpBonus;
            AttackBonus = attackBonus;
            DefenseBonus = defenseBonus;
            ManaBonus = manaBonus;
        }

        public string PromotionId { get; }

        public string UnitId { get; }

        public string TargetClassId { get; }

        public string TargetGrowthProfileId { get; }

        public PassiveSkillType PassiveSkill { get; }

        public string PassiveSkillNameKey { get; }

        public string PassiveSkillDescriptionKey { get; }

        public ActiveSkillType ActiveSkill { get; }

        public string ActiveSkillNameKey { get; }

        public string ActiveSkillDescriptionKey { get; }

        public int HpBonus { get; }

        public int AttackBonus { get; }

        public int DefenseBonus { get; }

        public int ManaBonus { get; }
    }

    public static class ItemCatalog
    {
        private static readonly UnitRole[] CommanderOnly = { UnitRole.Commander };
        private static readonly UnitRole[] GuardianOnly = { UnitRole.Guardian };
        private static readonly UnitRole[] RangerOnly = { UnitRole.Ranger };
        private static readonly UnitRole[] ScoutOnly = { UnitRole.Scout };
        private static readonly UnitRole[] RaiderOnly = { UnitRole.Raider };

        private static readonly IReadOnlyDictionary<string, ItemDefinition> Items =
            new Dictionary<string, ItemDefinition>(StringComparer.Ordinal)
            {
                ["vermilion-jian"] = new ItemDefinition("vermilion-jian", ItemCategory.Weapon, "item.vermilion_jian.name", "Vermilion Jian", "item.vermilion_jian.desc", "Liu Bei's field sword. It carries story weight, not extra force.", allowedRoles: CommanderOnly),
                ["iron-crescent-glaive"] = new ItemDefinition("iron-crescent-glaive", ItemCategory.Weapon, "item.iron_crescent_glaive.name", "Iron Crescent Glaive", "item.iron_crescent_glaive.desc", "A heavy glaive issued to the sworn vanguard. Solid, but not enchanted.", allowedRoles: GuardianOnly),
                ["featherback-war-bow"] = new ItemDefinition("featherback-war-bow", ItemCategory.Weapon, "item.featherback_war_bow.name", "Featherback War Bow", "item.featherback_war_bow.desc", "A trusted bow that keeps Huang Zhong steady in long exchanges.", allowedRoles: RangerOnly),
                ["wind-feather-fan"] = new ItemDefinition("wind-feather-fan", ItemCategory.Weapon, "item.wind_feather_fan.name", "Wind Feather Fan", "item.wind_feather_fan.desc", "A strategist's fan used to direct formations rather than carve through armor.", allowedRoles: CommanderOnly),
                ["white-dragon-spear"] = new ItemDefinition("white-dragon-spear", ItemCategory.Weapon, "item.white_dragon_spear.name", "White Dragon Spear", "item.white_dragon_spear.desc", "A balanced spear favored by fast cavalry who need precision over weight.", allowedRoles: ScoutOnly),
                ["western-lance"] = new ItemDefinition("western-lance", ItemCategory.Weapon, "item.western_lance.name", "Western Lance", "item.western_lance.desc", "A hard-riding lance built for sudden charges through broken lines.", allowedRoles: RaiderOnly),
                ["commander-travel-cloak"] = new ItemDefinition("commander-travel-cloak", ItemCategory.Armor, "item.commander_travel_cloak.name", "Commander Travel Cloak", "item.commander_travel_cloak.desc", "Campaign-worn layers meant for command, not raw defense.", allowedRoles: CommanderOnly),
                ["guardian-scale-vest"] = new ItemDefinition("guardian-scale-vest", ItemCategory.Armor, "item.guardian_scale_vest.name", "Guardian Scale Vest", "item.guardian_scale_vest.desc", "A battle-tested vest built to survive frontline collisions.", allowedRoles: GuardianOnly),
                ["ranger-hunt-coat"] = new ItemDefinition("ranger-hunt-coat", ItemCategory.Armor, "item.ranger_hunt_coat.name", "Ranger Hunt Coat", "item.ranger_hunt_coat.desc", "A light coat for long marches and clean draws.", allowedRoles: RangerOnly),
                ["strategist-robe"] = new ItemDefinition("strategist-robe", ItemCategory.Armor, "item.strategist_robe.name", "Strategist Robe", "item.strategist_robe.desc", "Layered robes cut for mobility in the command tent and on the march.", allowedRoles: CommanderOnly),
                ["scout-travel-mail"] = new ItemDefinition("scout-travel-mail", ItemCategory.Armor, "item.scout_travel_mail.name", "Scout Travel Mail", "item.scout_travel_mail.desc", "Light mail that keeps a rider quick enough to punch through openings.", allowedRoles: ScoutOnly),
                ["raider-scale-vest"] = new ItemDefinition("raider-scale-vest", ItemCategory.Armor, "item.raider_scale_vest.name", "Raider Scale Vest", "item.raider_scale_vest.desc", "Flexible armor that can survive a charge without dragging the rider down.", allowedRoles: RaiderOnly),
                ["tempered-jian"] = new ItemDefinition("tempered-jian", ItemCategory.Weapon, "item.tempered_jian.name", "Tempered Jian", "item.tempered_jian.desc", "A sharpened command blade that adds a little more bite.", attackBonus: 1, allowedRoles: CommanderOnly),
                ["commander-lamellar"] = new ItemDefinition("commander-lamellar", ItemCategory.Armor, "item.commander_lamellar.name", "Commander Lamellar", "item.commander_lamellar.desc", "Layered armor balanced for leadership and survivability.", defenseBonus: 1, hpBonus: 2, allowedRoles: CommanderOnly),
                ["crescent-glaive"] = new ItemDefinition("crescent-glaive", ItemCategory.Weapon, "item.crescent_glaive.name", "Crescent Glaive", "item.crescent_glaive.desc", "A heavier crescent blade that hits far harder than the standard issue.", attackBonus: 2, allowedRoles: GuardianOnly),
                ["guardian-plate"] = new ItemDefinition("guardian-plate", ItemCategory.Armor, "item.guardian_plate.name", "Guardian Plate", "item.guardian_plate.desc", "Dense plates that turn guardians into true anchors.", defenseBonus: 2, hpBonus: 4, allowedRoles: GuardianOnly),
                ["composite-bow"] = new ItemDefinition("composite-bow", ItemCategory.Weapon, "item.composite_bow.name", "Composite Bow", "item.composite_bow.desc", "A tighter bow with better stored force for disciplined volleys.", attackBonus: 1, allowedRoles: RangerOnly),
                ["ranger-coat"] = new ItemDefinition("ranger-coat", ItemCategory.Armor, "item.ranger_coat.name", "Ranger Coat", "item.ranger_coat.desc", "A reinforced coat that trades little speed for needed resilience.", defenseBonus: 1, hpBonus: 2, allowedRoles: RangerOnly),
                ["dragon-rider-spear"] = new ItemDefinition("dragon-rider-spear", ItemCategory.Weapon, "item.dragon_rider_spear.name", "Dragon Rider Spear", "item.dragon_rider_spear.desc", "A long cavalry spear tuned for decisive thrusts at full speed.", attackBonus: 2, allowedRoles: ScoutOnly),
                ["scout-war-cloak"] = new ItemDefinition("scout-war-cloak", ItemCategory.Armor, "item.scout_war_cloak.name", "Scout War Cloak", "item.scout_war_cloak.desc", "A reinforced cloak that keeps elite scouts fast without leaving them exposed.", defenseBonus: 1, hpBonus: 2, allowedRoles: ScoutOnly),
                ["storm-lance"] = new ItemDefinition("storm-lance", ItemCategory.Weapon, "item.storm_lance.name", "Storm Lance", "item.storm_lance.desc", "A charge lance heavy enough to smash a hole in a pinned formation.", attackBonus: 2, allowedRoles: RaiderOnly),
                ["raider-war-harness"] = new ItemDefinition("raider-war-harness", ItemCategory.Armor, "item.raider_war_harness.name", "Raider War Harness", "item.raider_war_harness.desc", "Extra plates strapped for impact without sacrificing the momentum of a raid.", defenseBonus: 1, hpBonus: 3, allowedRoles: RaiderOnly),
                ["field-horse"] = new ItemDefinition("field-horse", ItemCategory.Mount, "item.field_horse.name", "Field Horse", "item.field_horse.desc", "A dependable campaign horse that adds a little more reach to every move.", moveBonus: 1),
                ["swift-warhorse"] = new ItemDefinition("swift-warhorse", ItemCategory.Mount, "item.swift_warhorse.name", "Swift Warhorse", "item.swift_warhorse.desc", "A faster warhorse bred for aggressive repositioning and sudden breakthroughs.", moveBonus: 2),
                ["yellow-turban-signet"] = new ItemDefinition("yellow-turban-signet", ItemCategory.Armor, "item.yellow_turban_signet.name", "Yellow Turban Signet", "item.yellow_turban_signet.desc", "A bronze signet seized at Guangzong. It hardens the command line when the battle turns desperate.", defenseBonus: 1, hpBonus: 2, allowedRoles: CommanderOnly, isTreasure: true, treasureEffect: TreasureEffectType.GuardOnLowHp, recommendedOwnerUnitId: "player-liu-bei"),
                ["guangzong-rally-seal"] = new ItemDefinition("guangzong-rally-seal", ItemCategory.Armor, "item.guangzong_rally_seal.name", "Guangzong Rally Seal", "item.guangzong_rally_seal.desc", "A fast-command seal taken when Guangzong's outer zealots were broken before the line could close. It steadies the commander the moment the battle turns dangerous.", defenseBonus: 1, hpBonus: 2, allowedRoles: CommanderOnly, isTreasure: true, treasureEffect: TreasureEffectType.GuardOnLowHp, recommendedOwnerUnitId: "player-liu-bei"),
                ["bowang-fire-token"] = new ItemDefinition("bowang-fire-token", ItemCategory.Weapon, "item.bowang_fire_token.name", "Bowang Fire Token", "item.bowang_fire_token.desc", "A signal token from the Bowangpo ambush. It sharpens the force behind tactical battle arts.", attackBonus: 1, allowedRoles: CommanderOnly, isTreasure: true, treasureEffect: TreasureEffectType.SkillDamageBonus, recommendedOwnerUnitId: "player-zhuge-liang"),
                ["changban-scout-map"] = new ItemDefinition("changban-scout-map", ItemCategory.Mount, "item.changban_scout_map.name", "Changban Scout Map", "item.changban_scout_map.desc", "An escape map marked at full gallop through Changban. The rider reads the ground before anyone else.", moveBonus: 1, allowedRoles: ScoutOnly, isTreasure: true, treasureEffect: TreasureEffectType.MovePlusOneOnFirstThreeTurns, recommendedOwnerUnitId: "player-zhao-yun"),
                ["changban-white-plume"] = new ItemDefinition("changban-white-plume", ItemCategory.Mount, "item.changban_white_plume.name", "Changban White Plume", "item.changban_white_plume.desc", "A white plume recovered after Zhao Yun's lone breakthrough at Changban. It pushes the first mounted rush even farther before the enemy can close.", moveBonus: 1, allowedRoles: ScoutOnly, isTreasure: true, treasureEffect: TreasureEffectType.MovePlusOneOnFirstThreeTurns, recommendedOwnerUnitId: "player-zhao-yun"),
                ["jiangxia-river-reins"] = new ItemDefinition("jiangxia-river-reins", ItemCategory.Mount, "item.jiangxia_river_reins.name", "Jiangxia River Reins", "item.jiangxia_river_reins.desc", "River-worn reins taken from the Jiangxia crossing. The mount keeps pace even through fire and broken banks.", moveBonus: 1, isTreasure: true, treasureEffect: TreasureEffectType.IgnoreHazardTick, recommendedOwnerUnitId: "player-liu-bei"),
                ["jiameng-oath-banner"] = new ItemDefinition("jiameng-oath-banner", ItemCategory.Armor, "item.jiameng_oath_banner.name", "Jiameng Oath Banner", "item.jiameng_oath_banner.desc", "A battle banner from the pass. Its oath-lashed silk lets pressure effects linger on routed foes.", defenseBonus: 1, hpBonus: 2, allowedRoles: RaiderOnly, isTreasure: true, treasureEffect: TreasureEffectType.StatusDurationBonus, recommendedOwnerUnitId: "player-ma-chao"),
                ["jiameng-iron-girth"] = new ItemDefinition("jiameng-iron-girth", ItemCategory.Armor, "item.jiameng_iron_girth.name", "Jiameng Iron Girth", "item.jiameng_iron_girth.desc", "A reinforced cavalry girth taken after the pass commandant was crushed head-on. It keeps a rider upright and guarded once the charge turns bloody.", defenseBonus: 1, hpBonus: 2, allowedRoles: RaiderOnly, isTreasure: true, treasureEffect: TreasureEffectType.GuardOnLowHp, recommendedOwnerUnitId: "player-ma-chao"),
                ["baishui-signal-spear"] = new ItemDefinition("baishui-signal-spear", ItemCategory.Weapon, "item.baishui_signal_spear.name", "Baishui Signal Spear", "item.baishui_signal_spear.desc", "A silver-tipped spear used to break the Baishui bridge line before the retreat could close. It rewards precise rescue thrusts.", attackBonus: 1, allowedRoles: ScoutOnly, isTreasure: true, treasureEffect: TreasureEffectType.SkillDamageBonus, recommendedOwnerUnitId: "player-zhao-yun"),
                ["baishui-rapid-order"] = new ItemDefinition("baishui-rapid-order", ItemCategory.Weapon, "item.baishui_rapid_order.name", "Baishui Rapid Order", "item.baishui_rapid_order.desc", "A lacquered order tablet seized when the lower bridge was cut by force instead of timing. It sharpens skills used in the instant an opening appears.", attackBonus: 1, allowedRoles: ScoutOnly, isTreasure: true, treasureEffect: TreasureEffectType.SkillDamageBonus, recommendedOwnerUnitId: "player-zhao-yun"),
                ["mianzhu-feather-sigil"] = new ItemDefinition("mianzhu-feather-sigil", ItemCategory.Weapon, "item.mianzhu_feather_sigil.name", "Mianzhu Feather Sigil", "item.mianzhu_feather_sigil.desc", "A command sigil carried through the Mianzhu breach. Tactical arts cut deeper when formations answer the signal in time.", attackBonus: 1, allowedRoles: CommanderOnly, isTreasure: true, treasureEffect: TreasureEffectType.SkillDamageBonus, recommendedOwnerUnitId: "player-zhuge-liang"),
                ["luocheng-breach-hammer"] = new ItemDefinition("luocheng-breach-hammer", ItemCategory.Weapon, "item.luocheng_breach_hammer.name", "Luocheng Breach Hammer", "item.luocheng_breach_hammer.desc", "A city-breaker's head refitted for field command. It turns every crushing strike into a deeper crack in the line.", attackBonus: 2, allowedRoles: GuardianOnly, isTreasure: true, treasureEffect: TreasureEffectType.SkillDamageBonus, recommendedOwnerUnitId: "player-zhang-fei"),
                ["yangping-stone-route"] = new ItemDefinition("yangping-stone-route", ItemCategory.Mount, "item.yangping_stone_route.name", "Yangping Stone Route", "item.yangping_stone_route.desc", "A hidden mountain route mapped through falling stone. The rider finds a flank before the pass fully closes.", moveBonus: 1, allowedRoles: RaiderOnly, isTreasure: true, treasureEffect: TreasureEffectType.MovePlusOneOnFirstThreeTurns, recommendedOwnerUnitId: "player-ma-chao"),
                ["tiandang-falcon-badge"] = new ItemDefinition("tiandang-falcon-badge", ItemCategory.Armor, "item.tiandang_falcon_badge.name", "Tiandang Falcon Badge", "item.tiandang_falcon_badge.desc", "A mountain badge taken in the Tiandang night raid. Marksman pressure lingers longer after the signal flare falls.", defenseBonus: 1, hpBonus: 2, allowedRoles: RangerOnly, isTreasure: true, treasureEffect: TreasureEffectType.StatusDurationBonus, recommendedOwnerUnitId: "player-huang-zhong"),
                ["tiandang-night-token"] = new ItemDefinition("tiandang-night-token", ItemCategory.Armor, "item.tiandang_night_token.name", "Tiandang Night Token", "item.tiandang_night_token.desc", "A silent-pass token taken when both beacons died before the ridge could answer. Its marks keep pressure effects hanging on enemies longer in the dark.", defenseBonus: 1, hpBonus: 2, allowedRoles: RangerOnly, isTreasure: true, treasureEffect: TreasureEffectType.StatusDurationBonus, recommendedOwnerUnitId: "player-huang-zhong"),
                ["hanshui-command-seal"] = new ItemDefinition("hanshui-command-seal", ItemCategory.Armor, "item.hanshui_command_seal.name", "Hanshui Command Seal", "item.hanshui_command_seal.desc", "A riverbank seal carried from the Hanshui counterstroke. Fortified ground answers more readily to its bearer.", defenseBonus: 1, hpBonus: 2, allowedRoles: RangerOnly, isTreasure: true, treasureEffect: TreasureEffectType.FortHealingBonus, recommendedOwnerUnitId: "player-huang-zhong"),
                ["dingjun-war-banner"] = new ItemDefinition("dingjun-war-banner", ItemCategory.Weapon, "item.dingjun_war_banner.name", "Dingjun War Banner", "item.dingjun_war_banner.desc", "A captured banner from Dingjun Mountain. Its weight turns a finishing art into a decisive kill stroke.", attackBonus: 2, allowedRoles: GuardianOnly, isTreasure: true, treasureEffect: TreasureEffectType.SkillDamageBonus, recommendedOwnerUnitId: "player-guan-yu"),
                ["dingjun-gold-spur"] = new ItemDefinition("dingjun-gold-spur", ItemCategory.Mount, "item.dingjun_gold_spur.name", "Dingjun Gold Spur", "item.dingjun_gold_spur.desc", "A gilded spur taken after Huang Zhong's decisive duel at Dingjun. It lets the bearer hit the kill line sooner in the opening turns.", moveBonus: 1, allowedRoles: RangerOnly, isTreasure: true, treasureEffect: TreasureEffectType.MovePlusOneOnFirstThreeTurns, recommendedOwnerUnitId: "player-huang-zhong"),
            };

        public static IReadOnlyList<ItemDefinition> All => Items.Values.OrderBy(item => item.ItemId, StringComparer.Ordinal).ToList();

        public static ItemDefinition Get(string itemId)
        {
            if (!string.IsNullOrWhiteSpace(itemId) && Items.TryGetValue(itemId, out ItemDefinition definition))
            {
                return definition;
            }

            return null;
        }
    }

    public static class ShopCatalog
    {
        private static readonly IReadOnlyDictionary<string, ShopOfferDefinition> Offers =
            new Dictionary<string, ShopOfferDefinition>(StringComparer.Ordinal)
            {
                ["tempered-jian"] = new ShopOfferDefinition("tempered-jian", 60, 0),
                ["commander-lamellar"] = new ShopOfferDefinition("commander-lamellar", 55, 0),
                ["crescent-glaive"] = new ShopOfferDefinition("crescent-glaive", 80, 1),
                ["guardian-plate"] = new ShopOfferDefinition("guardian-plate", 75, 1),
                ["composite-bow"] = new ShopOfferDefinition("composite-bow", 70, 1),
                ["ranger-coat"] = new ShopOfferDefinition("ranger-coat", 50, 0),
                ["dragon-rider-spear"] = new ShopOfferDefinition("dragon-rider-spear", 75, 1),
                ["scout-war-cloak"] = new ShopOfferDefinition("scout-war-cloak", 55, 1),
                ["storm-lance"] = new ShopOfferDefinition("storm-lance", 85, 2),
                ["raider-war-harness"] = new ShopOfferDefinition("raider-war-harness", 60, 1),
                ["field-horse"] = new ShopOfferDefinition("field-horse", 40, 0),
                ["swift-warhorse"] = new ShopOfferDefinition("swift-warhorse", 80, 1),
            };

        public static IReadOnlyList<ShopOfferDefinition> All => Offers.Values.OrderBy(offer => offer.ItemId, StringComparer.Ordinal).ToList();

        public static ShopOfferDefinition Get(string itemId)
        {
            if (!string.IsNullOrWhiteSpace(itemId) && Offers.TryGetValue(itemId, out ShopOfferDefinition definition))
            {
                return definition;
            }

            return null;
        }
    }

    public static class PromotionCatalog
    {
        private static readonly IReadOnlyDictionary<string, IReadOnlyList<PromotionDefinition>> Promotions =
            new Dictionary<string, IReadOnlyList<PromotionDefinition>>(StringComparer.Ordinal)
            {
                ["player-liu-bei"] = new[]
                {
                    new PromotionDefinition("lord", "player-liu-bei", "lord", "lord", PassiveSkillType.BenevolentCommand, "skill.benevolent_command.name", "skill.benevolent_command.desc", ActiveSkillType.ImperialAid, "skill.imperial_aid.name", "skill.imperial_aid.desc", 3, 1, 1, 2),
                    new PromotionDefinition("warlord", "player-liu-bei", "warlord", "warlord", PassiveSkillType.CommandAura, "skill.command_aura.name", "skill.command_aura.desc", ActiveSkillType.KingsBanner, "skill.kings_banner.name", "skill.kings_banner.desc", 4, 2, 1, 1),
                },
                ["player-guan-yu"] = new[]
                {
                    new PromotionDefinition("saint_blade", "player-guan-yu", "saint_blade", "saint_blade", PassiveSkillType.DragonGuard, "skill.dragon_guard.name", "skill.dragon_guard.desc", ActiveSkillType.AzureDragonSlash, "skill.azure_dragon_slash.name", "skill.azure_dragon_slash.desc", 4, 2, 1, 1),
                    new PromotionDefinition("halberdier_general", "player-guan-yu", "halberdier_general", "halberdier_general", PassiveSkillType.ArmorBreak, "skill.armor_break.name", "skill.armor_break.desc", ActiveSkillType.CrimsonCrescent, "skill.crimson_crescent.name", "skill.crimson_crescent.desc", 5, 3, 0, 0),
                },
                ["player-zhang-fei"] = new[]
                {
                    new PromotionDefinition("vanguard_general", "player-zhang-fei", "vanguard_general", "vanguard_general", PassiveSkillType.ThunderVanguard, "skill.thunder_vanguard.name", "skill.thunder_vanguard.desc", ActiveSkillType.LionWarCry, "skill.lion_war_cry.name", "skill.lion_war_cry.desc", 5, 2, 1, 0),
                    new PromotionDefinition("fortress_general", "player-zhang-fei", "fortress_general", "fortress_general", PassiveSkillType.Fortress, "skill.fortress.name", "skill.fortress.desc", ActiveSkillType.StonewallChallenge, "skill.stonewall_challenge.name", "skill.stonewall_challenge.desc", 6, 1, 2, 1),
                },
                ["player-huang-zhong"] = new[]
                {
                    new PromotionDefinition("master_bow", "player-huang-zhong", "master_bow", "master_bow", PassiveSkillType.Deadeye, "skill.deadeye.name", "skill.deadeye.desc", ActiveSkillType.SkyVolley, "skill.sky_volley.name", "skill.sky_volley.desc", 3, 2, 0, 2),
                    new PromotionDefinition("pinning_bow", "player-huang-zhong", "pinning_bow", "pinning_bow", PassiveSkillType.LongShot, "skill.long_shot.name", "skill.long_shot.desc", ActiveSkillType.PinningShot, "skill.pinning_shot.name", "skill.pinning_shot.desc", 2, 2, 1, 1),
                },
                ["player-zhuge-liang"] = new[]
                {
                    new PromotionDefinition("sleeping_dragon", "player-zhuge-liang", "sleeping_dragon", "sleeping_dragon", PassiveSkillType.BenevolentCommand, "skill.benevolent_command.name", "skill.benevolent_command.desc", ActiveSkillType.EightTrigramInferno, "skill.eight_trigram_inferno.name", "skill.eight_trigram_inferno.desc", 2, 1, 1, 4),
                    new PromotionDefinition("tactician_general", "player-zhuge-liang", "tactician_general", "tactician_general", PassiveSkillType.CommandAura, "skill.command_aura.name", "skill.command_aura.desc", ActiveSkillType.FeatherFormation, "skill.feather_formation.name", "skill.feather_formation.desc", 3, 1, 1, 3),
                },
                ["player-zhao-yun"] = new[]
                {
                    new PromotionDefinition("white_horse_general", "player-zhao-yun", "white_horse_general", "white_horse_general", PassiveSkillType.GaleStride, "skill.gale_stride.name", "skill.gale_stride.desc", ActiveSkillType.WhiteHorseRescue, "skill.white_horse_rescue.name", "skill.white_horse_rescue.desc", 4, 2, 1, 1),
                    new PromotionDefinition("dragon_lancer", "player-zhao-yun", "dragon_lancer", "dragon_lancer", PassiveSkillType.ArmorBreak, "skill.armor_break.name", "skill.armor_break.desc", ActiveSkillType.GreenDragonSlash, "skill.green_dragon_slash.name", "skill.green_dragon_slash.desc", 5, 2, 1, 0),
                },
                ["player-ma-chao"] = new[]
                {
                    new PromotionDefinition("storm_raider", "player-ma-chao", "storm_raider", "storm_raider", PassiveSkillType.ThunderVanguard, "skill.thunder_vanguard.name", "skill.thunder_vanguard.desc", ActiveSkillType.StormbreakCharge, "skill.stormbreak_charge.name", "skill.stormbreak_charge.desc", 5, 2, 1, 0),
                    new PromotionDefinition("western_lancer", "player-ma-chao", "western_lancer", "western_lancer", PassiveSkillType.GaleStride, "skill.gale_stride.name", "skill.gale_stride.desc", ActiveSkillType.DustDevilSweep, "skill.dust_devil_sweep.name", "skill.dust_devil_sweep.desc", 4, 2, 1, 1),
                },
            };

        public static PromotionDefinition Get(string unitId)
        {
            if (!string.IsNullOrWhiteSpace(unitId) && Promotions.TryGetValue(unitId, out IReadOnlyList<PromotionDefinition> definitions))
            {
                return definitions.FirstOrDefault();
            }

            return null;
        }

        public static IReadOnlyList<PromotionDefinition> GetOptions(string unitId)
        {
            if (!string.IsNullOrWhiteSpace(unitId) && Promotions.TryGetValue(unitId, out IReadOnlyList<PromotionDefinition> definitions))
            {
                return definitions;
            }

            return Array.Empty<PromotionDefinition>();
        }
    }
}
